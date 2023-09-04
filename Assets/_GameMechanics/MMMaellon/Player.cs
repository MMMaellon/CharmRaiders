
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class Player : P_Shooters.Player
    {

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(portalIndex))]
        public int _portalIndex = -1001;
        bool foundPortalOwner;
        int activePortalCount;
        public int portalIndex{
            get => _portalIndex;
            set
            {
                if (Utilities.IsValid(portal))
                {
                    portal.animator.enabled = true;
                    portal.animator.SetBool("local", false);
                    portal.color = portal.color;
                    portal.EnterAnimation();
                    if (Networking.LocalPlayer.IsOwner(portal.gameObject) && portal.portal)
                    {
                        foundPortalOwner = false;
                        foreach (Player p in playerHandler.players)
                        {
                            if (p.gameObject.activeSelf && p.portalIndex == portal.index && p != this)
                            {
                                foundPortalOwner = true;
                                p.RequestTakeOwnerShipOfPortal();
                                break;
                            }
                        }
                        if (!foundPortalOwner)
                        {
                            portal.portal = false;
                            if (!game.skipTeamCheckForDebug)
                            {
                                activePortalCount = 0;
                                foreach (Portal p in game.portals)
                                {
                                    if (p.portal)
                                    {
                                        activePortalCount++;
                                        if (activePortalCount > 1)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (activePortalCount <= 1)
                                {
                                    //Not enough people still playing
                                    game.RequestEndGame();
                                }
                            }
                        }
                    }
                }
                _portalIndex = value;
                statsAnimator.SetInteger("team", value);
                if (Utilities.IsValid(portal))
                {
                    portal.UpdatePlayerListText();
                }
                if (value < 0 || value >= game.portals.Length)
                {
                    portal = null;
                    ResetPlayer();
                } else
                {
                    portal = game.portals[value];
                    portal.UpdatePlayerListText();
                    if (IsOwnerLocal())
                    {
                        portal.animator.enabled = true;
                        portal.animator.SetBool("local", true);
                        portal.color = portal.color;
                        portal.EnterAnimation();
                        if (!portal.portal)
                        {
                            Networking.SetOwner(Networking.LocalPlayer, portal.gameObject);
                            portal.PortalOn();
                        }
                    }
                    else
                    {

                    }
                }
                if (IsOwnerLocal())
                {
                    RequestSerialization();
                }
            }
        }

        [HideInInspector]
        public GameHandler game;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(points))]
        public int _points;
        public int points{
            get => _points;
            set
            {
                Debug.LogWarning("setting points on player to " + value);
                if (value > _points && Utilities.IsValid(portal) && Networking.LocalPlayer.IsOwner(portal.gameObject))
                {
                    Debug.LogWarning("portal is valid, we are increasing portal points by " + (value - _points));
                    portal.points += value - _points;
                }
                _points = value;
                if (IsOwnerLocal())
                {
                    RequestSerialization();
                }
            }
        }

        public void ResetPlayerAndPoints()
        {
            ResetPlayer();
            points = 0;
        }

        public void RequestTakeOwnerShipOfPortal()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(TakeOwnershipOfPortal));
        }

        public void TakeOwnershipOfPortal()
        {
            if (Utilities.IsValid(portal))
            {
                Networking.SetOwner(Networking.LocalPlayer, portal.gameObject);
                portal.portal = true;
            }
        }
        public Portal portal;
        public Bag bag;
        public PlayerGunModeSetter gun;
        public override void _OnOwnerSet()
        {
            base._OnOwnerSet();
            bag.backpackAttachment.sync.rigid.detectCollisions = Owner != null && Owner.isLocal;
            if (Owner != null && Owner.isLocal)
            {
                Networking.SetOwner(Networking.LocalPlayer, bag.gameObject);
                bag.backpackAttachment.EnterState();
                bag.backpackAttachment.sync.RequestSerialization();
                LocalLoop();
            }
            bag.backpackAttachment.sync.StartInterpolation();
        }
        public override void _OnCleanup()
        {

        }

        public void Spawn()
        {
            if (!IsOwnerLocal() || !Utilities.IsValid(portal))
            {
                return;
            }
            eventAnimator.SetTrigger("spawn");
            if (game.state != GameHandler.STATE_GAME_IN_PROGRESS)
            {
                Owner.TeleportTo(game.teleportPortal.transform.position, game.teleportPortal.transform.rotation);
            }
            else
            {
                Owner.TeleportTo(portal.transform.position, portal.transform.rotation);
            }
            portal.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnterAnimation");
        }

        public void LocalLoop()
        {
            SendCustomEventDelayedFrames(nameof(LocalLoop), 1);
            if (Utilities.IsValid(portal))
            {
                portal.pointsText.transform.rotation = Quaternion.LookRotation(portal.pointsText.transform.position - headPos);
            }
        }
    }
}