
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class OpenableDoorListener : UdonSharpBehaviour
    {
        public abstract void OnOpen(OpenableDoor door, VRCPlayerApi opener);
        public abstract void OnClose(OpenableDoor door, VRCPlayerApi closer);
    }
}