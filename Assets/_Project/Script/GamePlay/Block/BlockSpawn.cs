using System.Collections.Generic;
using Core;
using PrimeTween;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Sand
{
    public class BlockSpawn : GameElement,
        IReceive<SignalResetAllBlocks>
    {
        [SerializeField] public GameObject[] _prefabBlock;
        [SerializeField] private Transform[] _posSpawn;
        [SerializeField] private Transform _posParentSpawn;
        [SerializeField] private int _poolSizePerPrefab = 5;

        public GameObject[] PrefabBlocks => _prefabBlock;

        private Queue<GameObject> _pool = new();
        private GameObject[] _currentBlocks;

        CheckLevelScore _checkLevelScore;

        [Inject]
        void Construct(CheckLevelScore checkLevelScore)
        {
            _checkLevelScore = checkLevelScore;
        }

        private void Start()
        {
            _currentBlocks = new GameObject[_posSpawn.Length];
            InitPool();
            SpawnAllSlots();
        }

        private void InitPool()
        {
            for (int i = 0; i < _prefabBlock.Length; i++)
            {
                for (int j = 0; j < _poolSizePerPrefab; j++)
                {
                    var obj = Instantiate(_prefabBlock[i], transform.position, Quaternion.identity);
                    obj.transform.SetParent(_posParentSpawn.transform);
                    obj.SetActive(false);

                    var instance = obj.GetComponent<BlockInfo>();
                    if (instance == null) instance = obj.AddComponent<BlockInfo>();
                    instance.PrefabIndex = i;
                    _pool.Enqueue(obj);
                }
            }
        }

        private int BlockMaxInLevel()
        {
            if (_prefabBlock == null || _prefabBlock.Length == 0)
                return 0;

            if (_checkLevelScore == null)
                return _prefabBlock.Length;

            int level = _checkLevelScore.CurrentLevel;

            return level switch
            {
                0 => Mathf.Min(5, _prefabBlock.Length),
                1 => Mathf.Min(9, _prefabBlock.Length),
                2 => Mathf.Min(13, _prefabBlock.Length),
                _ => _prefabBlock.Length
            };
        }

        private void SpawnAllSlots()
        {
            for (int i = 0; i < _posSpawn.Length; i++)
            {
                SpawnAtSlot(i);
            }
        }

        private void SpawnAtSlot(int slot)
        {
            var obj = GetBlockByLevel();
            if (obj == null) return;

            obj.transform.position = _posSpawn[slot].position;
            obj.SetActive(true);
            Tween.PunchScale(obj.transform, Vector3.one * 4 * 4f, 0.2f, 0.5f, false, Ease.Linear);
            _currentBlocks[slot] = obj;
        }

        private GameObject GetBlockByLevel()
        {
            int maxPrefabIndex = BlockMaxInLevel();
            if (maxPrefabIndex <= 0) return null;

            // int randomPrefabIndex = Random.Range(0, maxPrefabIndex);
            var randomPrefabIndex = GetRandomPrefabIndex(maxPrefabIndex);
            var pooledObject = GetFromPoolByPrefabIndex(randomPrefabIndex);
            if (pooledObject != null)
            {
                return pooledObject;
            }

            var newObj = Instantiate(_prefabBlock[randomPrefabIndex], transform.position, Quaternion.identity);
            newObj.transform.SetParent(_posParentSpawn.transform);
            newObj.transform.localScale = Vector3.one * 5;

            var instance = newObj.GetComponent<BlockInfo>();
            if (instance == null) instance = newObj.AddComponent<BlockInfo>();
            instance.PrefabIndex = randomPrefabIndex;

            return newObj;
        }

        private GameObject GetFromPoolByPrefabIndex(int prefabIndex)
        {
            if (_pool.Count == 0) return null;

            int count = _pool.Count;
            for (int i = 0; i < count; i++)
            {
                var obj = _pool.Dequeue();
                var instance = obj.GetComponent<BlockInfo>();

                if (instance != null && instance.PrefabIndex == prefabIndex)
                {
                    return obj;
                }
                else
                {
                    _pool.Enqueue(obj);
                }
            }

            return null;
        }

        public void ReturnBlock(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.localScale = Vector3.one * 5f;
            obj.SetActive(false);
            _pool.Enqueue(obj);

            for (int i = 0; i < _currentBlocks.Length; i++)
            {
                if (_currentBlocks[i] == obj)
                {
                    _currentBlocks[i] = null;
                    break;
                }
            }

            bool allEmpty = true;
            for (int i = 0; i < _currentBlocks.Length; i++)
            {
                if (_currentBlocks[i] != null)
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty)
            {
                SpawnAllSlots();
            }
        }

        public void ResetAllBlocks()
        {
            for (int i = 0; i < _currentBlocks.Length; i++)
            {
                if (_currentBlocks[i] != null)
                {
                    var obj = _currentBlocks[i];
                    obj.transform.localScale = Vector3.one * 5f;
                    obj.SetActive(false);
                    _pool.Enqueue(obj);
                    _currentBlocks[i] = null;
                }
            }

            SpawnAllSlots();
        }

        // lấy tỉ leej của các khối
        // sau đó cộng tổng các khối 
        // lấy tỉ lệ của các khối
        // random lấy ra 1 khối
        public int GetRandomPrefabIndex(int maxIndex)
        {
            var totalWeight= 0f;
            for (int i = 0; i < maxIndex; i++)
            {
                var weight = GetPrefabRateSpawn(i);
                totalWeight += weight;
            }
            
            if (totalWeight <= 0f)
                return Random.Range(0, maxIndex);

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < maxIndex; i++)
            {
                float weight = GetPrefabRateSpawn(i);
                currentWeight += weight;
                if (randomValue <= currentWeight) return i;
            }
            return maxIndex - 1;
        }
        public float GetPrefabRateSpawn(int prefabIndex)
        {
            var prefab = _prefabBlock[prefabIndex].GetComponent<BlockInfo>();
            if (prefab != null & prefab.RateSpawn > 0)
                return prefab.RateSpawn;
            return 1;
        }
        
        public void Receive(in SignalResetAllBlocks signal)
        {
            ResetAllBlocks();
        }
    }
}