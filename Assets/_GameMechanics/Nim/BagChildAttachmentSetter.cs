
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
        public void OnTriggerEnter(Collider other)
        {
            if(Networking.LocalPlayer.IsUserInVR()){
                if (lastBag == null && Utilities.IsValid(other) && other.gameObject.layer == bagLayer)
                {
                    lastBag = other.GetComponent<Bag>();
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if(Networking.LocalPlayer.IsUserInVR()){
                if (Utilities.IsValid(lastBag) && Utilities.IsValid(other) && lastBag.gameObject == other.gameObject)
                {
                    lastBag = null;
                }
            }
        }

        RaycastHit[] raycasts = new RaycastHit[16];
        int size = 0;
        VRCPlayerApi.TrackingData headData;
        public LayerMask bagLayer = 1 << 22;


        public override void OnPickup()
        {
            lastBag = null;
        }

        GameObject playerObject;
        Player localPlayer;
        public override void OnDrop()
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                if (Utilities.IsValid(lastBag))
                {
                    if (lastBag.gameObject.activeSelf)
                    {
                        child.Attach(lastBag.transform);
                    }
                }
            } else
            {
                playerObject = tracker.playerObjectAssigner._GetPlayerPooledObject(Networking.LocalPlayer);
                if (Utilities.IsValid(playerObject))
                {
                    localPlayer = playerObject.GetComponent<Player>();
                    if (Utilities.IsValid(localPlayer) && localPlayer.bag.playerBackAttachment.hasRaycastPos)
                    {
                        child.Attach(localPlayer.bag.transform);
                        child.sync.pos = localPlayer.bag.transform.InverseTransformPoint(localPlayer.bag.playerBackAttachment.raycastPos);
                    }
                }
            }
            lastBag = null;
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