
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HamsterBallPickup : Upgrade
    {
        public HamsterBall hamsterBallEffect;
        public int armor = 10;
        public override void StartUpgrade()
        {
            hamsterBallEffect.player = player;
            hamsterBallEffect.enabled = true;
            if(player != null && player.IsOwnerLocal()){
                bagSetter.tracker.charmListener.armorResource.ChangeValue(player, armor);
            }
        }

        public override void StopUpgrade()
        {
            hamsterBallEffect.player = null;
            if(player != null && player.IsOwnerLocal()){
                bagSetter.tracker.charmListener.armorResource.ChangeValue(player, -armor);
            }
        }
        public override void UpgradeLoop()
        {
            //unused
        }
    }
}