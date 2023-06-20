
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace MMMaellon
{
    [RequireComponent(typeof(BackpackAttachment))]
    public class Bag : UdonSharpBehaviour
    {
        public BackpackAttachment backpackAttachment;
        public Player player;
        public Animator animator;
        public float explodeVelocity = 5f;
        VRCPlayerApi _localplayer;
        public void Start()
        {
            _localplayer = Networking.LocalPlayer;
        }
        public void EjectAll()
        {
            foreach (ChildAttachmentState child in GetComponentsInChildren<ChildAttachmentState>())
            {
                child.ExitState();
            }
        }

        public void Unzip()
        {
            animator.SetBool("zip", false);
            foreach (ChildAttachmentState child in GetComponentsInChildren<ChildAttachmentState>())
            {
                child.sync.rigid.detectCollisions = true;
            }
            if (!_localplayer.IsUserInVR())
            {
                GetComponent<VRC_Pickup>().pickupable = false;
            }
        }

        public void Zip()
        {
            animator.SetBool("zip", true);
            foreach (ChildAttachmentState child in GetComponentsInChildren<ChildAttachmentState>())
            {
                child.sync.rigid.detectCollisions = false;
            }
        }

        public override void OnPickup()
        {
            Unzip();
        }

        public override void OnDrop()
        {
            Zip();
        }

        public void OnEnable()
        {
            animator.SetBool("zip", true);
        }

        public void OnDisable(){
            ExplodeChildren(true);
        }

        public void ExplodeChildren(bool checkOwnership)
        {
            foreach (ChildAttachmentState child in GetComponentsInChildren<ChildAttachmentState>())
            {
                if (checkOwnership && !_localplayer.IsOwner(child.gameObject))
                {
                    continue;
                }
                child.sync.rigid.velocity = (child.transform.position - child.sync.parentPos).normalized * explodeVelocity;
                child.sync.rigid.angularVelocity = child.sync.rigid.velocity;
                child.ExitState();
            }
        }
    }
}