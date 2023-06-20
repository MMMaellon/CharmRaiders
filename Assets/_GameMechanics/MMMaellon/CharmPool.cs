
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
        public CharmSpawn[] spawns = new CharmSpawn[0];
        public ChildAttachmentState[] children = new ChildAttachmentState[0];
        OpenableDoor door;
        int unspawnedIndex;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(spawnedCharms))]
        public bool[] _spawnedCharms = null;
        public bool[] spawnedCharms
        {
            get => _spawnedCharms;
            set
            {
                _spawnedCharms = value;
                for (int i = 0; i < _spawnedCharms.Length; i++)
                {
                    if(!_spawnedCharms[i] && children[i].sync.IsLocalOwner() && (children[i].sync.IsAttachedToPlayer() || children[i].sync.state >= SmartObjectSync.STATE_CUSTOM))
                    {
                        children[i].sync.Respawn();
                    }
                    children[i].gameObject.SetActive(_spawnedCharms[i]);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        VRC.SDK3.Data.DataDictionary spawnCharmDict = new VRC.SDK3.Data.DataDictionary();
        VRC.SDK3.Data.DataDictionary spawnPointDict = new VRC.SDK3.Data.DataDictionary();
        public void Start()
        {
            if (_spawnedCharms == null || _spawnedCharms.Length != children.Length)
            {
                _spawnedCharms = new bool[children.Length];
                for (int i = 0; i < _spawnedCharms.Length; i++)
                {
                    _spawnedCharms[i] = children[i].gameObject.activeSelf;
                    spawnCharmDict.Add(i, children[i]);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
            for (int i = 0; i < spawns.Length; i++)
            {
                spawnPointDict.Add(i, spawns[i]);
            }
        }
        VRC.SDK3.Data.DataDictionary selectedSpawnDict = new VRC.SDK3.Data.DataDictionary();
        int pointsToRemove = 0;
        VRC.SDK3.Data.DataList selectedSpawnKeys;
        VRC.SDK3.Data.DataList selectedCharmKeys;
        VRC.SDK3.Data.DataToken unspawnedIndexToken;
        public void SpawnCharms()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _spawnedCharms = new bool[children.Length];//will be set everything to false
            selectedSpawnDict = spawnPointDict.ShallowClone();
            pointsToRemove = spawns.Length - Mathf.CeilToInt(spawns.Length * spawnChance);
            Debug.LogWarning("Removing " + pointsToRemove + " charms. Selected " + selectedSpawnDict.Count + " charms.");
            while (pointsToRemove > 0 && selectedSpawnDict.Count > 0)
            {
                if (!selectedSpawnDict.Remove(Random.Range(0, spawns.Length)))
                {
                    continue;
                }
                pointsToRemove--;
            }
            selectedSpawnKeys = selectedSpawnDict.GetKeys();
            selectedCharmKeys = spawnCharmDict.GetKeys().ShallowClone();
            for (int i = 0; i < selectedSpawnKeys.Count; i++)
            {
                if (selectedCharmKeys.Count <= 0)
                {
                    Debug.LogWarning("No charms left to spawn.");
                    break;
                }
                unspawnedIndexToken = selectedCharmKeys[Random.Range(0, selectedCharmKeys.Count)];
                selectedCharmKeys.Remove(unspawnedIndexToken);
                unspawnedIndex = unspawnedIndexToken.Int;
                _spawnedCharms[unspawnedIndex] = true;
                children[unspawnedIndex].gameObject.SetActive(true);
                Debug.LogWarning("Spawning charm " + children[unspawnedIndex].name + " at " + selectedSpawnKeys[i].Int + " spawn");
                spawns[selectedSpawnKeys[i].Int].SpawnCharm(children[unspawnedIndex]);
            }
            spawnedCharms = _spawnedCharms;
        }

        int startIndex = 0;
        public int GetUnspawnedCharmIndex()
        {
            startIndex = Random.Range(0, _spawnedCharms.Length);
            for (int i = 0; i < _spawnedCharms.Length; i++)
            {
                if (!_spawnedCharms[(startIndex + i) % _spawnedCharms.Length])
                {
                    return (startIndex + i) % _spawnedCharms.Length;
                }
            }
            return -1001;
        }
        public int GetUnspawnedSpawnIndex()
        {
            startIndex = Random.Range(0, _spawnedCharms.Length);
            for (int i = 0; i < _spawnedCharms.Length; i++)
            {
                if (!_spawnedCharms[(startIndex + i) % _spawnedCharms.Length])
                {
                    return (startIndex + i) % _spawnedCharms.Length;
                }
            }
            return -1001;
        }

        public void UnspawnAll()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < _spawnedCharms.Length; i++)
            {
                _spawnedCharms[i] = true;
                children[i].gameObject.SetActive(true);
                children[i].sync.Respawn();
            }
            spawnedCharms = _spawnedCharms;
        }

        public override void OnDeserialization()
        {
            spawnedCharms = spawnedCharms;
        }
    }
}