
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PortalIndicator : UdonSharpBehaviour
{
    public GameHandler game;
    public Transform portalIcon;
    public Transform arrowIcon;

    Vector3 flatForward;
    Vector3 flatForwardFromIcon;
    RaycastHit[] hits = new RaycastHit[1];
    Vector3 headPos;
    public override void PostLateUpdate()
    {
        if (Utilities.IsValid(game.localPlayer) && Utilities.IsValid(game.localPlayer.portal) && game.state == GameHandler.STATE_GAME_IN_PROGRESS)
        {
            portalIcon.gameObject.SetActive(true);
            arrowIcon.gameObject.SetActive(true);
            headPos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

            Debug.LogWarning("game.localPlayer.portal.rigid.worldCenterOfMass " + (game.localPlayer.portal.rigid.worldCenterOfMass));
            Debug.LogWarning("game.localPlayer.portal.transform.position " + (game.localPlayer.portal.transform.position));
            if (Physics.RaycastNonAlloc(headPos, game.localPlayer.portal.rigid.worldCenterOfMass - headPos, hits, 100, 1, QueryTriggerInteraction.Collide) > 0 && hits.Length > 0 && hits[0].rigidbody == game.localPlayer.portal.rigid)
            {
                portalIcon.position = Vector3.Project(transform.position - headPos, game.localPlayer.portal.rigid.worldCenterOfMass - headPos) + headPos;
            }
            else
            {
                if (hits.Length > 0 && Utilities.IsValid(hits[0]) && Utilities.IsValid(hits[0].collider))
                {
                    Debug.LogWarning("collider " + hits[0].collider.name);
                }
                else
                {
                    Debug.LogWarning("hits.Length > 0 " + (hits.Length > 0));
                    Debug.LogWarning("Utilities.IsValid(hits[0]) " + (Utilities.IsValid(hits[0])));
                    Debug.LogWarning("Utilities.IsValid(hits[0].collider) " + (Utilities.IsValid(hits[0].collider)));
                }
                flatForward = transform.InverseTransformDirection(game.localPlayer.portal.transform.position - transform.position).normalized;
                portalIcon.localPosition = new Vector3(flatForward.x, 0, 0);
                flatForwardFromIcon = transform.InverseTransformDirection(game.localPlayer.portal.transform.position - portalIcon.position);
                flatForwardFromIcon.y = 0;
                arrowIcon.localRotation = Quaternion.FromToRotation(Vector3.forward, flatForwardFromIcon);
            }
        }
        else
        {
            portalIcon.gameObject.SetActive(false);
            arrowIcon.gameObject.SetActive(false);
        }
    }

    public void Start()
    {
        portalIcon.gameObject.SetActive(false);
        arrowIcon.gameObject.SetActive(false);
    }
}
