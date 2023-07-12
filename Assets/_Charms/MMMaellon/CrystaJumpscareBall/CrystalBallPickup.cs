
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CrystalBallPickup : Charm
    {
        public CrystalBallJumpscareListener jumpscareListener;

        public override void Start()
        {
            jumpscareListener.gameObject.SetActive(false);
        }

        public override void StartCharmEffects()
        {
            Debug.LogWarning("StartUpgrade on CrystallBallPickup");
            if (player != null && player.IsOwnerLocal())
            {
                jumpscareListener.gameObject.SetActive(true);
                jumpscareListener.ActivateListener();
            }
        }

        public override void StopCharmEffects()
        {
            jumpscareListener.gameObject.SetActive(false);
        }
        public override void CharmLoop()
        {
            //unused
        }
    }
}
