#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using System.IO;

namespace MMMaellon
{
    [InitializeOnLoad]
    public class AutoSetup : IVRCSDKBuildRequestedCallback
    {

        [MenuItem("Tools/Trigger CharmRaider Auto Setup")]
        static void TriggerAutoSetup()
        {
            Setup();
        }
        public static bool Setup()
        {
            UpgradeTracker upgradeTracker = GameObject.FindObjectOfType<UpgradeTracker>();
            GameHandler game = GameObject.FindObjectOfType<GameHandler>();
            MMMaellon.P_Shooters.P_ShootersPlayerHandler playerHandler = GameObject.FindObjectOfType<MMMaellon.P_Shooters.P_ShootersPlayerHandler>();
            CharmPlayerListener charmPlayerListener = GameObject.FindObjectOfType<CharmPlayerListener>();

            SerializedObject serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("tracker").objectReferenceValue = upgradeTracker;
            serializedGame.FindProperty("playerHandler").objectReferenceValue = playerHandler;
            serializedGame.FindProperty("charmListener").objectReferenceValue = charmPlayerListener;
            serializedGame.FindProperty("portals").ClearArray();
            Portal[] portals = GameObject.FindObjectsOfType<Portal>();
            for (int i = 0; i < portals.Length; i++)
            {
                serializedGame.FindProperty("portals").InsertArrayElementAtIndex(i);
                serializedGame.FindProperty("portals").GetArrayElementAtIndex(i).objectReferenceValue = portals[i];
                SerializedObject serializedPortal = new SerializedObject(portals[i]);
                serializedPortal.FindProperty("index").intValue = i;
                serializedPortal.FindProperty("game").objectReferenceValue = game;
                serializedPortal.ApplyModifiedProperties();
            }
            foreach (Player player in GameObject.FindObjectsOfType<Player>())
            {
                SerializedObject serializedPlayer = new SerializedObject(player);
                serializedPlayer.FindProperty("game").objectReferenceValue = game;
                serializedPlayer.ApplyModifiedProperties();
            }
            serializedGame.ApplyModifiedProperties();

            SerializedObject serializedUpgradeTracker = new SerializedObject(upgradeTracker);
            serializedUpgradeTracker.FindProperty("playerObjectAssigner").objectReferenceValue = GameObject.FindObjectOfType<Cyan.PlayerObjectPool.CyanPlayerObjectAssigner>();
            serializedUpgradeTracker.FindProperty("playerHandler").objectReferenceValue = playerHandler;
            serializedUpgradeTracker.FindProperty("charmListener").objectReferenceValue = charmPlayerListener;

            SerializedObject serializedCharmPlayerListener = new SerializedObject(charmPlayerListener);
            serializedCharmPlayerListener.FindProperty("game").objectReferenceValue = game;
            serializedCharmPlayerListener.ApplyModifiedProperties();

            Charm[] upgrades = GameObject.FindObjectsOfType<Charm>();
            if (Utilities.IsValid(upgradeTracker) && Utilities.IsValid(upgrades) && upgrades.Length > 0)
            {
                serializedUpgradeTracker.FindProperty("upgrades").ClearArray();
                for (int i = 0; i < upgrades.Length; i++)
                {
                    serializedUpgradeTracker.FindProperty("upgrades").InsertArrayElementAtIndex(i);
                    serializedUpgradeTracker.FindProperty("upgrades").GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
                    SerializedObject serializedUpgrade = new SerializedObject(upgrades[i]);
                    if (upgrades[i].GetComponent<BagChildAttachmentSetter>())
                    {
                        SerializedObject serializedBagSetter = new SerializedObject(upgrades[i].GetComponent<BagChildAttachmentSetter>());
                        serializedBagSetter.FindProperty("tracker").objectReferenceValue = upgradeTracker;
                        serializedBagSetter.ApplyModifiedProperties();
                        serializedUpgrade.FindProperty("bagSetter").objectReferenceValue = upgrades[i].GetComponent<BagChildAttachmentSetter>();
                        serializedUpgrade.FindProperty("tracker").objectReferenceValue = upgradeTracker;
                        serializedUpgrade.FindProperty("game").objectReferenceValue = game;
                    }
                    serializedUpgrade.ApplyModifiedProperties();
                    upgrades[i].AddPriceTag();
                }
                CharmPool[] charmPools = GameObject.FindObjectsOfType<CharmPool>();
                foreach (CharmPool charmPool in charmPools)
                {
                    SerializedObject serializedPool = new SerializedObject(charmPool);
                    serializedPool.FindProperty("charms").ClearArray();
                    Charm[] charmChildren = charmPool.GetComponentsInChildren<Charm>();
                    for (int i = 0; i < charmChildren.Length; i++)
                    {
                        serializedPool.FindProperty("charms").InsertArrayElementAtIndex(i);
                        serializedPool.FindProperty("charms").GetArrayElementAtIndex(i).objectReferenceValue = charmChildren[i];
                    }
                    serializedPool.ApplyModifiedProperties();
                }
                serializedUpgradeTracker.FindProperty("charmPools").ClearArray();
                for (int i = 0; i < charmPools.Length; i++)
                {
                    serializedUpgradeTracker.FindProperty("charmPools").InsertArrayElementAtIndex(i);
                    serializedUpgradeTracker.FindProperty("charmPools").GetArrayElementAtIndex(i).objectReferenceValue = charmPools[i];
                }
                serializedUpgradeTracker.ApplyModifiedProperties();
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
}
#endif