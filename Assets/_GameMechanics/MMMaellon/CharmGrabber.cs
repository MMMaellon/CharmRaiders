
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CharmGrabber : UdonSharpBehaviour
    {
        public bool right = true;
        VRCPlayerApi.TrackingData rightHand;
        VRCPlayerApi.TrackingData leftHand;
        public void Start()
        {
            rightHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            leftHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        }
        public void LateUpdate()
        {
            if (right)
            {
                rightHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                transform.position = rightHand.position;
            } else
            {
                leftHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
                transform.position = leftHand.position;
            }
        }
    }
}