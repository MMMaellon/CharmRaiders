
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TestRespawn : UdonSharpBehaviour
{
    //array of VRChat ObjectSyncs
    public VRC.SDK3.Components.VRCObjectSync[] objectSyncs;
    void Start()
    {
        
    }

    public void TestRespawnObjects()
    {
        foreach (VRC.SDK3.Components.VRCObjectSync os in objectSyncs)
        {
            os.Respawn();
        }
    }
}
