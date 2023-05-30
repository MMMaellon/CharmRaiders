
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;
#endif

namespace MMMaellon
{
    
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public class UpgradeTrackerEditor : IVRCSDKBuildRequestedCallback
    {
        public static bool Setup()
        {
            UpgradeTracker upgradeTracker = GameObject.FindObjectOfType<UpgradeTracker>();
            Upgrade[] upgrades = GameObject.FindObjectsOfType<Upgrade>();
            if (Utilities.IsValid(upgradeTracker) && Utilities.IsValid(upgrades) && upgrades.Length > 0)
            {
                SerializedObject serialized = new SerializedObject(upgradeTracker);
                serialized.FindProperty("upgrades").ClearArray();
                for (int i = 0; i < upgrades.Length; i++)
                {
                    serialized.FindProperty("upgrades").InsertArrayElementAtIndex(i);
                    serialized.FindProperty("upgrades").GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
                    SerializedObject serializedUpgrade = new SerializedObject(upgrades[i]);
                    if (upgrades[i].GetComponent<CharmAttachment>())
                    {
                        SerializedObject serializedCharm = new SerializedObject(upgrades[i].GetComponent<CharmAttachment>());
                        serializedCharm.FindProperty("tracker").objectReferenceValue = upgradeTracker;
                        serializedCharm.ApplyModifiedProperties();
                        serializedUpgrade.FindProperty("charm").objectReferenceValue = upgrades[i].GetComponent<CharmAttachment>();
                    }
                    serializedUpgrade.ApplyModifiedProperties();
                    upgrades[i].AddPriceTag();
                }
                CharmPool[] charmPools = GameObject.FindObjectsOfType<CharmPool>();
                foreach (CharmPool charmPool in charmPools)
                {
                    SerializedObject serializedPool = new SerializedObject(charmPool);
                    serializedPool.FindProperty("charms").ClearArray();
                    ChildAttachmentState[] charmChildren = charmPool.GetComponentsInChildren<ChildAttachmentState>();
                    for (int i = 0; i < charmChildren.Length; i++)
                    {
                        serializedPool.FindProperty("charms").InsertArrayElementAtIndex(i);
                        serializedPool.FindProperty("charms").GetArrayElementAtIndex(i).objectReferenceValue = charmChildren[i];
                    }
                    serializedPool.ApplyModifiedProperties();
                }
                serialized.FindProperty("charmPools").ClearArray();
                for (int i = 0; i < charmPools.Length; i++)
                {
                    serialized.FindProperty("charmPools").InsertArrayElementAtIndex(i);
                    serialized.FindProperty("charmPools").GetArrayElementAtIndex(i).objectReferenceValue = charmPools[i];
                }
                serialized.ApplyModifiedProperties();
            }

            return true;
        }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            Setup();
        }

        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            return Setup();
        }
    }
