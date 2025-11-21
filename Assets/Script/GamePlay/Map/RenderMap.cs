using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sand
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class RenderMap : MonoBehaviour
    {
        [Header("Setting")] [SerializeField] public Color32 _backgroundColor;
        [SerializeField] public int _hight;
        [SerializeField] public int _wight;
        [SerializeField] private BlockManager _blockManager;
        [HideInInspector] public SpriteRenderer _spriteRenderer;

        public Map _map;
        private SandColorMap _colorMap;
        private bool _isSettled = false;
        private bool _isGameStarted = false;

        private int Idx(int x, int y) => y * _wight + x;

        IDisposable _sandSpawnSub;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _map = new Map(_wight, _hight);
            Application.targetFrameRate = 60;
        }

        public void StartGame()
        {
            // if (_isGameStarted) return; 
            _isGameStarted = true;
        }

        public void Reset()
        {
            _isGameStarted = false;
            _isSettled = false;
            if (_map != null)
            {
                _map.SetUpMap(_backgroundColor);
                _map.ApplyTexture(_spriteRenderer);
            }

            if (_colorMap != null)
                _colorMap = new SandColorMap(_map, _wight, _hight);
            StartGame();
        }

        private void Start()
        {
            _map.SetUpMap(_backgroundColor);
            _map.ApplyTexture(_spriteRenderer);
            _colorMap = new SandColorMap(_map, _wight, _hight);

            _sandSpawnSub = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(1))
                .TimeInterval()
                .Chunk(2, 1)
                .Where(clicks => clicks[1].Interval.TotalSeconds <= 0.5f)
                .ThrottleFirst(TimeSpan.FromSeconds(0.25f))
                .Subscribe(_ => _blockManager.SpawnSandWithRandomShape(_map, _spriteRenderer));
        }

        private void Update()
        {
            if (!_isGameStarted || _map == null) return;
            SandUpdate();
        }

        private void SandUpdate()
        {
            bool isTick = _map.Tick(4);
            if (isTick) _isSettled = false;
            else
            {
                if (!_isSettled && !_map.IsMovePause)
                {
                    ProcessSettledSand().Forget();
                    _isSettled = true;
                }
            }

            _map.UpdateTexture();
        }

        private async UniTask ProcessSettledSand()
        {
            await _colorMap.SameColorCompleteBands(_backgroundColor);
        }

        private void OnDestroy()
        {
            _map?.Dispose();
            _sandSpawnSub?.Dispose();
        }
    }
}