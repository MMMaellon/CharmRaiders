
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Collider))]
    public class PriceTag : UdonSharpBehaviour
    {
        public Upgrade upgrade;
        public TMPro.TextMeshProUGUI nameText;
        public TMPro.TextMeshProUGUI descriptionText;
        public TMPro.TextMeshProUGUI priceText;
        public TMPro.TextMeshProUGUI weightText;
        [System.NonSerialized]
        public Vector3 startingPosition;
        [System.NonSerialized]
        public Quaternion startingRotation;
        public AudioSource flipSound;
        [System.NonSerialized]
        public int _state;
        public int state{
            get => _state;
            set
            {
                _state = value;
                lastStateChange = Time.timeSinceLevelLoad;
                lastPosition = transform.localPosition;
                lastRotation = transform.localRotation;
            }
        }
        const int STATE_IDLE = 0;
        const int STATE_FRONT = 1;
        const int STATE_BACK = 2;
        const int STATE_VRFLIP = 3;
        float lastStateChange = -1001f;
        float transitionTime = 0.25f;
        Vector3 lastPosition;
        Quaternion lastRotation;
        bool loop = false;
        bool inVR = false;
        Collider col;
        public void Start()
        {
            startingPosition = transform.localPosition;
            startingRotation = transform.localRotation;
            inVR = Networking.LocalPlayer.IsUserInVR();
            col = GetComponent<Collider>();
            col.enabled = false;
        }
        Vector3 goalPos;
        Quaternion goalRot;
        VRCPlayerApi.TrackingData headData;
        float progress;
        Vector3 desktopOffset = new Vector3(-0.15f, -0.1f, 0.5f);
        Vector3 vrOffset = new Vector3(-0.8f, 0, 0);
        public void ShowLoop()
        {
            if ((state == STATE_IDLE || state == STATE_VRFLIP) && lastStateChange + transitionTime < Time.timeSinceLevelLoad)
            {
                if (state == STATE_IDLE)
                {
                    transform.localPosition = startingPosition;
                } else
                {
                    transform.localPosition = startingPosition + startingRotation * (vrOffset) * transform.lossyScale.x * transform.localScale.x;
                }
                transform.localRotation = goalRot;
                loop = false;
                return;
            }
            SendCustomEventDelayedFrames(nameof(ShowLoop), 1);
            progress = (Time.timeSinceLevelLoad - lastStateChange) / transitionTime;
            //smoothly lerp progress from 0 to 1 with easing in and out
            // progress = progress < 0.5f ? Mathf.Lerp(0, progress * 2, progress) : Mathf.Lerp((progress * 2) - 1, 1, progress);
            // push = Vector3.back * (Mathf.Sin(Mathf.PI * progress)) * 0.8f;
            switch (state)
            {
                case STATE_IDLE:
                    goalPos = startingPosition;
                    goalRot = startingRotation;
                    break;
                case STATE_FRONT:
                    headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    goalPos = transform.parent.InverseTransformPoint(headData.position + headData.rotation * desktopOffset);
                    goalRot = Quaternion.Inverse(transform.parent.rotation) * headData.rotation;
                    break;
                case STATE_BACK:
                    headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    goalPos = transform.parent.InverseTransformPoint(headData.position + headData.rotation * (desktopOffset + Vector3.left * (transform.lossyScale.x * 0.4f)));
                    goalRot = Quaternion.Inverse(transform.parent.rotation) * headData.rotation * Quaternion.Euler(0, 180.00001f, 0);//.00001f to force a direction when slerping
                    break;
                case STATE_VRFLIP:
                    goalPos = startingPosition + startingRotation * vrOffset * transform.lossyScale.x * transform.localScale.x;
                    goalRot = startingRotation * Quaternion.Euler(0, 180.00001f, 0);//.00001f to force a direction when slerping
                    break;
            }
            transform.localPosition = Vector3.Lerp(lastPosition, goalPos, progress);
            transform.localRotation = Quaternion.Slerp(lastRotation, goalRot, progress);
            if (Input.GetKeyDown(KeyCode.E))
            {
                Flip();
            }
        }

        public override void Interact()
        {
            Flip();
        }

        public void Flip()
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                switch (state)
                {
                    case STATE_IDLE:
                        state = STATE_VRFLIP;
                        break;
                    default:
                        state = STATE_IDLE;
                        break;
                }
                if (!loop)
                {
                    loop = true;
                    ShowLoop();
                }
            } else
            {
                switch (state)
                {
                    case STATE_FRONT:
                        state = STATE_BACK;
                        break;
                    case STATE_BACK:
                        state = STATE_FRONT;
                        break;
                }
            }
            flipSound.Play();
        }

        public void ShowPriceTag()
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                col.enabled = true;
            } else
            {
                state = STATE_FRONT;
                if (!loop)
                {
                    loop = true;
                    ShowLoop();
                }
            }
        }

        public void HidePriceTag()
        {
            state = STATE_IDLE;
            if (Networking.LocalPlayer.IsUserInVR())
            {
                col.enabled = false;
            }
        }

        public void DebugVRFlip()
        {
            switch (state)
            {
                case STATE_IDLE:
                    state = STATE_VRFLIP;
                    break;
                default:
                    state = STATE_IDLE;
                    break;
            }
            if (!loop)
            {
                loop = true;
                ShowLoop();
            }
        }

        public void OnEnable()
        {
            if (loop)
            {
                ShowLoop();
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void SetupText()
        {
            SerializedObject nameObj = new SerializedObject(nameText);
            SerializedObject descriptionObj = new SerializedObject(descriptionText);
            SerializedObject priceObj = new SerializedObject(priceText);
            SerializedObject weightObj = new SerializedObject(weightText);

            nameObj.FindProperty("m_text").stringValue = upgrade.upgradeName;
            descriptionObj.FindProperty("m_text").stringValue = upgrade.upgradeDescription;
            priceObj.FindProperty("m_text").stringValue = "$" + upgrade.price.ToString();
            weightObj.FindProperty("m_text").stringValue = upgrade.weight.ToString();

            nameObj.ApplyModifiedProperties();
            descriptionObj.ApplyModifiedProperties();
            priceObj.ApplyModifiedProperties();
            weightObj.ApplyModifiedProperties();
        }
#endif
    }
}