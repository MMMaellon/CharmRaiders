
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
            bag.backpackAttachment.sync.rigid.detectCollisions = Owner != null && Owner.isLocal;
            if (Owner != null && Owner.isLocal)
            {
                Networking.SetOwner(Networking.LocalPlayer, bag.gameObject);
                bag.backpackAttachment.EnterState();
                bag.backpackAttachment.sync.RequestSerialization();
            }
            bag.backpackAttachment.sync.StartInterpolation();
        }
        public override void _OnCleanup()
        {
        }
    }
}