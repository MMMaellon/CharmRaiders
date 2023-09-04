
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{
    public class RifleCharm : Charm
    {
        public override void CharmLoop()
        {
            
        }

        public override void StartCharmEffects()
        {
            if (Utilities.IsValid(player) && player.IsOwnerLocal())
            {
                game.localPlayer.gun.rifle = true;
            }
        }

        public override void StopCharmEffects()
        {
            if (Utilities.IsValid(player) && player.IsOwnerLocal())
            {
                game.localPlayer.gun.rifle = false;
            }
        }
    }
}