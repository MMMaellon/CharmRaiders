
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace MMMaellon
{
    public class BackpackAttachment : SmartObjectSyncState
    {
        public P_Shooters.Player parentPlayer;
        public float localLerpTime = 0.25f;
        public KeyCode shortcut = KeyCode.Tab;
        public Vector3 raycastPos;
        public bool hasRaycastPos = false;

        Vector3 targetOffset;
        public float horizontalThreshold = 15f;
        public float horizontalThresholdSmoothing = 0.9f;
        public Vector3 backpackOffset = new Vector3(0.15f, -0.15f, -0.4f);
        public Vector3 desktopPickupOffset = new Vector3(0, 0, 0.5f);
        public Quaternion rotationOffset = Quaternion.Euler(120, 0, 0);
        public Quaternion desktopRotationOffset = Quaternion.Euler(90, 0, 0);
        VRCPlayerApi _localPlayer;
        float lastAttach = -1001f;
        Transform startParent;
        bool startRan = false;
        public Bag bag;
        public bool changeParent = false;
        public void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            startParent = transform.parent;
            startRan = true;
        }

        public void OnEnable()
        {
            if (!startRan)
            {
                return;
            }
            if (Utilities.IsValid(sync.owner))
            {
                bodyRotation = sync.owner.GetRotation();
                headData = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            }
            if (parentPlayer.IsOwnerLocal())
            {
                EnterState();
            }
        }

        public override void OnPickup()
        {
            if (changeParent)
            {
                transform.parent = null;
            }
        }

        public override void OnDrop()
        {
            if (sync.IsLocalOwner() && sync.owner == parentPlayer.Owner)
            {
                SendCustomEventDelayedSeconds(nameof(ReAttach), localLerpTime);
            }
        }
        public override void OnEnterState()
        {
            lastAttach = -1001f;
            sync.rigid.isKinematic = true;
            sync.startPos = sync.rigid.position;
            sync.startRot = sync.rigid.rotation;
            lastShortcutTime = Time.timeSinceLevelLoad;
            sync.pickupable = parentPlayer.IsOwnerLocal();
            if (changeParent)
            {
                transform.parent = startParent;
            }
            if (parentPlayer.IsOwnerLocal() && !sync.IsLocalOwner())
            {
                sync.TakeOwnership(false);
                EnterState();
            }
        }

        public override void OnExitState()
        {
            lastAttach = Time.timeSinceLevelLoad;
            sync.rigid.isKinematic = false;
            sync.pickupable = parentPlayer.IsOwnerLocal();
            if (parentPlayer.IsOwnerLocal())
            {
                SendCustomEventDelayedSeconds(nameof(ReAttach), localLerpTime);
            }
        }

        public void ReAttach()
        {
            if (IsActiveState() || sync.IsHeld() || lastAttach + localLerpTime > Time.timeSinceLevelLoad + 0.01f)
            {
                return;
            }
            if (!sync.IsHeld())
            {
                EnterState();
            }
        }

        public override void OnSmartObjectSerialize()
        {

        }

        public override void OnInterpolationStart()
        {
            sync.startPos = sync.rigid.position;
            sync.startRot = sync.rigid.rotation;
        }

        float smootherInterpolation;
        bool desktopPickup = false;
        float lastShortcutTime = -1001f;
        VRCPlayerApi.TrackingData headData;
        Quaternion bodyRotation;
        Vector3 targetVector;
        float targetAngle;
        Vector3 lastPosition;
        public GameObject placementIcon;
        RaycastHit[] raycasts = new RaycastHit[16];
        int size = 0;
        public LayerMask bagLayer = 1 << 22;
        public override void Interpolate(float interpolation)
        {
            if (!Utilities.IsValid(parentPlayer.Owner))
            {
                return;
            }
            headData = parentPlayer.Owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            bodyRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(parentPlayer.Owner.GetRotation() * Vector3.forward, Vector3.Slerp(headData.rotation * Vector3.up, Vector3.up, 0.25f)));
            if (!parentPlayer.IsOwnerLocal())
            {
                transform.position = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, headData.position + bodyRotation * backpackOffset, Vector3.zero, interpolation);
                transform.rotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, bodyRotation * rotationOffset, Vector3.zero, interpolation);
                return;
            }

            if (Input.GetKey(shortcut) != desktopPickup)
            {
                lastPosition = headData.position + bodyRotation * desktopPickupOffset;
                OnEnterState();//basically restart the lerp
                desktopPickup = Input.GetKey(shortcut);
                if (Utilities.IsValid(bag))
                {
                    if (desktopPickup)
                    {
                        bag.Unzip();
                    }
                    else
                    {
                        bag.Zip();
                    }
                }
            }
            smootherInterpolation = (Time.timeSinceLevelLoad - lastShortcutTime) / localLerpTime;
            if (desktopPickup)
            {
                bodyRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(parentPlayer.Owner.GetRotation() * Vector3.forward, Vector3.up));
                targetVector = Quaternion.Inverse(bodyRotation) * (lastPosition - headData.position);
                targetAngle = Vector3.SignedAngle(Vector3.ProjectOnPlane(desktopPickupOffset, Vector3.up), Vector3.ProjectOnPlane(targetVector, Vector3.up), Vector3.up);
                if (targetAngle < -horizontalThreshold)
                {
                    targetAngle = Mathf.Lerp(-horizontalThreshold, targetAngle, horizontalThresholdSmoothing);
                }
                else if (targetAngle > horizontalThreshold)
                {
                    targetAngle = Mathf.Lerp(horizontalThreshold, targetAngle, horizontalThresholdSmoothing);
                }
                targetOffset = Quaternion.AngleAxis(targetAngle, Vector3.up) * desktopPickupOffset;

                if (Utilities.IsValid(placementIcon))
                {
                    size = Physics.RaycastNonAlloc(headData.position, headData.rotation * Vector3.forward, raycasts, 3f, bagLayer, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < Mathf.Min(size, raycasts.Length); i++)
                    {
                        if (Utilities.IsValid(raycasts[i].collider))
                        {
                            if (Utilities.IsValid(raycasts[i].collider.GetComponent<Bag>()))
                            {
                                hasRaycastPos = true;
                                raycastPos = raycasts[i].point;
                                placementIcon.gameObject.SetActive(true);
                                placementIcon.transform.position = raycastPos;
                                break;
                            }
                            hasRaycastPos = false;
                            placementIcon.gameObject.SetActive(false);
                        }
                    }
                    if (size == 0)
                    {
                        hasRaycastPos = false;
                        placementIcon.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (Utilities.IsValid(placementIcon))
                {
                    hasRaycastPos = false;
                    placementIcon.gameObject.SetActive(false);
                }
                targetOffset = backpackOffset;
            }
            transform.position = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, headData.position + bodyRotation * targetOffset, Vector3.zero, smootherInterpolation);
            if (desktopPickup)
            {
                transform.rotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, bodyRotation * desktopRotationOffset, Vector3.zero, smootherInterpolation);
            }
            else
            {
                transform.rotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, bodyRotation * rotationOffset, Vector3.zero, smootherInterpolation);
            }
            if (smootherInterpolation >= 1f)
            {
                lastPosition = transform.position;
            }
        }

        public override bool OnInterpolationEnd()
        {
            return !Utilities.IsValid(parentPlayer.Owner) || parentPlayer.Owner.isLocal || !Networking.IsObjectReady(gameObject) || !Networking.IsClogged;
        }

        public void Reposition()
        {
            sync.StartInterpolation();
        }

        public void OnAvatarEyeHeightChanged()
        {
            if (sync.IsOwnerLocal())
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Reposition));
            }
        }

        public override void OnAvatarChanged(VRCPlayerApi player)
        {
            if (player == sync.owner)
            {
                Reposition();
            }
        }

        // public void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.B))
        //     {
        //         sync.StartInterpolation();
        //     }
        // }
    }
}