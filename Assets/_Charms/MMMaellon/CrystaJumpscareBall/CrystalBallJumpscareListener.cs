
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;
#endif

namespace MMMaellon
{

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public class CrystalBallJumpscareListenerEditor : IVRCSDKBuildRequestedCallback
    {
        public static bool Setup()
        {
            CrystalBallJumpscareListener listener = GameObject.FindObjectOfType<CrystalBallJumpscareListener>();
            OpenableDoor[] doors = GameObject.FindObjectsOfType<OpenableDoor>();
            if (Utilities.IsValid(listener) && Utilities.IsValid(doors) && doors.Length > 0)
            {
                SerializedObject serialized = new SerializedObject(listener);
                serialized.FindProperty("doors").ClearArray();
                for (int i = 0; i < doors.Length; i++)
                {
                    serialized.FindProperty("doors").InsertArrayElementAtIndex(i);
                    serialized.FindProperty("doors").GetArrayElementAtIndex(i).objectReferenceValue = doors[i];
                }
                serialized.ApplyModifiedProperties();
            }
            return true;
        }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            Setup();
        }

        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            return Setup();
        }
    }
#endif
    public class CrystalBallJumpscareListener : OpenableDoorListener
    {
        public float scareChance = 0.25f;
        public float scareCooldown = 20f;
        public float scareDuration = 2f;
        public float scareFlyDuration = 0.5f;
        public float scareDistance = 0.5f;
        [System.NonSerialized]
        public float lastScare = -1001f;
        public GameObject scare;
        public OpenableDoor[] doors;
        public override void OnClose(OpenableDoor door, VRCPlayerApi closer)
        {
            
        }

        float randomChance;
        public override void OnOpen(OpenableDoor door, VRCPlayerApi opener)
        {
            if (!Utilities.IsValid(opener) || !opener.isLocal)
            {
                return;
            }
            if (gameObject.activeSelf)
            {
                if (lastScare + scareCooldown < Time.timeSinceLevelLoad)
                {
                    randomChance = Random.value;
                    if (randomChance < scareChance)
                    {
                        SpawnScare(door);
                    }
                }
            } else
            {
                door.RemoveListener(this);
            }
        }

        public void ActivateListener()
        {
            lastScare = -1001f;
            foreach (OpenableDoor door in doors)
            {
                door.AddListener(this);
            }
            scare.SetActive(false);
        }
        Vector3 headVector;
        Vector3 headPos;
        Vector3 startPos;
        public void SpawnScare(OpenableDoor door)
        {
            lastScare = Time.timeSinceLevelLoad;
            headPos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            headVector = headPos - door.transform.position;
            if (Utilities.IsValid(door.objectParent))
            {
                scare.transform.position = door.objectParent.transform.position;
            } else
            {
                scare.transform.position = door.transform.position - headVector;
            }
            startPos = scare.transform.position;
            goalPos = startPos;
            scare.transform.rotation = Quaternion.LookRotation(headVector);
            scare.SetActive(true);
            SendCustomEventDelayedFrames(nameof(ScareLoop), 1);
        }

        Vector3 goalPos;
        VRCPlayerApi.TrackingData headData;
        float progress;
        public void ScareLoop()
        {
            if (lastScare + scareDuration > Time.timeSinceLevelLoad)
            {
                progress = (Time.timeSinceLevelLoad - lastScare) / (scareFlyDuration);
                headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                headPos = headData.position;
                headVector = headPos - scare.transform.position;
                scare.transform.rotation = Quaternion.LookRotation(headVector);
                goalPos = Vector3.Lerp(goalPos, headPos + headData.rotation * Vector3.forward * scareDistance, 0.1f);
                if(progress < 1f)
                {
                    scare.transform.position = Vector3.Lerp(startPos, goalPos, progress);
                } else
                {
                    scare.transform.position = goalPos;
                }
                SendCustomEventDelayedFrames(nameof(ScareLoop), 1);
            } else
            {
                scare.SetActive(false);
            }
        }
    }
}