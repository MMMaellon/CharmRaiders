
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CharmPlayerListener : P_Shooters.PlayerListener
    {
        [HideInInspector]
        public GameHandler game;
        public float respawnDelayTime = 2f;
        public float respawnInvincibilityTime = 3f;
        public AudioSource receiveDamageAudio;
        public AudioSource onDieAudio;
        public AudioSource onKillConfirmedAudio;

        [System.NonSerialized]
        public float[] respawnTimes;
        public P_Shooters.ResourceManager armorResource;
        public P_Shooters.ResourceManager armorPenetrationResource;
        public P_Shooters.ResourceManager damageBoostResource;
        public P_Shooters.ResourceManager pointsListener;
        Player p;
        [UdonSynced(UdonSyncMode.None)]
        public bool _gameActive = false;
        public bool gameActive{
            get => _gameActive;
            set
            {
                _gameActive = value;
                //game either ended or started. We refresh.
                if (Utilities.IsValid(playerHandler.localPlayer))
                {
                    playerHandler.localPlayer.SetResourceValueById(pointsListener.id, 0);
                    p = playerHandler.localPlayer.GetComponent<Player>();
                    if (!Utilities.IsValid(p.portal) && Networking.LocalPlayer.IsOwner(p.portal.gameObject))
                    {
                        p.portal.points = 0;
                    }
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        void Start()
        {
            respawnTimes = new float[playerHandler.players.Length];
            for (int i = 0; i < respawnTimes.Length; i++)
            {
                respawnTimes[i] = -1001f;
            }

            Networking.LocalPlayer.CombatSetup();
            Networking.LocalPlayer.CombatSetRespawn(true, respawnDelayTime, null);
            Networking.LocalPlayer.CombatSetMaxHitpoints(100f);
            Networking.LocalPlayer.CombatSetCurrentHitpoints(100f);
            Networking.LocalPlayer.CombatSetDamageGraphic(null);
        }

        public override bool CanDealDamage(P_Shooters.Player attacker, P_Shooters.Player receiver)
        {
            if (gameObject.activeInHierarchy && respawnTimes[receiver.id] + respawnDelayTime + respawnInvincibilityTime >= Time.timeSinceLevelLoad)
            {
                return false;
            }
            return gameActive;
        }

        public override void OnDecreaseHealth(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                if (receiver.IsOwnerLocal())
                {
                    receiveDamageAudio.transform.position = receiver.transform.position;
                    receiveDamageAudio.Play();
                }
            }
        }
        public override void OnDecreaseShield(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                if (receiver.IsOwnerLocal())
                {
                    receiveDamageAudio.transform.position = receiver.transform.position;
                    receiveDamageAudio.Play();
                }
            }
        }

        public override void OnMinHealth(P_Shooters.Player attacker, P_Shooters.Player receiver, int value)
        {
            if (gameObject.activeInHierarchy)
            {
                if (receiver.IsOwnerLocal())
                {
                    localPlayer = (Player)receiver;
                    localPlayer.bag.ExplodeChildren(false);
                    onDieAudio.Play();
                    if (Utilities.IsValid(attacker) && !attacker.IsOwnerLocal())
                    {
                        attacker.ConfirmNormalKill();
                    }
                    SendCustomEventDelayedSeconds(nameof(RespawnCallback), respawnDelayTime, VRC.Udon.Common.Enums.EventTiming.Update);
                    respawnTimes[receiver.id] = Time.timeSinceLevelLoad;
                    receiver.Owner.CombatSetCurrentHitpoints(0);
                    receiver.state = Player.STATE_DEAD;
                }
            }
        }

        public override void OnReceiveNormalKillConfirmation(P_Shooters.Player attacker)
        {
            if (gameObject.activeInHierarchy && Utilities.IsValid(onKillConfirmedAudio))
            {
                onKillConfirmedAudio.transform.position = attacker.transform.position;
                onKillConfirmedAudio.Play();
            }
        }

        int armor;
        int armorPenetration;
        int damageBoost;
        Player localPlayer;
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

        public void RespawnCallback()
        {
            //we have to make sure this gets called on a normal update loop
            game.tracker.DropUpgrades();
            localPlayer.Spawn();
            localPlayer.ResetPlayer();
            localPlayer.state = Player.STATE_INVINCIBLE;
            SendCustomEventDelayedSeconds(nameof(InvincinbilityOffCallback), respawnInvincibilityTime);
        }
        public void InvincinbilityOffCallback()
        {
            if (localPlayer.state == Player.STATE_INVINCIBLE && Mathf.Abs(respawnTimes[localPlayer.id] + respawnInvincibilityTime + respawnDelayTime - Time.timeSinceLevelLoad) < 0.25f )
            {
                localPlayer.state = Player.STATE_NORMAL;
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            base.OnPlayerJoined(player);
            player.CombatSetup();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                game.tracker.DropUpgrades();
            }
        }

    }
}