
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace MMMaellon
{
    
    public class GameHandler : UdonSharpBehaviour
    {
        public PortalTeleport teleportPortal;
        public Portal[] portals;
        public CharmPlayerListener charmListener;
        public UpgradeTracker tracker;
        public MMMaellon.P_Shooters.P_ShootersPlayerHandler playerHandler;
        Player _localPlayer = null;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(error))]
        public string _error = "";
        [System.NonSerialized]
        public bool skipTeamCheckForDebug = true;
        public string error
        {
            get => _error;
            set
            {
                _error = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                //SET ERROR TEXT HERE
            }
        }
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(state))]
        public int _state = 0;
        public Animator gameAnimator;
        public int state
        {
            get => _state;
            set
            {
                switch (_state)
                {
                    case STATE_MATCHMAKING:
                        {
                            OnStopMatchmaking();
                            break;
                        }
                    case STATE_GAME_START_COUNTDOWN:
                        {
                            OnStopGameCountdown();
                            break;
                        }
                    case STATE_GAME_IN_PROGRESS:
                        {
                            OnStopGame();
                            break;
                        }
                    case STATE_GAME_END:
                        {
                            OnStopGameEnd();
                            break;
                        }
                    case STATE_GAME_ERROR:
                        {
                            OnStopGameError();
                            break;
                        }
                }
                _state = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }

                gameAnimator.SetInteger("state", value);

                switch (value)
                {
                    case STATE_MATCHMAKING:
                        {
                            OnStartMatchmaking();
                            break;
                        }
                    case STATE_GAME_START_COUNTDOWN:
                        {
                            OnStartGameCountdown();
                            break;
                        }
                    case STATE_GAME_IN_PROGRESS:
                        {
                            OnStartGame();
                            break;
                        }
                    case STATE_GAME_END:
                        {
                            OnStartGameEnd();
                            break;
                        }
                    case STATE_GAME_ERROR:
                        {
                            OnStartGameError();
                            break;
                        }
                }
            }
        }
        public const int STATE_MATCHMAKING = 0;
        public const int STATE_GAME_START_COUNTDOWN = 1;
        public const int STATE_GAME_IN_PROGRESS = 2;
        public const int STATE_GAME_END = 3;
        public const int STATE_GAME_ERROR = 4;
        public Player localPlayer{
            get
            {
                if (!Utilities.IsValid(_localPlayer))
                {
                    _localPlayer = (Player)playerHandler.localPlayer;

                }
                return _localPlayer;
            }
        }
        void Start()
        {
            state = state;
        }

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(masterOnly))]
        public bool _masterOnly = false;
        public bool masterOnly
        {
            get => _masterOnly;
            set
            {
                _masterOnly = value;
                gameAnimator.SetBool("masterOnly", value);
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(gameDuration))]
        public float _gameDuration = 180f;

        public float gameDuration
        {
            get => _gameDuration;
            set
            {
                _gameDuration = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public void StartGame()
        {
            if (state == STATE_MATCHMAKING)
            {
                if (Networking.LocalPlayer.IsOwner(gameObject)) {
                    StartGameCallback();
                } else if (!masterOnly) {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(StartGameCallback));
                }
            }
        }

        int activePortalCount;
        public void StartGameCallback()
        {
            if (state == STATE_MATCHMAKING)
            {
                tracker.SpawnCharms();
                state = STATE_GAME_START_COUNTDOWN;
            }
        }

        //called by animator?
        public void EndGameCountdownCallback()
        {
            if (state == STATE_GAME_START_COUNTDOWN && Networking.LocalPlayer.IsOwner(gameObject))
            {
                activePortalCount = 0;
                foreach (Portal p in portals)
                {
                    if (p.portal)
                    {
                        activePortalCount++;
                        if (activePortalCount > 1)//at least two teams
                        {
                            break;
                        }
                    }
                }
                if (activePortalCount > 1 || skipTeamCheckForDebug)
                {
                    state = STATE_GAME_IN_PROGRESS;
                }
                else
                {
                    error = STRING_TEAM_ERROR;
                    state = STATE_GAME_ERROR;
                }
            }
        }

        public void RequestEndGame()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(EndGame));
        }
        public void EndGame()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (state == STATE_GAME_IN_PROGRESS)
                {
                    state = STATE_GAME_END;
                }
                else
                {
                    state = STATE_MATCHMAKING;
                }
            }
        }

        public void RestartMatchmakingCallback()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject) && state == STATE_GAME_END)
            {
                state = STATE_MATCHMAKING;
            }
        }

        public void OnStartMatchmaking()
        {

        }

        public void OnStopMatchmaking()
        {

        }

        public void OnStartGameCountdown()
        {
            if (Utilities.IsValid(localPlayer))
            {
                localPlayer.ResetPlayerAndPoints();
            }
            foreach (Portal p in portals)
            {
                if (Networking.LocalPlayer.IsOwner(p.gameObject))
                {
                    p.points = 0;
                }
            }
        }

        public void OnStopGameCountdown()
        {

        }
        float lastGameStart = -1001f;
        public void OnStartGame()
        {
            lastGameStart = Time.timeSinceLevelLoad;
            SendCustomEventDelayedSeconds(nameof(GameEndCallback), gameDuration);
            if (Utilities.IsValid(localPlayer))
            {
                localPlayer.ResetPlayerAndPoints();
                // localPlayer.bag.totalPrice = 0;
                // localPlayer.bag.totalWeight = 0;
            }
        }

        public void SpawnCallback()
        {
            teleportPortal.PortalAnimation(true);
        }

        public void GameEndCallback()
        {
            if (state == STATE_GAME_IN_PROGRESS && Mathf.Abs(lastGameStart + gameDuration - Time.timeSinceLevelLoad) < 0.5f)
            {
                EndGame();
            }
        }

        public void OnStopGame()
        {
            //spawn back in lobby
            if (localPlayer.portalIndex >= 0)
            {
                tracker.DropUpgrades();
                Networking.LocalPlayer.TeleportTo(teleportPortal.transform.position, teleportPortal.transform.rotation);
            }
            teleportPortal.PortalAnimation(false);
        }
        public void OnStartGameEnd()
        {

        }

        public void StopGameEndCallback()
        {
            if (state == STATE_GAME_END && Networking.LocalPlayer.IsOwner(gameObject))
            {
                state = STATE_MATCHMAKING;
            }
        }
        public void OnStopGameEnd()
        {

        }
        public void OnStartGameError()
        {

        }

        public void OnStopGameError()
        {

        }

        public void ClearError()
        {
            if (state == STATE_GAME_ERROR)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(ClearErrorCallback));
            }
        }

        public void ClearErrorCallback()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                state = STATE_MATCHMAKING;
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.IsValid() && player.isLocal && (state == STATE_GAME_IN_PROGRESS || state == STATE_GAME_START_COUNTDOWN))
            {
                localPlayer.portalIndex = -1;
            }
        }


        [System.NonSerialized]
        public string language = "[EN]";
        public const string EN = "[EN]";
        public const string JP = "[JP]";
        public const string KR = "[KR]";
        public const string CN = "[CN]";

        string fullQuery;
        public string GetString(string query)
        {
            fullQuery = query + language;
            if (!strings.ContainsKey(fullQuery))
            {
                return (string)strings[fullQuery];
            }
            else
            {
                switch (language)
                {
                    case JP:
                        {
                            return "欠落しているテキスト";
                        }
                    case KR:
                        {
                            return "누락된 텍스트";
                        }
                    case CN:
                        {
                            return "缺失文本";
                        }
                    default:
                        {
                            return "Missing Text";
                        }
                }
            }
        }

        public const string STRING_GAME_NAME = "game_name";
        public const string STRING_TEAM_ERROR = "team_error";
        public VRC.SDK3.Data.DataDictionary strings = new VRC.SDK3.Data.DataDictionary()
        {
            {STRING_GAME_NAME + EN, "Charm Raiders"},
            {STRING_TEAM_ERROR + EN, "At least 2 Teams are required to play"},
        };
    }
}