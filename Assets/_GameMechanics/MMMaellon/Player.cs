
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class Player : P_Shooters.Player
    {
        public Bag bag;
        public override void _OnOwnerSet()
        {
            base._OnOwnerSet();
            bag.playerBackAttachment.sync.rigid.detectCollisions = Owner != null && Owner.isLocal;
            if (Owner != null && Owner.isLocal)
            {
                Networking.SetOwner(Networking.LocalPlayer, bag.gameObject);
                bag.playerBackAttachment.EnterState();
                bag.playerBackAttachment.sync.RequestSerialization();
            }
            bag.playerBackAttachment.sync.StartInterpolation();
        }
        public override void _OnCleanup()
        {
        }
    }
}