using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class CharmSpawn : UdonSharpBehaviour
    {
        public abstract void SpawnCharm(ChildAttachmentState charm);
    }
}