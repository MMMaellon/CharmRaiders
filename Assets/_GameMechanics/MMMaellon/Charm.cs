
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync)), RequireComponent(typeof(ChildAttachmentState))]
    public abstract class Charm : UdonSharpBehaviour
    {
        [Multiline]
        public string charmName = "Gemstone";
        [Multiline]
        public string description = "No Effect";
        public int weight = 5;
        public int price = 5;
        public bool useLoop = false;
        public PriceTag priceTag;
        public abstract void StartCharmEffects();
        public abstract void CharmLoop();
        public abstract void StopCharmEffects();
        [HideInInspector]
        public BagChildAttachmentSetter bagSetter;
        [HideInInspector]
        public int index;
        [HideInInspector]
        public UpgradeTracker tracker;
        [HideInInspector]
        public CharmPool pool;
        [HideInInspector]
        public GameHandler game;
        [System.NonSerialized, FieldChangeCallback(nameof(player))]
        public Player _player;
        public bool playerChanged = false;
        public Player player{
            get => _player;
            set
            {
                if (_player != value)
                {
                    playerChanged = true;
                    if (Utilities.IsValid(_player))
                    {
                        StopCharmEffects();
                        if (_player.IsOwnerLocal())
                        {
                            tracker.RemoveCharm(this);
                        }
                    }
                    if (Utilities.IsValid(value))
                    {
                        if (value.IsOwnerLocal())
                        {
                            tracker.AddCharm(this);
                            bagSetter.child.sync.rigid.detectCollisions = !Utilities.IsValid(transform.parent) || !transform.parent.GetComponent<Bag>();
                        } else
                        {
                            bagSetter.child.sync.rigid.detectCollisions = false;
                        }
                    } else
                    {
                        bagSetter.child.sync.rigid.detectCollisions = true;
                    }
                } else
                {
                    playerChanged = false;
                }
                _player = value;
                if (playerChanged)
                {
                    if (value)
                    {
                        StartCharmEffects();
                    } else
                    {
                        //we want to run stop upgrade while player is defined so we do it up there
                    }
                }
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.FindProperty("bagSetter").objectReferenceValue = GetComponent<BagChildAttachmentSetter>();
            serializedObject.FindProperty("tracker").objectReferenceValue = GameObject.FindObjectOfType<UpgradeTracker>();
            serializedObject.ApplyModifiedProperties();
        }

        public void AddPriceTag()
        {
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
                Vector3 scaleVector = transform.InverseTransformVector(new Vector3(0.25f, 0.25f, 0.25f));
                priceTag.transform.localScale = scaleVector;
                SerializedObject serializedPriceTag = new SerializedObject(priceTag);
                serializedPriceTag.FindProperty("upgrade").objectReferenceValue = this;
                serializedPriceTag.ApplyModifiedProperties();
                priceTag.SetupText();
            }
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

        public void OnEnable()
        {
            bagSetter.child.sync.rigid.detectCollisions = true;
            portalIndex = -1001;
            ResetDisappearTimer();
            if (bagSetter.child.sync.IsLocalOwner())
            {
                bagSetter.child.sync.Respawn();
            }
        }

        public void ResetDisappearTimer()
        {
            lastDisappear = -1001f;
        }
        float lastDisappear = -1001f;
        public void Disappear()
        {
            bagSetter.child.sync.rigid.detectCollisions = false;
            bagSetter.child.sync.pickup.Drop();
            lastDisappear = Time.timeSinceLevelLoad;
            DisappearLoop();
        }

        public void DisappearLoop()
        {
            if (lastDisappear > 0 && lastDisappear + 0.5f < Time.timeSinceLevelLoad)
            {
                gameObject.SetActive(false);
                if (Networking.LocalPlayer.IsOwner(pool.gameObject))
                {
                    pool.Despawn(index);
                }
                lastDisappear = -1001f;
                return;
            } else if (portalIndex >= 0 && portalIndex < game.portals.Length && gameObject.activeSelf)
            {
                transform.position = Vector3.Lerp(transform.position, game.portals[portalIndex].transform.position, 0.25f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Random.rotation, 0.25f);
                SendCustomEventDelayedFrames(nameof(DisappearLoop), 1);
            }
        }

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(portalIndex))]
        public int _portalIndex = -1001;
        public int portalIndex{
            get => _portalIndex;
            set
            {
                Debug.LogWarning("setting portal index on charm to " + value);
                if (Utilities.IsValid(game.localPlayer) && value >= 0)
                {
                    Debug.LogWarning("localplayer is valid");
                    if (value != _portalIndex && bagSetter.child.sync.IsOwnerLocal())
                    {
                        Debug.LogWarning("we are owner");
                        game.localPlayer.points += price;
                    }
                }
                _portalIndex = value;
                if (value >= 0 && value < game.portals.Length)
                {
                    Disappear();
                } else
                {
                    ResetDisappearTimer();
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public virtual void Start()
        {
            
        }
    }
}