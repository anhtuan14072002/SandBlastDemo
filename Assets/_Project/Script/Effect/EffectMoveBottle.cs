using System;
using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace Sand
{
    public class EffectMoveBottle : GameElement,
        IReceive<SignalMoveBottleDraw>
    {
        [Header("Bottle Prefab")]
        [SerializeField] private GameObject _bottlePrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _poolSize = 3;

        [Header("Positions")]
        [SerializeField] private Transform _spawnPos;
        [SerializeField] private Transform _targetPos;
        [SerializeField] private Transform _originalPos;

        [Header("Settings")]
        [SerializeField] private float _moveDuration = 0.25f;
        [SerializeField] private float _returnDuration = 0.25f;
        [SerializeField] private float _delayBeforeReturn = 1f;
        [SerializeField] private float _pourAngle = 90f;

        private GameObject _bottleInstance;

        private void Awake()
        {
            if (_bottlePrefab == null)
            {
                Debug.LogError("[EffectMoveBottle] _bottlePrefab is null!");
                return;
            }

            Vector3 scale = _bottlePrefab.transform.localScale;
            SpawnPool.InitPool(_bottlePrefab, _poolSize, scale);
        }

        public void Receive(in SignalMoveBottleDraw signal)
        {
            if (_bottlePrefab == null || _spawnPos == null || _targetPos == null || _originalPos == null)
            {
                Debug.LogWarning("[EffectMoveBottle] Missing references!");
                return;
            }

            if (_bottleInstance != null)
            {
                SpawnPool.Despawn(_bottlePrefab, _bottleInstance);
                _bottleInstance = null;
            }

            Vector3 scale = _bottlePrefab.transform.localScale;
            _bottleInstance = SpawnPool.Spawn(
                _bottlePrefab,
                _spawnPos.position,
                Quaternion.identity,
                scale
            );

            Move();
        }

        private void Move()
        {
            if (_bottleInstance == null) return;

            Tween.Position(_bottleInstance.transform, _targetPos.position, _moveDuration, Ease.OutQuad);
            Tween.LocalRotation(_bottleInstance.transform, Quaternion.Euler(0, 0, _pourAngle), 0.2f);
            EndMove().Forget();
        }

        private async UniTask EndMove()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_delayBeforeReturn));
            if (_bottleInstance == null) return;
            // Tween.LocalRotation(_bottleInstance.transform, Quaternion.identity, 0.2f);
            // Tween.Position(_bottleInstance.transform, _originalPos.position, _returnDuration, Ease.OutQuad);

            await UniTask.Delay(TimeSpan.FromSeconds(_returnDuration));
            if (_bottleInstance != null)
            {
                SpawnPool.Despawn(_bottlePrefab, _bottleInstance);
                _bottleInstance = null;
            }
        }
    }
}
