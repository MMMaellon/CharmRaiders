
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CharmPlayerListener : PlayerListener
    {
        public float respawnInvincibilityTime = 3f;
        public AudioSource receiveDamageAudio;
        public AudioSource dealDamageAudio;
        public AudioSource onKillConfirmedAudio;

        [System.NonSerialized]
        public float[] respawnTimes;
        public ResourceManager armorResource;
        public ResourceManager armorPenetrationResource;
        public ResourceManager damageBoostResource;
        void Start()
        {
            respawnTimes = new float[playerHandler.players.Length];
            for (int i = 0; i < respawnTimes.Length; i++)
            {
                respawnTimes[i] = -1001f;
            }
        }

        public override bool CanDealDamage(Player attacker, Player receiver)
        {
            if (gameObject.activeInHierarchy && respawnTimes[receiver.id] + respawnInvincibilityTime >= Time.timeSinceLevelLoad)
            {
                return false;
            }
            return true;
        }

        public override void OnDecreaseHealth(Player attacker, Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                if (receiver.IsOwnerLocal())
                {
                    if (Utilities.IsValid(receiveDamageAudio))
                    {
                        receiveDamageAudio.transform.position = receiver.transform.position;
                        receiveDamageAudio.Play();
                    }
                } else if (attacker.IsOwnerLocal())
                {
                    if (Utilities.IsValid(dealDamageAudio))
                    {
                        dealDamageAudio.transform.position = receiver.transform.position;
                        dealDamageAudio.Play();
                    }
                }
            }
        }
        public override void OnDecreaseShield(Player attacker, Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                if (receiver.IsOwnerLocal())
                {
                    if (Utilities.IsValid(receiveDamageAudio))
                    {
                        receiveDamageAudio.transform.position = receiver.transform.position;
                        receiveDamageAudio.Play();
                    }
                }
                else if (attacker.IsOwnerLocal())
                {
                    if (Utilities.IsValid(dealDamageAudio))
                    {
                        dealDamageAudio.transform.position = receiver.transform.position;
                        dealDamageAudio.Play();
                    }
                }
            }
        }

        public override void OnMinHealth(Player attacker, Player receiver, int value)
        {
            if (gameObject.activeInHierarchy && receiver.IsOwnerLocal())
            {
                receiver.Owner.Respawn();
                respawnTimes[receiver.id] = Time.timeSinceLevelLoad;
                attacker.ConfirmNormalKill();
                receiver.ResetPlayer();
            }
        }

        public override void OnReceiveNormalKillConfirmation(Player attacker)
        {
            if (gameObject.activeInHierarchy && Utilities.IsValid(onKillConfirmedAudio))
            {
                receiveDamageAudio.transform.position = attacker.transform.position;
                receiveDamageAudio.Play();
            }
        }

        int armor;
        int armorPenetration;
        int damageBoost;
        public override int AdjustDamage(Player attacker, Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                armor = receiver.GetResourceValueById(armorResource.id);

                if(armor > 0 && attacker != receiver)
                {
                    armorPenetration = attacker.GetResourceValueById(armorPenetrationResource.id);
                    if (armorPenetration > armor)
                    {
                        armor = 0;
                    }
                    else
                    {
                        armor -= armorPenetration;
                    }
                }
                damageBoost = attacker.GetResourceValueById(damageBoostResource.id);
                return armor > -10 ? damageBoost + Mathf.FloorToInt(value / ((0.1f * armor) + 1)) : -1001;//insta kill if your armor is too far into the negative
            }
            return value;
        }

    }
}