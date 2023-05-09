
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CharmPool : UdonSharpBehaviour
    {
        public float spawnChance = 0.5f;
        public CharmSpawn[] spawns;
        public ChildAttachmentState[] charms;
        OpenableDoor door;
        int unspawnedIndex;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(spawned))]
        public bool[] _spawned = null;
        public bool[] spawned
        {
            get => _spawned;
            set
            {
                Debug.LogWarning("CHARM POOL set value of _spawned");
                _spawned = value;
                for (int i = 0; i < _spawned.Length; i++)
                {
                    if (!_spawned[i])
                    {
                        if (Utilities.IsValid(charms[i].sync) && charms[i].sync.IsAttachedToPlayer())
                        {
                            charms[i].sync.state = SmartObjectSync.STATE_TELEPORTING;
                        }
                    }
                    Debug.LogWarning("CHARM POOL setting " + charms[i].name + " to " + _spawned[i]);
                    charms[i].gameObject.SetActive(_spawned[i]);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    Debug.LogWarning("CHARM POOL RequestSerialization");
                    RequestSerialization();
                }
            }
        }
        public void Start()
        {
            if (_spawned == null || _spawned.Length != charms.Length)
            {
                _spawned = new bool[charms.Length];
                for (int i = 0; i < _spawned.Length; i++)
                {
                    _spawned[i] = charms[i].gameObject.activeSelf;
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public void SpawnCharms()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < _spawned.Length; i++)
            {
                _spawned[i] = false;
            }
            foreach (CharmSpawn spawn in spawns)
            {
                float random = Random.Range(0.0f, 1.0f);
                if (random < spawnChance)
                {
                    unspawnedIndex = GetUnspawnedCharmIndex();
                    if (unspawnedIndex < 0)
                    {
                        break;
                    }
                    _spawned[unspawnedIndex] = true;
                    charms[unspawnedIndex].gameObject.SetActive(true);
                    spawn.SpawnCharm(charms[unspawnedIndex]);
                }
            }
            spawned = _spawned;
        }

        int startIndex = 0;
        public int GetUnspawnedCharmIndex()
        {
            startIndex = Random.Range(0, _spawned.Length);
            for (int i = 0; i < _spawned.Length; i++)
            {
                if (!_spawned[(startIndex + i) % _spawned.Length])
                {
                    return (startIndex + i) % _spawned.Length;
                }
            }
            return -1001;
        }

        public void UnspawnAll()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < _spawned.Length; i++)
            {
                _spawned[i] = false;
            }
            spawned = _spawned;
        }

        public override void OnDeserialization()
        {
            Debug.LogWarning("CHARM POOL DESERIALIZATION");
            spawned = spawned;
        }
    }
}