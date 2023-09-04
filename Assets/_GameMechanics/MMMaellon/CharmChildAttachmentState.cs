
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
    public class CharmChildAttachmentState : ChildAttachmentState
    {
        Bag currentBag;
        // [HideInInspector]
        public Charm charm;
        public override void OnEnterState()
        {
            base.OnEnterState();
            currentBag = parentTransform.GetComponent<Bag>();
            if (Utilities.IsValid(currentBag) && Networking.LocalPlayer.IsOwner(currentBag.gameObject))
            {
                currentBag.totalPrice += charm.price;
            }
        }
        public override void OnExitState()
        {
            currentBag = parentTransform.GetComponent<Bag>();
            if (Utilities.IsValid(currentBag) && Networking.LocalPlayer.IsOwner(currentBag.gameObject))
            {
                currentBag.totalPrice -= charm.price;
            }
            base.OnExitState();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            SerializedObject obj = new SerializedObject(this);
            obj.FindProperty("charm").objectReferenceValue = GetComponent<Charm>();
            obj.ApplyModifiedProperties();
        }
#endif
    }
}