
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CharmPlayerListener : P_Shooters.PlayerListener
    {
        public float respawnInvincibilityTime = 3f;
        public AudioSource receiveDamageAudio;
        public AudioSource dealDamageAudio;
        public AudioSource onKillConfirmedAudio;

        [System.NonSerialized]
        public float[] respawnTimes;
        public P_Shooters.ResourceManager armorResource;
        public P_Shooters.ResourceManager armorPenetrationResource;
        public P_Shooters.ResourceManager damageBoostResource;
        void Start()
        {
            respawnTimes = new float[playerHandler.players.Length];
            for (int i = 0; i < respawnTimes.Length; i++)
            {
                respawnTimes[i] = -1001f;
            }
        }

        public override bool CanDealDamage(P_Shooters.Player attacker, P_Shooters.Player receiver)
        {
            if (gameObject.activeInHierarchy && respawnTimes[receiver.id] + respawnInvincibilityTime >= Time.timeSinceLevelLoad)
            {
                return false;
            }
            return true;
        }

        public override void OnDecreaseHealth(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
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
        public override void OnDecreaseShield(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
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

        public override void OnMinHealth(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
        {
            if (gameObject.activeInHierarchy && receiver.IsOwnerLocal())
            {
                receiver.Owner.Respawn();
                respawnTimes[receiver.id] = Time.timeSinceLevelLoad;
                attacker.ConfirmNormalKill();
                receiver.ResetPlayer();
            }
        }

        public override void OnReceiveNormalKillConfirmation(P_Shooters.Player attacker)
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
        public override int AdjustDamage(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
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