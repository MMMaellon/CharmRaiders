
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
        public Collider col;
        public AudioClip[] collisionClips;
        public float minVolume = 0;
        public float maxVolume = 1;
        public float minSpeed = 0.1f;
        public float maxSpeed = 3f;

        float radius = 1;
        float lastEnable = -1001f;
        public void OnEnable()
        {
            transform.rotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            lastPos = transform.position;
            lastEnable = Time.timeSinceLevelLoad;
            rollingSound.volume = 0;
            col.enabled = true;
        }

        public void OnDisable()
        {
            rollingSound.volume = 0;
            col.enabled = false;
        }

        Vector3 lastPos;
        Quaternion lastRot;
        float volume;
        bool wasInAir;
        float lerpTime = 0.25f;
        float lerp = 0f;
        Vector3 lastLossyScale;
        public void LateUpdate()
        {
            if (Utilities.IsValid(player))
            {
                transform.rotation = lastRot;
                radius = 1.5f *  player.capsuleCollider.height / 2f / transform.parent.lossyScale.x;//half distance to get radius, multiplied by a ratio so it's bigger than the player's avatar
                if (radius == 0 || lastEnable == 0)
                {
                    return;
                }
                lerp = (Time.timeSinceLevelLoad - lastEnable) / lerpTime;
                transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(radius, radius, radius), lerp);
                transform.position = Vector3.Lerp(transform.parent.position, player.transform.position, lerp);
                transform.Rotate((transform.position.z - lastPos.z) * 180 / radius, 0, (transform.position.x - lastPos.x) * -180 / radius, Space.World);
                if (!Utilities.IsValid(player.Owner) || !player.Owner.IsPlayerGrounded())
                {
                    volume = 0;
                    wasInAir = true;
                }
                else
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
                lastLossyScale = transform.lossyScale;
            } else if (lerp > 0)
            {
                col.enabled = false;
                if (radius == 0)
                {
                    return;
                }
                if (lerp > 1)
                {
                    lerp = 1;
                }
                lerp -= (Time.deltaTime) / lerpTime;
                transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), lastLossyScale / transform.parent.lossyScale.x, lerp);
                transform.position = Vector3.Lerp(transform.parent.position, lastPos, lerp);
            } else
            {
                transform.localScale = new Vector3(1, 1, 1);
                transform.localPosition = Vector3.zero;
                enabled = false;
            }
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
        P_Shooters.LocalEventTrigger trigger;
        public void OnParticleCollision(GameObject other)
        {
            if (!Utilities.IsValid(player))
            {
                return;
            }
            PlayCollisionSound();
            player.OnParticleCollision(other);
            trigger = other.GetComponent<P_Shooters.LocalEventTrigger>();
            if (Utilities.IsValid(trigger))
            {
                trigger.OnParticleCollision(player.gameObject);
            }
        }
    }
}