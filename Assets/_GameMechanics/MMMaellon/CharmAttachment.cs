
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;


namespace MMMaellon
{
    [CustomEditor(typeof(CharmAttachment)), CanEditMultipleObjects]

    public class CharmAttachmentEditor : Editor
    {
        SerializedProperty _allowedBones;
        SerializedProperty allowedBones;
        SerializedProperty allowAttachToSelf;

        void OnEnable()
        {
            // Fetch the objects from the MyScript script to display in the inspector
            _allowedBones = serializedObject.FindProperty("_allowedBones");
            allowedBones = serializedObject.FindProperty("allowedBones");
            allowAttachToSelf = serializedObject.FindProperty("allowAttachToSelf");
            SyncAllowedBones();
        }
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(allowAttachToSelf, true);
            EditorGUILayout.PropertyField(_allowedBones, true);
            if (EditorGUI.EndChangeCheck())
            {
                SyncAllowedBones();
            }
            // EditorGUILayout.Space();
            // base.OnInspectorGUI();
        }

        public void SyncAllowedBones()
        {
            allowedBones.ClearArray();
            for (int i = 0; i < _allowedBones.arraySize; i++)
            {
                allowedBones.InsertArrayElementAtIndex(i);
                allowedBones.GetArrayElementAtIndex(i).intValue = _allowedBones.GetArrayElementAtIndex(i).enumValueIndex;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CharmAttachment : SmartObjectSyncState
    {
        public int bone = -1001;
        public Vector3 localPosition = Vector3.zero;
        public Quaternion localRotation = Quaternion.identity;

        public bool allowAttachToSelf = true;
        // public bool allowAttachToOthers = true;
        public int[] allowedBones = { 0 };

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        //For displaying in the editor only
        public HumanBodyBones[] _allowedBones = { 0 };
#else
        //For displaying in the editor only
        [System.NonSerialized]
        public int[] _allowedBones = { 0 };
#endif
        VRCPlayerApi localPlayer;
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            startScale = transform.localScale;
        }

        public override void OnPickup()
        {
            base.OnPickup();
            bone = -1001;
        }

        public override void OnPickupUseDown()
        {
            base.OnPickupUseDown();
            if (localPlayer.IsUserInVR())
            {
                Attach();
            } else
            {
                EnterState();
            }
        }

        public void Attach()
        {
            sync._print("Attach");
            if (!sync.IsLocalOwner())
            {
                sync.TakeOwnership(false);
            }

            // if (allowAttachToOthers)
            // {
            //     VRCPlayerApi[] nearbyPlayers = GetNearbyPlayers(3);//arbitrarily pick the closest 3 players to compare against
            //     VRCPlayerApi closestPlayer = null;
            //     bone = GetClosestBoneInGroup(nearbyPlayers, ref closestPlayer);
            //     playerId = closestPlayer.playerId;

            //     Vector3 bonePos = closestPlayer.GetBonePosition((HumanBodyBones)(bone));
            //     Quaternion boneRot = closestPlayer.GetBoneRotation((HumanBodyBones)(bone));
            //     localPosition = Quaternion.Inverse(boneRot) * (transform.position - bonePos);
            //     localRotation = Quaternion.Inverse(boneRot) * (transform.rotation);

            //     if (playerId == localPlayer.playerId)
            //     {
            //         ApplyAttachment();
            //     }
            //     else
            //     {
            //         sync.state = SmartObjectSync.STATE_WORLD_LOCK;
            //         RequestSerialization();
            //     }
            // }
            if (allowAttachToSelf)
            {
                bone = GetClosestBone(localPlayer);
                ApplyAttachment();
            }
        }

        public VRCPlayerApi[] GetNearbyPlayers(int count)
        {
            VRCPlayerApi[] nearby = new VRCPlayerApi[count];
            float[] nearbyDist = new float[count];
            VRCPlayerApi[] allPlayers = new VRCPlayerApi[82];
            VRCPlayerApi.GetPlayers(allPlayers);
            float dist;
            foreach (VRCPlayerApi player in allPlayers)
            {
                if (!Utilities.IsValid(player) || (player.isLocal && !allowAttachToSelf))
                {
                    continue;
                }
                dist = Vector3.Distance(transform.position, player.GetPosition());
                if (nearby[0] == null || nearbyDist[0] == 0 || nearbyDist[0] > dist)
                {
                    nearby[2] = nearby[1];
                    nearby[1] = nearby[0];
                    nearby[0] = player;
                    nearbyDist[2] = nearbyDist[1];
                    nearbyDist[1] = nearbyDist[0];
                    nearbyDist[0] = dist;
                }
                else if (nearby[1] == null || nearbyDist[1] == 0 || nearbyDist[1] > dist)
                {
                    nearby[2] = nearby[1];
                    nearby[1] = player;
                    nearbyDist[2] = nearbyDist[1];
                    nearbyDist[1] = dist;
                }
                else if (nearby[2] == null || nearbyDist[2] == 0 || nearbyDist[2] > dist)
                {
                    nearby[2] = player;
                    nearbyDist[2] = dist;
                }
            }
            return nearby;
        }

        public void ApplyAttachment()
        {
            sync._print("ApplyAttachment bone is " + bone);
            if (bone >= 0)
            {
                sync.AttachToBone((HumanBodyBones)bone);
            }
            bone = -1001;
            RequestSerialization();
        }

        public int GetClosestBone(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
            {
                return -1001;
            }
            int closestBone = -1001;
            float closestDistance = -1001f;
            foreach (int i in allowedBones)
            {
                Vector3 bonePos = FindBoneCenter((HumanBodyBones)i, player);
                float boneDist = Vector3.Distance(bonePos, transform.position);
                if (bonePos != Vector3.zero && (closestDistance < 0 || closestDistance > boneDist))
                {
                    closestBone = i;
                    closestDistance = boneDist;
                }
            }
            return closestBone;
        }
        public int GetClosestBoneInGroup(VRCPlayerApi[] players, ref VRCPlayerApi closestPlayer)
        {
            int closestBone = -1001;
            float closestDistance = -1001f;
            foreach (VRCPlayerApi player in players)
            {
                if (!Utilities.IsValid(player))
                {
                    continue;
                }
                foreach (int i in allowedBones)
                {
                    Vector3 bonePos = FindBoneCenter((HumanBodyBones)i, player);
                    float boneDist = Vector3.Distance(bonePos, transform.position);
                    if (bonePos != Vector3.zero && (closestDistance < 0 || closestDistance > boneDist))
                    {
                        closestBone = i;
                        closestDistance = boneDist;
                        closestPlayer = player;
                    }
                }
            }
            return closestBone;
        }

        public Vector3 FindBoneCenter(HumanBodyBones humanBodyBone, VRCPlayerApi player)
        {
            Vector3 bonePos = player.GetBonePosition(humanBodyBone);
            switch (humanBodyBone)
            {
                case (HumanBodyBones.LeftUpperLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftLowerLeg), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightUpperLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightLowerLeg), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftLowerLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftFoot), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightLowerLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightFoot), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftUpperArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftLowerArm), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightUpperArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightLowerArm), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftLowerArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftHand), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightLowerArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightHand), 0.5f);
                        break;
                    }
            }
            return bonePos;
        }

        //FOR DESKTOP
        [HideInInspector]
        public UpgradeTracker tracker;
        [System.NonSerialized]
        public int inventoryIndex = 0;//0 or greater means it's in the left hand, -1 or less means its in the left hand inventory

        [System.NonSerialized]
        public int nonstatic_leftInventoryItemCount = 0;
        [System.NonSerialized]
        public int nonstatic_rightInventoryItemCount = 0;
        public float inventoryItemScale = 0.1f;
        [System.NonSerialized]
        public Vector3 startScale;

        [System.NonSerialized]
        public float interpolationStart = -1001f;
        [System.NonSerialized]
        public Vector3 startPos;
        [System.NonSerialized]
        public Quaternion startRot;
        Collider col;
        [System.NonSerialized]
        public Vector3 offset;

        public override void OnEnterState()
        {
            if (sync.IsLocalOwner())
            {
                tracker.AddToDesktopInventory(this);
            }
            Debug.LogWarning(name + " CharmAttachment.OnEnterState");
            sync.rigid.detectCollisions = false;
        }

        public override void OnExitState()
        {
            if (sync.IsLocalOwner())
            {
                tracker.RemoveFromDesktopInventory(inventoryIndex);
            }
            transform.localScale = startScale;
            sync.rigid.detectCollisions = true;
        }

        public override void OnSmartObjectSerialize()
        {
            sync.pos = GetTightlyPackedPosition(inventoryIndex, tracker.desktopCharms.Length);
        }

        public override void OnInterpolationStart()
        {
            interpolationStart = Time.timeSinceLevelLoad;
            if (Utilities.IsValid(sync.owner))
            {
                startPos = Quaternion.Inverse(sync.owner.GetRotation()) * (transform.position - sync.owner.GetPosition());
                startRot = Quaternion.Inverse(sync.owner.GetRotation()) * (transform.rotation);
                if (sync.owner.isLocal)
                {
                    offset = CalcOffset();
                    sync.pos = GetTightlyPackedPosition(inventoryIndex, tracker.desktopCharms.Length);
                }
            }
        }

        public override void Interpolate(float interpolation)
        {
            sync.parentPos = sync.owner.GetPosition();
            if (Utilities.IsValid(sync.owner))
            {
                float slowerInterpolation = tracker.inventoryInterpolationTime <= 0 ? 1.0f : Mathf.Clamp01((Time.timeSinceLevelLoad - interpolationStart) / tracker.inventoryInterpolationTime);
                transform.position = sync.HermiteInterpolatePosition(sync.parentPos + sync.owner.GetRotation() * startPos, Vector3.zero, CalcPos(sync.IsLocalOwner()), Vector3.zero, slowerInterpolation);
                transform.rotation = sync.HermiteInterpolateRotation(sync.owner.GetRotation() * startRot, Vector3.zero, CalcRot(), Vector3.zero, slowerInterpolation);
                if (sync.IsLocalOwner())
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, startScale * inventoryItemScale, slowerInterpolation);
                }
                else
                {
                    transform.localScale = startScale;
                }
            }
            sync.rigid.velocity = Vector3.zero;
            sync.rigid.angularVelocity = Vector3.zero;
        }

        public override bool OnInterpolationEnd()
        {
            return true;
        }
        VRCPlayerApi.TrackingData data;
        public Vector3 CalcPos(bool calcAsOwner)
        {
            if (calcAsOwner)
            {
                data = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                return data.position + (data.rotation * tracker.desktopInventoryPlacement) + (sync.owner.GetRotation() * offset);
            } else if(Utilities.IsValid(sync.owner))
            {
                data = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                return Vector3.Lerp(sync.owner.GetPosition(), data.position, 0.5f) + sync.owner.GetRotation() * sync.pos;
            }
            return transform.position;
        }
        public Quaternion CalcRot()
        {
            if (sync.IsLocalOwner())
            {
                return sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            }
            else if (Utilities.IsValid(sync.owner))
            {
                return sync.owner.GetRotation();
            }
            return transform.rotation;
        }
        public Vector3 CalcOffset()
        {
            return Vector3.right * tracker.inventorySpacing * (inventoryIndex - ((tracker.desktopCharms.Length - 1) / 2.0f));
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            base.OnOwnershipTransferred(player);
            if (IsActiveState() && sync.IsLocalOwner())
            {
                ExitState();
            }
        }

        int objectsPerCircle = 6;//mathematically derived. circumference is 2 * PI which can be rounded to 6. unit circle scales uniformly so spacing doesn't factor into this calculation
        public Vector3 GetTightlyPackedPosition(int index, int totalCount)
        {
            if (index == 0 || totalCount == 0)
            {
                return Vector3.back * tracker.desktopInventorySpacing;
            }
            //Forumla uses triangle numbers and concentric circles
            //there's a single object at Vector3.zero which makes the inner most circle
            //all circles have a radius that's a multiple of our spacing variable
            //As we add new circles the total capacity of all these circles increases to the next Triangle number
            //Triangle numbers are given by T(n) = n * (n + 1) / 2
            //To place an object, we find the triangle number / 
            // The formula for the circumference of a circle is 2 * Pi * r (r = radius)
            int innerCircleIndex = Mathf.FloorToInt((Mathf.Sqrt(8 * ((index - 1) / objectsPerCircle) + 1) - 1) / 2);
            int existingObjectsCount = (innerCircleIndex * (innerCircleIndex + 1) / 2) * objectsPerCircle;
            int nextObjectCount = ((innerCircleIndex + 1) * ((innerCircleIndex + 1) + 1) / 2) * objectsPerCircle;
            if (nextObjectCount > totalCount - 1)
            {
                return GetPointOnCircle((index - 1) - existingObjectsCount, (totalCount - 1) - existingObjectsCount, tracker.desktopInventorySpacing * (innerCircleIndex + 1));
            } else
            {
                return GetPointOnCircle((index - 1) - existingObjectsCount, nextObjectCount - existingObjectsCount, tracker.desktopInventorySpacing * (innerCircleIndex + 1));
            }
        }
        float angle;
        float x;
        float y;
        public Vector3 GetPointOnCircle(int i, int totalCircleCount, float radius)
        {
            angle = (((2 * Mathf.PI) / totalCircleCount) * i) + (Mathf.PI / 2);
            x = radius * Mathf.Cos(angle);
            y = radius * Mathf.Sin(angle);
            return new Vector3(x, y, Mathf.Lerp(-0.25f, 0f, radius));
        }
    }
}