#endif

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class UpgradeTracker : SmartObjectSyncListener
    {
        public Cyan.PlayerObjectPool.CyanPlayerObjectAssigner playerObjectAssigner;
        public P_ShootersPlayerHandler playerHandler;
        public CharmPlayerListener charmListener;
        public CharmPool[] charmPools;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(upgradesEnabled))]
        public bool _upgradesEnabled;
        public bool upgradesEnabled {
            get => _upgradesEnabled;
            set
            {
                _upgradesEnabled = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        [HideInInspector]
        public Upgrade[] upgrades;
        [System.NonSerialized]
        public VRC.SDK3.Data.DataList activeUpgradeList = new VRC.SDK3.Data.DataList();
        [System.NonSerialized]
        public VRC.SDK3.Data.DataList activeLoopingUpgradeList = new VRC.SDK3.Data.DataList();
        [System.NonSerialized]
        public CharmAttachment[] desktopCharms = new CharmAttachment[0];
        [System.NonSerialized]
        public Upgrade[] activeLoopingUpgradeListArray = new Upgrade[0];
        [System.NonSerialized]
        public VRC.SDK3.Data.DataToken[] activeLoopingUpgradeListTokenArray = new VRC.SDK3.Data.DataToken[0];

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            currentUpgrade = sync.GetComponent<Upgrade>();
            if (!Utilities.IsValid(currentUpgrade) || !currentUpgrade.isUpgrading || oldOwner == newOwner)
            {
                return;
            }
            if (Utilities.IsValid(oldOwner) && oldOwner.isLocal)
            {
                RemoveUpgrade(currentUpgrade);
            } else if(sync.IsLocalOwner())
            {
                AddUpgrade(currentUpgrade);
            }
            currentUpgrade.StopUpgrade();
            playerObject = playerObjectAssigner._GetPlayerPooledObject(newOwner);
            if (!Utilities.IsValid(playerObject))
            {
                if (sync.IsLocalOwner())
                {
                    sync.customState.ExitState();
                    //force drop
                }
                return;
            }
            currentUpgrade.player = playerObject.GetComponent<Player>();
            currentUpgrade.StartUpgrade();
        }

        Upgrade currentUpgrade;
        CharmAttachment currentCharm;

        public float weightCurve = 5f;
        public float baseRunSpeed = 12;
        public float baseWalkSpeed = 8;
        public float explodeVelocity = 5f;

        [System.NonSerialized]
        public int _weight = 0;
        public int weight{
            get => _weight;
            set
            {
                _weight = value;
                Networking.LocalPlayer.SetRunSpeed((baseRunSpeed * weightCurve) / (weight + weightCurve));
                Networking.LocalPlayer.SetStrafeSpeed((baseRunSpeed * weightCurve) / (weight + weightCurve));
                Networking.LocalPlayer.SetWalkSpeed((baseWalkSpeed * weightCurve) / (weight + weightCurve));
            }
        }
        CharmAttachment currentState;
        GameObject playerObject;
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (sync.IsAttachedToPlayer() || (Utilities.IsValid(sync.customState) && sync.customState.GetUdonTypeName() == "MMMaellon.CharmAttachment"))
            {
                currentUpgrade = sync.GetComponent<Upgrade>();
                if (!Utilities.IsValid(currentUpgrade) || currentUpgrade.isUpgrading)
                {
                    return;
                }
                playerObject = playerObjectAssigner._GetPlayerPooledObject(sync.owner);
                if (!Utilities.IsValid(playerObject))
                {
                    if (sync.IsLocalOwner())
                    {
                        sync.customState.ExitState();
                        //force drop
                    }
                    return;
                }
                currentUpgrade.player = playerObject.GetComponent<Player>();
                if (sync.IsLocalOwner())
                {
                    AddUpgrade(currentUpgrade);
                }
                currentUpgrade.isUpgrading = true;
                currentUpgrade.StartUpgrade();
            }
            else if(oldState < SmartObjectSync.STATE_SLEEPING || oldState > SmartObjectSync.STATE_FALLING)//includes checks that it's not teleporting and interpolating
            {
                currentUpgrade = sync.GetComponent<Upgrade>();
                if (!Utilities.IsValid(currentUpgrade) || !currentUpgrade.isUpgrading)
                {
                    return;
                }
                if (sync.IsLocalOwner())
                {
                    RemoveUpgrade(currentUpgrade);
                }
                currentUpgrade.StopUpgrade();
                currentUpgrade.player = null;
                currentUpgrade.isUpgrading = false;
            }

        }

        public void AddUpgrade(Upgrade upgrade)
        {
            if (!activeUpgradeList.Contains(upgrade))
            {
                weight = weight + upgrade.weight;
                if (upgrade.useLoop)
                {
                    activeLoopingUpgradeList.Add(upgrade);
                    activeLoopingUpgradeListArray = new Upgrade[activeLoopingUpgradeList.Count];
                    activeLoopingUpgradeListTokenArray = activeLoopingUpgradeList.ToArray();
                    for (int i = 0; i < activeLoopingUpgradeListTokenArray.Length; i++)
                    {
                        activeLoopingUpgradeListArray[i] = (Upgrade) activeLoopingUpgradeListTokenArray[i].Reference;
                    }
                }
                activeUpgradeList.Add(upgrade);
            }
        }

        int listCount = 0;
        public void RemoveUpgrade(Upgrade upgrade)
        {
            if (activeUpgradeList.Contains(upgrade))
            {
                weight = weight - upgrade.weight;
                activeUpgradeList.Remove(upgrade);
                listCount = activeLoopingUpgradeList.Count;
                activeLoopingUpgradeList.Remove(upgrade);
                if (listCount != activeLoopingUpgradeList.Count)
                {
                    activeLoopingUpgradeListArray = new Upgrade[activeLoopingUpgradeList.Count];
                    activeLoopingUpgradeListTokenArray = activeLoopingUpgradeList.ToArray();
                    for (int i = 0; i < activeLoopingUpgradeListTokenArray.Length; i++)
                    {
                        activeLoopingUpgradeListArray[i] = (Upgrade)activeLoopingUpgradeListTokenArray[i].Reference;
                    }
                }
            }
        }

        void Start()
        {
            foreach (Upgrade upgrade in upgrades)
            {
                upgrade.charm.sync.AddListener(this);
            }
            weight = weight;
            Networking.LocalPlayer.SetJumpImpulse(4);
        }
        [System.NonSerialized]
        public int selectedIndex = 0;
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (selectedIndex >= 0 && selectedIndex < desktopCharms.Length)
                {
                    desktopCharms[selectedIndex].ExitState();
                }
            }
            if (!Utilities.IsValid(activeLoopingUpgradeListArray) || activeLoopingUpgradeListArray.Length == 0)
            {
                return;
            }
            foreach (VRC.SDK3.Data.DataToken upgradeToken in activeLoopingUpgradeListArray)
            {
                ((Upgrade) upgradeToken.Reference).UpgradeLoop();
            }
        }

        public void TestRespawn()
        {
            Networking.LocalPlayer.Respawn();
        }

        public void SpawnCharms()
        {
            foreach (CharmPool pool in charmPools)
            {
                pool.SpawnCharms();
            }
        }
        public void TestRespawnObjects()
        {
            foreach (CharmPool pool in charmPools)
            {
                pool.UnspawnAll();
            }
        }

        public void ExplodeUpgrades()
        {
            //clear it so we don't need to repeatedly recreate this thing
            foreach (VRC.SDK3.Data.DataToken desktopCharm in desktopCharms)
            {
                currentCharm = (CharmAttachment) desktopCharm.Reference;
                currentCharm.transform.position += currentCharm.transform.rotation * (currentCharm.sync.pos);
            }
            desktopCharms = new CharmAttachment[0];
            foreach (VRC.SDK3.Data.DataToken upgradeToken in activeUpgradeList.ToArray())
            {
                currentUpgrade = (Upgrade)upgradeToken.Reference;
                currentUpgrade.charm.sync.rigid.velocity = (currentUpgrade.transform.position - currentUpgrade.charm.sync.parentPos).normalized * explodeVelocity;
                currentUpgrade.charm.sync.rigid.angularVelocity = currentUpgrade.charm.sync.rigid.velocity;
                currentUpgrade.charm.sync.state = SmartObjectSync.STATE_FALLING;
            }
            activeLoopingUpgradeList.Clear();
            activeLoopingUpgradeListArray = new Upgrade[0];
            activeUpgradeList.Clear();
        }
        public float inventoryInterpolationTime = 0.25f;
        public Vector3 desktopInventoryPlacement = new Vector3(0, -0.25f, 0.5f);
        public float inventorySpacing = 0.05f;
        public float desktopInventorySpacing = 0.25f;
        [System.NonSerialized]
        public float inventoryOffset = 0.25f;
        CharmAttachment[] tempArray;
        public void AddToDesktopInventory(CharmAttachment charm)
        {
            if (charm)
            {
                charm.sync.TakeOwnership(false);
                charm.inventoryIndex = desktopCharms.Length;
                tempArray = new CharmAttachment[desktopCharms.Length + 1];
                for (int i = 0; i < desktopCharms.Length; i++)
                {
                    //restart interpolation
                    desktopCharms[i].inventoryIndex = i;
                    tempArray[i] = desktopCharms[i];
                }
                tempArray[desktopCharms.Length] = charm;
                desktopCharms = tempArray;
                foreach (CharmAttachment c in desktopCharms)
                {
                    c.sync.Serialize();
                    c.sync.OnInterpolationStart();
                }
            }
        }
        public void RemoveFromDesktopInventory(int index)
        {
            if (index >= 0 && index < desktopCharms.Length)
            {
                tempArray = new CharmAttachment[desktopCharms.Length - 1];
                for (int i = 0; i < desktopCharms.Length; i++)
                {
                    if (i < index)
                    {
                        tempArray[i] = desktopCharms[i];
                    }
                    else if (i > index)
                    {
                        desktopCharms[i].inventoryIndex = i - 1;
                        tempArray[i - 1] = desktopCharms[i];
                    }
                }
                desktopCharms = tempArray;
                foreach (CharmAttachment c in desktopCharms)
                {
                    c.sync.Serialize();
                    c.sync.OnInterpolationStart();
                }
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                ExplodeUpgrades();
            }
        }
    }
}