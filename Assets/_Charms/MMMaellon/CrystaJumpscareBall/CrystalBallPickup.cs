
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CrystalBallPickup : Upgrade
    {
        public CrystalBallJumpscareListener jumpscareListener;

        public void Start()
        {
            jumpscareListener.gameObject.SetActive(false);
        }

        public override void StartUpgrade()
        {
            Debug.LogWarning("StartUpgrade on CrystallBallPickup");
            if (player != null && player.IsOwnerLocal())
            {
                jumpscareListener.gameObject.SetActive(true);
                jumpscareListener.ActivateListener();
            }
        }

        public override void StopUpgrade()
        {
            jumpscareListener.gameObject.SetActive(false);
        }
        public override void UpgradeLoop()
        {
            //unused
        }
    }
}
