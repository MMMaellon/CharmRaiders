
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class UpgradeTracker : SmartObjectSyncListener
    {
        public Cyan.PlayerObjectPool.CyanPlayerObjectAssigner playerObjectAssigner;
        public P_Shooters.P_ShootersPlayerHandler playerHandler;
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
        public Charm[] upgrades;
        [System.NonSerialized]
        public VRC.SDK3.Data.DataList activeUpgradeList = new VRC.SDK3.Data.DataList();
        [System.NonSerialized]
        public VRC.SDK3.Data.DataList activeLoopingUpgradeList = new VRC.SDK3.Data.DataList();
        [System.NonSerialized]
        public Charm[] activeLoopingUpgradeListArray = new Charm[0];
        [System.NonSerialized]
        public VRC.SDK3.Data.DataToken[] activeLoopingUpgradeListTokenArray = new VRC.SDK3.Data.DataToken[0];

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            currentUpgrade = sync.GetComponent<Charm>();
            if (!Utilities.IsValid(currentUpgrade) || oldOwner == newOwner)
            {
                return;
            }
            if (sync.IsHeld() || (Utilities.IsValid(sync.transform.parent) && Utilities.IsValid(sync.transform.parent.GetComponent<Bag>())))
            {
                currentUpgrade.player = playerObjectAssigner._GetPlayerPooledObject(sync.owner).GetComponent<Player>();
            } else
            {
                currentUpgrade.player = null;
            }
        }

        Charm currentUpgrade;

        [System.NonSerialized]
        public float weightCurve = 25f;
        [System.NonSerialized]
        public float baseRunSpeed = 6;
        [System.NonSerialized]
        public float baseWalkSpeed = 3;
        [System.NonSerialized]
        public float explodeVelocity = 5f;

        public float slowdownRatio = 0.75f;
        [System.NonSerialized]
        public float slowdown1Ratio = 0.5f;
        [System.NonSerialized]
        public float slowdown2Ratio = 0.25f;

        [System.NonSerialized]
        public int slowdownLimit = 5;
        [System.NonSerialized]
        public int slowdown1Limit = 15;
        [System.NonSerialized]
        public int slowdown2Limit = 30;
        [System.NonSerialized]
        public int overburdenedLimit = 40;

        [System.NonSerialized]
        public int _weight = 0;
        float speed;
        public int weight{
            get => _weight;
            set
            {
                _weight = value;
                // Networking.LocalPlayer.SetRunSpeed((baseRunSpeed * weightCurve) / (_weight + weightCurve));
                // Networking.LocalPlayer.SetStrafeSpeed((baseRunSpeed * weightCurve) / (_weight + weightCurve));
                // Networking.LocalPlayer.SetWalkSpeed((baseWalkSpeed * weightCurve) / (_weight + weightCurve));
                // speed = _weight / weightCurve;
                // Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed - speed));
                // Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed - speed));
                // Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed - speed));
                // speed = 2/(1 + Mathf.Pow(2, weight / weightCurve));
                // Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed * speed));
                // Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed * speed));
                // Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed * speed));
                if (value >= overburdenedLimit) {
                    Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, 1));
                    Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, 1));
                    Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, 1));
                    if (Utilities.IsValid(localPlayer))
                    {
                        localPlayer.bag.weightText.text = "<color=black>" + value.ToString();
                    }
                } else if (value >= slowdown2Limit)
                {
                    Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed * slowdown2Ratio));
                    Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed * slowdown2Ratio));
                    Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed * slowdown2Ratio));
                    if (Utilities.IsValid(localPlayer))
                    {
                        localPlayer.bag.weightText.text = "<color=red>" + value.ToString();
                    }
                }
                else if (value >= slowdown1Limit)
                {
                    Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed * slowdown1Ratio));
                    Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed * slowdown1Ratio));
                    Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed * slowdown1Ratio));
                    if (Utilities.IsValid(localPlayer))
                    {
                        localPlayer.bag.weightText.text = "<color=orange>" + value.ToString();
                    }
                }
                else if (value >= slowdownLimit)
                {
                    Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed * slowdownRatio));
                    Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed * slowdownRatio));
                    Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed * slowdownRatio));
                    if (Utilities.IsValid(localPlayer))
                    {
                        localPlayer.bag.weightText.text = "<color=yellow>" + value.ToString();
                    }
                }
                else
                {
                    Debug.LogWarning("what");
                    Networking.LocalPlayer.SetRunSpeed(Mathf.Max(0, baseRunSpeed));
                    Networking.LocalPlayer.SetStrafeSpeed(Mathf.Max(0, baseRunSpeed));
                    Networking.LocalPlayer.SetWalkSpeed(Mathf.Max(0, baseWalkSpeed));
                    if (Utilities.IsValid(localPlayer))
                    {
                        localPlayer.bag.weightText.text = value.ToString();
                    }
                }
            }
        }
        GameObject playerObject;
        Player _localPlayer;
        Player localPlayer{
            get
            {
                if (!Utilities.IsValid(_localPlayer))
                {
                    _localPlayer = (Player) playerHandler.localPlayer;
                }
                return _localPlayer;
            }
        }
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            currentUpgrade = sync.GetComponent<Charm>();
            if (!Utilities.IsValid(currentUpgrade))
            {
                return;
            }
            if (sync.IsHeld())
            {
                currentUpgrade.player = playerObjectAssigner._GetPlayerPooledObject(sync.owner).GetComponent<Player>();
            } else if (Utilities.IsValid(sync.transform.parent) && Utilities.IsValid(sync.transform.parent.GetComponent<Bag>()))
            {
                if (sync.IsLocalOwner())
                {
                    currentUpgrade.player = playerObjectAssigner._GetPlayerPooledObject(sync.owner).GetComponent<Player>();
                    sync.transform.parent.GetComponent<Bag>().backpackAttachment.sync.StartInterpolation();
                }
                //for non-localowners we handle it in Ondeserialize for the bag setter
            }
            else
            {
                currentUpgrade.player = null;
            }
            // if (Utilities.IsValid(currentUpgrade.player) && currentUpgrade.player.IsOwnerLocal())
            // {
            //     // if (oldState == currentUpgrade.bagSetter.child.stateID + SmartObjectSync.STATE_CUSTOM)
            //     // {
            //     //     localPlayer.bag.totalPrice -= currentUpgrade.price;
            //     // }
            //     //we subtract in our own thingy
            //     if (newState == currentUpgrade.bagSetter.child.stateID + SmartObjectSync.STATE_CUSTOM)
            //     {
            //         localPlayer.bag.totalPrice += currentUpgrade.price;
            //     }
            // }
        }

        public void AddCharm(Charm upgrade)
        {
            if (!activeUpgradeList.Contains(upgrade))
            {
                weight = weight + upgrade.weight;
                // if (Utilities.IsValid(localPlayer))
                // {
                //     localPlayer.bag.totalPrice = localPlayer.bag.totalPrice + upgrade.price;
                // }
                if (upgrade.useLoop)
                {
                    activeLoopingUpgradeList.Add(upgrade);
                    activeLoopingUpgradeListArray = new Charm[activeLoopingUpgradeList.Count];
                    activeLoopingUpgradeListTokenArray = activeLoopingUpgradeList.ToArray();
                    for (int i = 0; i < activeLoopingUpgradeListTokenArray.Length; i++)
                    {
                        activeLoopingUpgradeListArray[i] = (Charm) activeLoopingUpgradeListTokenArray[i].Reference;
                    }
                }
                activeUpgradeList.Add(upgrade);
            }
        }

        int listCount = 0;
        public void RemoveCharm(Charm upgrade)
        {
            // Debug.LogWarning("Remove Upgrade");
            if (activeUpgradeList.Contains(upgrade))
            {
                // Debug.LogWarning("Remove Upgrade -- it contains");
                weight = weight - upgrade.weight;
                // if (Utilities.IsValid(localPlayer))
                // {
                //     localPlayer.bag.totalPrice = localPlayer.bag.totalPrice - upgrade.price;
                // }
                activeUpgradeList.Remove(upgrade);
                listCount = activeLoopingUpgradeList.Count;
                activeLoopingUpgradeList.Remove(upgrade);
                if (listCount != activeLoopingUpgradeList.Count)
                {
                    activeLoopingUpgradeListArray = new Charm[activeLoopingUpgradeList.Count];
                    activeLoopingUpgradeListTokenArray = activeLoopingUpgradeList.ToArray();
                    for (int i = 0; i < activeLoopingUpgradeListTokenArray.Length; i++)
                    {
                        activeLoopingUpgradeListArray[i] = (Charm)activeLoopingUpgradeListTokenArray[i].Reference;
                    }
                }
            }
        }

        void Start()
        {
            foreach (Charm upgrade in upgrades)
            {
                upgrade.bagSetter.child.sync.AddListener(this);
            }
            weight = 0;
            Networking.LocalPlayer.SetJumpImpulse(4);
        }
        [System.NonSerialized]
        public int selectedIndex = 0;
        public void Update()
        {
            if (!Utilities.IsValid(activeLoopingUpgradeListArray) || activeLoopingUpgradeListArray.Length == 0)
            {
                return;
            }
            foreach (VRC.SDK3.Data.DataToken upgradeToken in activeLoopingUpgradeListArray)
            {
                ((Charm) upgradeToken.Reference).CharmLoop();
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
                foreach(CharmSpawn spawn in pool.spawns) {
                    spawn.ResetSpawn();
                }
            }
        }
        public void TestRespawnObjects()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TestRespawnObjectsCallback));
        }

        public void TestRespawnObjectsCallback()
        {
            foreach (CharmPool pool in charmPools)
            {
                if (Networking.LocalPlayer.IsOwner(pool.gameObject))
                {
                    pool.RespawnAll();
                }
            }
        }

        public void ClearUpgrades()
        {
            activeLoopingUpgradeList.Clear();
            activeLoopingUpgradeListArray = new Charm[0];
            activeUpgradeList.Clear();
            weight = 0;
            if (Utilities.IsValid(localPlayer))
            {
                localPlayer.bag.totalPrice = 0;
            }
        }

        VRC_Pickup currentPickup;
        public void DropUpgrades()
        {
            currentPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            if (Utilities.IsValid(currentPickup))
            {
                currentPickup.Drop();
            }
            currentPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.None);
            if (Utilities.IsValid(currentPickup))
            {
                currentPickup.Drop();
            }
            currentPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (Utilities.IsValid(currentPickup))
            {
                currentPickup.Drop();
            }
            ClearUpgrades();
            localPlayer.bag.ExplodeChildren(false);
        }

        public float inventoryInterpolationTime = 0.25f;
        public Vector3 desktopInventoryPlacement = new Vector3(0, -0.25f, 0.5f);
        public float inventorySpacing = 0.05f;
        public float desktopInventorySpacing = 0.25f;

    }
}