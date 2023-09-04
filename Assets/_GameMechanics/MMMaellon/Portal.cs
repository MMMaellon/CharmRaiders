
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Portal : PortalTeleport
    {
        public TMPro.TextMeshProUGUI playerList;
        public TMPro.TextMeshProUGUI scoreText;
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

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(points))]
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
            UpdateScoreText();
            if (pointsTextValue != points)
            {
                SendCustomEventDelayedFrames(nameof(updatePointsText), 5);
            }
        }
        public ParticleSystem[] particles;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(portal))]
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
            if (Utilities.IsValid(playerList))
            {
                playerList.text = "";
            }
            if (Utilities.IsValid(scoreText))
            {
                scoreText.text = "$0";
            }

            color = color;
            portal = portal;

            rigid.centerOfMass = rigid.GetComponent<CapsuleCollider>().center;
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

        string playerListBuilder;
        public void UpdatePlayerListText()
        {
            if (!Utilities.IsValid(playerList))
            {
                return;
            }
            playerListBuilder = "";
            foreach (Player p in game.playerHandler.players)
            {
                if (Utilities.IsValid(p.Owner) && p.gameObject.activeSelf && p.portalIndex == index)
                {
                    playerListBuilder += p.Owner.displayName + "\n";
                }
            }
            playerList.text = playerListBuilder.ToString();
        }

        public void UpdateScoreText()
        {
            Debug.LogWarning("Update Score Text");
            if (!Utilities.IsValid(scoreText))
            {
                return;
            }
            Debug.LogWarning("should be happening rn");
            scoreText.text = "$" + pointsTextValue;
        }
    }
}
