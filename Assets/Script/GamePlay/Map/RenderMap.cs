using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Zenject;

namespace Sand
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class RenderMap : MonoBehaviour
    {
        [Header("Setting")] [SerializeField] public Color32 _backgroundColor;
        [SerializeField] public int _hight;
        [SerializeField] public int _wight;
        [SerializeField] public int _hightGameOver;
        [SerializeField] private BlockManager _blockManager;
        [HideInInspector] public SpriteRenderer _spriteRenderer;
        
        EffectBlock _effectBlock;
        GameRevive _gameRevive;
        SoundManager _soundManager;
        GameVisual _gameVisual;
        
        public Map _map;
        private SandColorMap _colorMap;
        private bool _isSettled = false;
        private bool _isGameStarted = false;
        private bool _hasPlayedTickSound = false;
        private int Idx(int x, int y) => y * _wight + x;
        IDisposable _sandSpawnSub;

        [Inject]
        void Construct(GameRevive gameRevive, EffectBlock effectBlock, SoundManager soundManager, GameVisual gameVisual)
        {
            _gameRevive = gameRevive;
            _effectBlock = effectBlock;
            _soundManager = soundManager;
            _gameVisual = gameVisual;
        }
        
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
            _hasPlayedTickSound = false; 
            if (_map != null)
            {
                _map.SetUpMap(_backgroundColor);
                _map.ApplyTexture(_spriteRenderer);
            }

            if (_colorMap != null)
            {
                _colorMap = new SandColorMap(_map, _wight, _hight, _effectBlock, _soundManager);
                _colorMap.ResetCombo();
            }
            StartGame();
        }

        private void Start()
        {
            _map.SetUpMap(_backgroundColor);
            _map.ApplyTexture(_spriteRenderer);
            _colorMap = new SandColorMap(_map, _wight, _hight, _effectBlock,_soundManager);

            /*_sandSpawnSub = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(1))
                .TimeInterval()
                .Chunk(2, 1)
                .Where(clicks => clicks[1].Interval.TotalSeconds <= 0.5f)
                .ThrottleFirst(TimeSpan.FromSeconds(0.25f))
                .Subscribe(_ => _blockManager.SpawnSandWithRandomShape(_map, _spriteRenderer));*/
        }

        private void Update()
        {
            if (!_isGameStarted || _map == null) return;
            SandUpdate();
        }

        private void SandUpdate()
        {
            bool isTick = _map.Tick(4);
            if (isTick) 
            {
                _isSettled = false;
                if (!_hasPlayedTickSound)
                {
                    _soundManager?.OnPlaySound(SoundType.SandDrop);
                    _hasPlayedTickSound = true;
                }
            }
            else
            {
                if (!_isSettled && !_map.IsMovePause)
                {
                    // CheckSandLosingLine();
                    ProcessSettledSand().Forget();
                    _isSettled = true;
                    _hasPlayedTickSound = false; 
                }
            }

            _map.UpdateTexture();
        }

        private async UniTask ProcessSettledSand()
        {
            await _colorMap.SameColorCompleteBands(_backgroundColor);
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            CheckSandLosingLine();
        }

        private void CheckSandLosingLine()
        {
            if (_hight <= _hightGameOver) return;
            bool foundSand = false;
            bool warningSand = false;
            int sandCount = 0;

            List<(int x, int y)> sandCells = new List<(int x, int y)>();
            for (int y = _hightGameOver; y < _hight; y++)
            {
                for (int x = 0; x < _wight; x++)
                {
                    var cell = _map.GetCell(x, y);
                    var cellWarning = _map.GetCell(x,80);
                    if (cell.hasValue == 1)
                    {
                        foundSand = true;
                        sandCount++;
                        sandCells.Add((x, y));
                    }
                    else if (cellWarning.hasValue == 1)
                    {
                        warningSand = true;
                    }
                }
            }

            if (foundSand)
            {
                _gameRevive.OpenPopupRevive();
                // _effectBlock.CheckSandLosingLineWithEffect(_map,_hight, _wight).Forget();
            }

            _gameVisual.WarningSand(warningSand);
        }

        public void MapGameOver()
        {
            _effectBlock.CheckSandLosingLineWithEffect(_map,_hight, _wight).Forget();
            _soundManager.OnPlaySound(SoundType.GameOver);
        }
        
        public SandColorMap GetColorMap()
        {
            if (_colorMap == null)
            {
                _colorMap = new SandColorMap(_map, _wight, _hight, _effectBlock, _soundManager);
            }
            return _colorMap;
        }
        
        private void OnDestroy()
        {
            _map?.Dispose();
            _sandSpawnSub?.Dispose();
        }
    }
}