
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
        public Vector3 backpackOffset = new Vector3(0.1f, 0, -0.25f);
        public Vector3 desktopPickupOffset = new Vector3(0, 0, 0.5f);
        public Quaternion rotationOffset = Quaternion.Euler(-90, 0, 0);
        VRCPlayerApi _localPlayer;
        float lastAttach = -1001f;
        Transform startParent;
        bool startRan = false;
        public Bag bag;
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
            // sync.startPos = headData.position + bodyRotation * backpackOffset;
            // sync.startRot = bodyRotation * rotationOffset;
            if (parentPlayer.IsOwnerLocal())
            {
                EnterState();
            }
            else
            {
                sync.state = (stateID + SmartObjectSync.STATE_CUSTOM);
            }
        }

        public override void OnPickup()
        {
            transform.parent = null;
        }

        public override void OnDrop()
        {
            if (sync.IsLocalOwner() && sync.owner == parentPlayer.Owner)
            {
                SendCustomEventDelayedSeconds(nameof(ReAttach), localLerpTime);
            }
        }

        bool lastPickupeable = false;
        public override void OnEnterState()
        {
            lastAttach = -1001f;
            sync.rigid.isKinematic = true;
            sync.startPos = sync.rigid.position;
            sync.startRot = sync.rigid.rotation;
            lastShortcutTime = Time.timeSinceLevelLoad;
            if (Utilities.IsValid(sync.pickup))
            {
                lastPickupeable = sync.pickup.pickupable;
                sync.pickup.pickupable = parentPlayer.IsOwnerLocal();
            }
            transform.parent = startParent;
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
            if (Utilities.IsValid(sync.pickup))
            {
                sync.pickup.pickupable = lastPickupeable && parentPlayer.IsOwnerLocal();
            }
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
            bodyRotation = sync.owner.GetRotation();
            headData = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            if (!sync.IsLocalOwner())
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
            transform.rotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, bodyRotation * rotationOffset, Vector3.zero, smootherInterpolation);
            if (smootherInterpolation >= 1f)
            {
                lastPosition = transform.position;
            }
        }

        public override bool OnInterpolationEnd()
        {
            return !Utilities.IsValid(parentPlayer.Owner) || parentPlayer.Owner.isLocal || !Networking.IsObjectReady(gameObject);
        }

        public void Reposition()
        {
            sync.StartInterpolation();
        }
    }
}