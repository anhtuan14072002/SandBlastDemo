    using System;
    using R3;
    using UnityEngine;

    namespace Sand
    {
        [RequireComponent(typeof(SpriteRenderer))]
        public class RenMap : MonoBehaviour
        {
            [Header("Setting")] 
            [SerializeField] private BlockManager _blockManager;
            [SerializeField] private Color32 _backgroundColor;
            [SerializeField] public int _hight;
            [SerializeField] public int _wight;

            [HideInInspector] public SpriteRenderer _spriteRenderer;
            public Map _map;
            IDisposable _sandSpawnSub;
            IDisposable _sandUpdateSub;
            IDisposable _mouseClickSub;

            private void Awake()
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _map = new Map(_wight, _hight);
            }

            private void Start()
            {
                _map.SetUpMap(_backgroundColor);
                _map.ApplyTexture(_spriteRenderer);

                _sandUpdateSub = Observable.EveryUpdate().Subscribe(_ => SandUpdate());
                
                _sandSpawnSub = Observable.EveryUpdate()
                    .Where(_ => Input.GetMouseButton(1))
                    .TimeInterval()
                    .Chunk(2, 1)
                    .Where(clicks => clicks[1].Interval.TotalSeconds <= 0.5f)
                    .ThrottleFirst(TimeSpan.FromSeconds(0.25f))
                    .Subscribe(_ => _blockManager.SpawnSandWithRandomShape(_map, _spriteRenderer));
            }
            
            private void SandUpdate()
            {
                _map.Tick();
                _map.UpdateTexture();
            }

            private void OnDestroy()
            {
                _map?.Dispose();
                _sandSpawnSub?.Dispose();
                _mouseClickSub?.Dispose();
                _sandUpdateSub?.Dispose();
            }
        }
    }