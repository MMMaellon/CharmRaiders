
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync)), RequireComponent(typeof(CharmAttachment))]
    public abstract class Upgrade : UdonSharpBehaviour
    {
        [Multiline]
        public string upgradeName = "Gemstone";
        [Multiline]
        public string upgradeDescription = "No Effect";
        public int weight = 5;
        public int price = 5;
        public bool useLoop = false;
        public PriceTag priceTag;
        public abstract void StartUpgrade();
        public abstract void UpgradeLoop();
        public abstract void StopUpgrade();
        [HideInInspector]
        public CharmAttachment charm;
        [System.NonSerialized]
        public Player player;
        [System.NonSerialized]
        public bool isUpgrading = false;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.FindProperty("charm").objectReferenceValue = GetComponent<CharmAttachment>();
            serializedObject.ApplyModifiedProperties();
        }

        public void AddPriceTag()
        {
            Debug.LogWarning("AddPriceTag " + name);
            SerializedObject serializedObject = new SerializedObject(this);
            if (priceTag == null)
            {
                serializedObject.FindProperty("priceTag").objectReferenceValue = GetComponentInChildren<PriceTag>();
                serializedObject.ApplyModifiedProperties();

            }
            if (priceTag == null)
            {
                serializedObject.FindProperty("priceTag").objectReferenceValue = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_OtherProps/MMMaellon/PriceTag.prefab"), transform);
                serializedObject.ApplyModifiedProperties();
            }

            if (priceTag != null)
            {
                Debug.LogWarning("we have a price tag");
                Vector3 scaleVector = transform.InverseTransformVector(new Vector3(0.5f, 0.5f, 0.5f));
                priceTag.transform.localScale = scaleVector;
                SerializedObject serializedPriceTag = new SerializedObject(priceTag);
                serializedPriceTag.FindProperty("upgrade").objectReferenceValue = this;
                serializedPriceTag.ApplyModifiedProperties();
                priceTag.SetupText();
            }
            Debug.LogWarning("AddPriceTag finish");
        }
#endif
        public override void OnPickup()
        {
            if (Utilities.IsValid(priceTag))
            {
                priceTag.ShowPriceTag();
            }
        }

        public override void OnDrop()
        {
            if (Utilities.IsValid(priceTag))
            {
                priceTag.HidePriceTag();
            }
        }
    }
}