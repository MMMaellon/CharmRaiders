using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using VRC.Editor;
#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(ChildAttachmentState))]
    public class BagChildAttachmentSetter : UdonSharpBehaviour
    {
        public ChildAttachmentState child;
        Bag lastBag = null;
        public UpgradeTracker tracker;

        RaycastHit[] raycasts = new RaycastHit[16];
        int size = 0;
        VRCPlayerApi.TrackingData headData;
        public LayerMask bagLayer = 1 << 22;

        GameObject playerObject;
        Player localPlayer;
        BoxCollider boxCol;
        int colSize;
        Collider[] colliders = new Collider[16];
        Vector3 bagClosest;
        Vector3 thisClosest;
        public override void OnDrop()
        {
            playerObject = tracker.playerObjectAssigner._GetPlayerPooledObject(Networking.LocalPlayer);
            if (Utilities.IsValid(playerObject))
            {
                localPlayer = playerObject.GetComponent<Player>();
                if (!Utilities.IsValid(localPlayer))
                {
                    return;
                }
                if (Networking.LocalPlayer.IsUserInVR())
                {
                    boxCol = localPlayer.bag.GetComponent<BoxCollider>();
                    bagClosest = boxCol.ClosestPoint(transform.position);
                    thisClosest = child.sync.rigid.GetComponent<Collider>().ClosestPoint(localPlayer.bag.transform.position);
                    if (Vector3.Distance(bagClosest, transform.position) < Vector3.Distance(thisClosest, transform.position))
                    {
                        //we are overlapping
                        child.Attach(localPlayer.bag.transform);
                        child.sync.pos = localPlayer.bag.transform.InverseTransformPoint(transform.position);
                        child.sync.rot = child.sync.startRot;
                        child.RequestSerialization();
                    }
                } else
                {
                    if (localPlayer.bag.backpackAttachment.hasRaycastPos)
                    {
                        child.Attach(localPlayer.bag.transform);
                        child.sync.pos = localPlayer.bag.transform.InverseTransformPoint(localPlayer.bag.backpackAttachment.raycastPos);
                        child.sync.rot = child.sync.startRot;
                        child.RequestSerialization();
                    }
                }
            }
        }

        public override void OnDeserialization()
        {
            //has to happen here
            if (child.IsActiveState() && Utilities.IsValid(transform.parent) && Utilities.IsValid(transform.parent.GetComponent<Bag>()))
            {
                GetComponent<Charm>().player = tracker.playerObjectAssigner._GetPlayerPooledObject(child.sync.owner).GetComponent<Player>();
                transform.parent.GetComponent<Bag>().backpackAttachment.sync.StartInterpolation();
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SerializedObject obj = new SerializedObject(this);
            obj.FindProperty("child").objectReferenceValue = GetComponent<ChildAttachmentState>();
            obj.FindProperty("tracker").objectReferenceValue = GetComponent<UpgradeTracker>();
            obj.ApplyModifiedProperties();
        }
#endif

    }
}