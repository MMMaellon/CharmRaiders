
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class PortalTeleport : UdonSharpBehaviour
    {
        Player player;

        public Animator animator;
        public AudioSource audioSource;
        public AudioClip portalOnSound;
        public AudioClip portalOffSound;
        public AudioClip portalEnterSound;
        public virtual void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            player = (Player)other.GetComponent<Player>();
            if (!Utilities.IsValid(other) || !Utilities.IsValid(player) || !player.IsOwnerLocal())
            {
                return;
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnterAnimation));
            player.Spawn();
        }

        public void DisableAnimator()
        {
            animator.enabled = false;
        }

        public void EnterAnimation()
        {
            animator.enabled = true;
            animator.SetTrigger("enter");
            audioSource.clip = portalEnterSound;
            audioSource.Play();
        }
        public void PortalAnimation(bool value)
        {
            animator.enabled = true;
            animator.SetBool("portal", value);
            if (value)
            {
                audioSource.PlayOneShot(portalOnSound);
            }
            else
            {
                audioSource.PlayOneShot(portalOffSound);
            }
        }

        public void OpenPortal()
        {
            PortalAnimation(true);
        }
    }
}