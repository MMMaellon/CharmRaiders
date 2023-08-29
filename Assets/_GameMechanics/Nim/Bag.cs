
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

        public TMPro.TextMeshProUGUI priceText;
        public TMPro.TextMeshProUGUI weightText;
        public int _totalPrice;
        public int _totalWeight;
        public int totalPrice{
            get => _totalPrice;
            set {
                _totalPrice = value;
                priceText.text = "$" + value;
            }
        }
        public int totalWeight{
            get => _totalWeight;
            set {
                _totalWeight = value;
                weightText.text = value.ToString();
            }
        }
        public void Start()
        {
            _localplayer = Networking.LocalPlayer;
            totalPrice = totalPrice;
            totalWeight = totalWeight;
        }

        public void NetworkReadyCheck()
        {
            if (!Networking.IsObjectReady(gameObject))
            {
                SendCustomEventDelayedFrames(nameof(NetworkReadyCheck), 1);
            } else
            {
                backpackAttachment.sync.StartInterpolation();
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
            SendCustomEventDelayedFrames(nameof(NetworkReadyCheck), 1);
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
                child.sync.rigid.velocity = (transform.position - child.transform.position).normalized * explodeVelocity;
                child.sync.rigid.angularVelocity = child.sync.rigid.velocity;
                child.ExitState();
            }
        }
    }
}