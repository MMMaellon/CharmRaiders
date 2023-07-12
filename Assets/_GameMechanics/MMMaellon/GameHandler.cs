
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{
    
    public class GameHandler : UdonSharpBehaviour
    {
        public Portal[] portals;
        public CharmPlayerListener charmListener;
        public UpgradeTracker tracker;
        public MMMaellon.P_Shooters.P_ShootersPlayerHandler playerHandler;
        Player _localPlayer = null;
        public Player localPlayer{
            get
            {
                if (!Utilities.IsValid(_localPlayer))
                {
                    _localPlayer = (Player)playerHandler.localPlayer;

                }
                return _localPlayer;
            }
        }
        void Start()
        {

        }
    }
}