
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(AudioSource))]
    public class HamsterBall : UdonSharpBehaviour
    {
        public Player player;
        public AudioSource rollingSound;
        public AudioSource collisionSound;
        public AudioClip[] collisionClips;
        public float minVolume = 0;
        public float maxVolume = 1;
        public float minSpeed = 0.1f;
        public float maxSpeed = 3f;

        float radius = 1;
        public void OnEnable()
        {
            transform.rotation = Quaternion.identity;
            lastPos = transform.position;
            transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
        }

        Vector3 lastPos;
        Quaternion lastRot;
        float volume;
        bool wasInAir;
        public void LateUpdate()
        {
            transform.rotation = lastRot;
            radius = Vector3.Distance(transform.position, player.feetPos);
            if (radius == 0)
            {
                return;
            }
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(radius, radius, radius), 0.1f);
            transform.Rotate((transform.position.z - lastPos.z) * 180 / radius, 0, (transform.position.x - lastPos.x) * -180 / radius, Space.World);
            if(!Utilities.IsValid(player.Owner) || !player.Owner.IsPlayerGrounded())
            {
                volume = 0;
                wasInAir = true;
            } else
            {
                volume = Mathf.Lerp(minVolume, maxVolume, ((Vector3.Distance(lastPos, transform.position) / Time.deltaTime) - minSpeed) / (maxSpeed - minSpeed));
                if (wasInAir)
                {
                    wasInAir = false;
                    rollingSound.volume = 2.0f;
                    PlayCollisionSound();
                }
            }
            rollingSound.volume = Mathf.Lerp(rollingSound.volume, volume, 0.1f);
            lastPos = transform.position;
            lastRot = transform.rotation;
        }

        public void PlayCollisionSound()
        {
            collisionSound.PlayOneShot(collisionClips[Random.Range(0, collisionClips.Length)]);
        }

        //collision enter event
        public void OnCollisionEnter(Collision collision)
        {
            PlayCollisionSound();
        }

        //particle collision event
        LocalEventTrigger trigger;
        public void OnParticleCollision(GameObject other)
        {
            PlayCollisionSound();
            player.OnParticleCollision(other);
            trigger = other.GetComponent<LocalEventTrigger>();
            if (Utilities.IsValid(trigger))
            {
                trigger.OnParticleCollision(player.gameObject);
            }
        }
    }
}