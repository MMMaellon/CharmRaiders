
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), DefaultExecutionOrder(1000)]//has to come last so that other network events can come in
    public class CharmPool : UdonSharpBehaviour
    {
        public float spawnChance = 0.5f;
        public CharmSpawn[] spawns = new CharmSpawn[0];
        public Charm[] charms = new Charm[0];
        int unspawnedIndex;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(spawnedCharms))]
        public bool[] _spawnedCharms = null;
        bool firstNetworkSettle = true;
        bool networkSettleLoop = false;
        public bool[] spawnedCharms
        {
            get => _spawnedCharms;
            set
            {
                _spawnedCharms = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                if (_spawnedCharms == null)
                {
                    return;
                }
                // if (firstNetworkSettle){
                //     if (!networkSettleLoop && !Networking.LocalPlayer.IsOwner(gameObject) && !Networking.IsNetworkSettled)
                //     {
                //         networkSettleLoop = true;
                //         SendCustomEventDelayedSeconds(nameof(CheckForNetworkSettle), 1);
                //     }
                //     return;
                // }
                for (int i = 0; i < _spawnedCharms.Length; i++)
                {
                    charms[i].gameObject.SetActive(_spawnedCharms[i]);
                    charms[i].ResetDisappearTimer();
                    if (!_spawnedCharms[i] && charms[i].bagSetter.child.sync.IsLocalOwner())
                    {
                        charms[i].bagSetter.child.sync.Respawn();
                    }
                }
            }
        }
        VRC.SDK3.Data.DataDictionary spawnCharmDict = new VRC.SDK3.Data.DataDictionary();
        VRC.SDK3.Data.DataDictionary spawnPointDict = new VRC.SDK3.Data.DataDictionary();
        public void Start()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                firstNetworkSettle = false;
                if (_spawnedCharms == null || _spawnedCharms.Length != charms.Length)
                {
                    _spawnedCharms = new bool[charms.Length];
                    for (int i = 0; i < _spawnedCharms.Length; i++)
                    {
                        _spawnedCharms[i] = charms[i].gameObject.activeSelf;
                        spawnCharmDict.Add(i, charms[i]);
                    }
                    spawnedCharms = _spawnedCharms;
                }
            }
            for (int i = 0; i < spawns.Length; i++)
            {
                spawnPointDict.Add(i, spawns[i]);
            }
        }

        public void CheckForNetworkSettle()
        {
            if (Networking.IsNetworkSettled)
            {
                firstNetworkSettle = false;
                spawnedCharms = spawnedCharms;
                return;
            }
            SendCustomEventDelayedSeconds(nameof(CheckForNetworkSettle), 1);
        }
        VRC.SDK3.Data.DataDictionary selectedSpawnDict = new VRC.SDK3.Data.DataDictionary();
        int pointsToRemove = 0;
        VRC.SDK3.Data.DataList selectedSpawnKeys;
        VRC.SDK3.Data.DataList selectedCharmKeys;
        VRC.SDK3.Data.DataToken unspawnedIndexToken;
        public void SpawnCharms()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _spawnedCharms = new bool[charms.Length];//will be set everything to false
            selectedSpawnDict = spawnPointDict.ShallowClone();
            pointsToRemove = spawns.Length - Mathf.CeilToInt(spawns.Length * spawnChance);
            // Debug.LogWarning("Removing " + pointsToRemove + " charms. Selected " + selectedSpawnDict.Count + " charms.");
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
                    // Debug.LogWarning("No charms left to spawn.");
                    break;
                }
                unspawnedIndexToken = selectedCharmKeys[Random.Range(0, selectedCharmKeys.Count)];
                selectedCharmKeys.Remove(unspawnedIndexToken);
                unspawnedIndex = unspawnedIndexToken.Int;
                _spawnedCharms[unspawnedIndex] = true;
                charms[unspawnedIndex].gameObject.SetActive(true);
                // Debug.LogWarning("Spawning charm " + charms[unspawnedIndex].name + " at " + selectedSpawnKeys[i].Int + " spawn");
                spawns[selectedSpawnKeys[i].Int].SpawnCharm(charms[unspawnedIndex].bagSetter.child);
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

        public void DespawnAll()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < _spawnedCharms.Length; i++)
            {
                _spawnedCharms[i] = true;
                charms[i].gameObject.SetActive(false);
                charms[i].bagSetter.child.sync.Respawn();
            }
            spawnedCharms = _spawnedCharms;
        }
        public void RespawnAll()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < _spawnedCharms.Length; i++)
            {
                _spawnedCharms[i] = true;
            }
            spawnedCharms = _spawnedCharms;
        }

        public void Despawn(int index)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (index < 0 || index >= _spawnedCharms.Length || !_spawnedCharms[index])
            {
                return;
            }
            _spawnedCharms[index] = false;
            spawnedCharms = _spawnedCharms;
        }

        public override void OnDeserialization()
        {
            spawnedCharms = spawnedCharms;
        }
    }
}