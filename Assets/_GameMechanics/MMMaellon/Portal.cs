
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Portal : PortalTeleport
    {
        [HideInInspector]
        public GameHandler game;
        [UdonSynced]
        public Color _color;
        public int index = -1001;
        ParticleSystem.MainModule main;
        float h;
        float s;
        float v;
        public Color color
        {
            get => _color;
            set
            {
                _color = value;
                foreach (ParticleSystem particle in particles)
                {
                    main = particle.main;
                    main.startColor = value;
                }
            }
        }

        [UdonSynced(UdonSyncMode.None)]
        public int _points;
        public int points{
            get => _points;
            set
            {
                if (_points != value)
                {
                    if (pointsTextValue == _points)
                    {
                        //value has changed and there was not an update loop already going because the value was the same
                        SendCustomEventDelayedFrames(nameof(updatePointsText), 0);
                    }
                    _points = value;
                    if(Networking.LocalPlayer.IsOwner(gameObject)){
                        RequestSerialization();
                    }
                }
            }
        }
        int pointsTextValue;
        public TMPro.TextMeshProUGUI pointsText;
        public void updatePointsText()
        {
            if (pointsTextValue < points)
            {
                pointsTextValue++;
            } else if (pointsTextValue > points)
            {
                pointsTextValue--;
            }
            pointsTextValue = Mathf.RoundToInt(Mathf.Lerp(pointsTextValue, points, 0.1f));
            pointsText.text = "$" + pointsTextValue;
            if (pointsTextValue != points)
            {
                SendCustomEventDelayedFrames(nameof(updatePointsText), 5);
            }
        }
        public ParticleSystem[] particles;
        [UdonSynced(UdonSyncMode.None)]
        public bool _portal = false;
        public bool portal{
            get => _portal;
            set
            {
                PortalAnimation(value);
                _portal = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        void Start()
        {
            color = color;
            portal = portal;
        }

        Charm otherCharm;

        public override void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            otherCharm = other.GetComponent<Charm>();
            if (!Utilities.IsValid(otherCharm))
            {
                return;
            }
            EnterAnimation();
            if (!otherCharm.bagSetter.child.sync.IsOwnerLocal() || otherCharm.bagSetter.child.sync.state > SmartObjectSync.STATE_NO_HAND_HELD || otherCharm.bagSetter.child.sync.state < 0 || game.localPlayer.portal != this)
            {
                return;
            }
            otherCharm.portalIndex = index;
        }

        public void PortalOn()
        {
            portal = true;
        }
        public void PortalOff()
        {
            portal = false;
        }

        public void JoinPortal()
        {
            if (!Utilities.IsValid(game.localPlayer))
            {
                return;
            }
            if (game.state == GameHandler.STATE_MATCHMAKING || game.state == GameHandler.STATE_GAME_START_COUNTDOWN)
            {
                game.localPlayer.portalIndex = index;
            }
        }

        public void LeavePortal()
        {
            if (!Utilities.IsValid(game.localPlayer))
            {
                return;
            }
            game.localPlayer.portalIndex = -1;
        }
    }
}
