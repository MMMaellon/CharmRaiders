
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
                Networking.LocalPlayer.SetRunSpeed((baseRunSpeed * weightCurve) / (_weight + weightCurve));
                Networking.LocalPlayer.SetStrafeSpeed((baseRunSpeed * weightCurve) / (_weight + weightCurve));
                Networking.LocalPlayer.SetWalkSpeed((baseWalkSpeed * weightCurve) / (_weight + weightCurve));
                if (Utilities.IsValid(localPlayer))
                {
                    localPlayer.bag.totalWeight = value;
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
        }

        public void AddCharm(Charm upgrade)
        {
            if (!activeUpgradeList.Contains(upgrade))
            {
                weight = weight + upgrade.weight;
                if (Utilities.IsValid(localPlayer))
                {
                    localPlayer.bag.totalPrice = localPlayer.bag.totalPrice + upgrade.price;
                }
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
            Debug.LogWarning("Remove Upgrade");
            if (activeUpgradeList.Contains(upgrade))
            {
                Debug.LogWarning("Remove Upgrade -- it contains");
                weight = weight - upgrade.weight;
                if (Utilities.IsValid(localPlayer))
                {
                    localPlayer.bag.totalPrice = localPlayer.bag.totalPrice - upgrade.price;
                }
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
            weight = weight;
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
            }
        }
        public void TestRespawnObjects()
        {
            foreach (CharmPool pool in charmPools)
            {
                pool.UnspawnAll();
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
        public float inventoryInterpolationTime = 0.25f;
        public Vector3 desktopInventoryPlacement = new Vector3(0, -0.25f, 0.5f);
        public float inventorySpacing = 0.05f;
        public float desktopInventorySpacing = 0.25f;

        VRC_Pickup currentPickup;
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
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
                ((Player)playerHandler.localPlayer).bag.ExplodeChildren(false);
            }
        }
    }
}