
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HamsterBallPickup : Upgrade
    {
        public int armor = 10;
        public override void StartUpgrade()
        {
            player.eventAnimator.SetFloat("HamsterBall", 1);
            if(player != null && player.IsOwnerLocal()){
                charm.tracker.charmListener.armorResource.ChangeValue(player, armor);
            }
        }

        public override void StopUpgrade()
        {
            player.eventAnimator.SetFloat("HamsterBall", 0);
            if(player != null && player.IsOwnerLocal()){
                charm.tracker.charmListener.armorResource.ChangeValue(player, -armor);
            }
        }
        public override void UpgradeLoop()
        {
            //unused
        }
    }
}