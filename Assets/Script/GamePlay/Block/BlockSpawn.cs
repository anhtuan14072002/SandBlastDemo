using System.Collections.Generic;
using Core;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sand
{
    public class BlockSpawn : GameElement, 
        IReceive<SignalResetAllBlocks>
    {
        [SerializeField] private GameObject[] _prefabBlock;
        [SerializeField] private Transform[] _posSpawn;
        [SerializeField] private Transform _posParentSpawn;
        [SerializeField] private int _poolSizePerPrefab = 5;

        private Queue<GameObject> _pool = new();
        private GameObject[] _currentBlocks;

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
                    _pool.Enqueue(obj);
                }
            }
        }

        private GameObject GetRandomFromPool()
        {
            var randomIndex = Random.Range(0, _pool.Count);
            GameObject result = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                var obj = _pool.Dequeue();
                obj.transform.localScale = Vector3.one * 5;
                if (i == randomIndex) result = obj;
                else _pool.Enqueue(obj);
            }

            return result;
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
            var obj = GetRandomFromPool();
            obj.transform.position = _posSpawn[slot].position;
            obj.SetActive(true);
            Tween.PunchScale(obj.transform, Vector3.one * 4 * 4f, 0.2f, 0.5f, false, Ease.Linear);
            _currentBlocks[slot] = obj;
        }

        public void ReturnBlock(GameObject obj)
        {
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

        public void Receive(in SignalResetAllBlocks signal)
        {
            ResetAllBlocks();
        }
    }
}