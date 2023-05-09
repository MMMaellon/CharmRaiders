
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync)), RequireComponent(typeof(CharmAttachment))]
    public abstract class Upgrade : UdonSharpBehaviour
    {
        [HideInInspector]
        public SmartObjectSync sync;
        public bool useLoop = false;
        public int weight = 5;
        public int price = 5;
        public abstract void StartUpgrade();
        public abstract void UpgradeLoop();
        public abstract void StopUpgrade();
    }
}