using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Runtime;
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
        [SerializeField] public int _interations = 4;
        [SerializeField] private int _pixelsPerCell;
        [SerializeField] private int _borderThickness;
        [SerializeField] private BlockManager _blockManager;
        [HideInInspector] public SpriteRenderer _spriteRenderer;

        EffectBlock _effectBlock;
        GameRevive _gameRevive;
        SoundManager _soundManager;
        GameVisual _gameVisual;
        SaveMapData _saveMapData;
        SaveService _saveService;
        CountDrawData _countDrawData; 

        public Map _map;
        private SandColorMap _colorMap;
        private bool _isSettled = false;
        private bool _isGameStarted = false;
        private bool _hasPlayedTickSound = false;
        private bool _isCheckingGameOver = false;
        private int Idx(int x, int y) => y * _wight + x;
        IDisposable _sandSpawnSub;
        private bool _pendingGameOverCheck = false;
        private float _lastSandMovementTime = 0f;

        [Inject]
        void Construct(GameRevive gameRevive, EffectBlock effectBlock, SoundManager soundManager, GameVisual gameVisual,
            SaveMapData saveMapData, SaveService saveService, CountDrawData countDrawData)
        {
            _gameRevive = gameRevive;
            _effectBlock = effectBlock;
            _soundManager = soundManager;
            _gameVisual = gameVisual;
            _saveMapData = saveMapData;
            _saveService = saveService;
            _countDrawData = countDrawData;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _map = new Map(_wight, _hight, _pixelsPerCell, _borderThickness);
            Application.targetFrameRate = 60;
        }
        private void Start()
        {
            _map.SetUpMap(_backgroundColor);
            _map.ApplyTexture(_spriteRenderer);
            _colorMap = new SandColorMap(_map, _wight, _hight, _effectBlock, _soundManager, _countDrawData);
            _saveMapData.LoadDataMap();
            AdManager.Instance.ShowBanner();
        }

        private void Update()
        {
            if (!_isGameStarted || _map == null) return;
            SandUpdate();
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
                _colorMap = new SandColorMap(_map, _wight, _hight, _effectBlock, _soundManager, _countDrawData);
                _colorMap.ResetCombo();
            }

            StartGame();
        }


        private void SandUpdate()
        {
            bool isTick = _map.Tick(_interations);
            if (isTick)
            {
                _isSettled = false;
                _lastSandMovementTime = Time.time;
                
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
            await WaitForSandToSettle();
            CheckSandLosingLineDelayed().Forget();
        }

        private async UniTask CheckSandLosingLineDelayed()
        {
            if (_pendingGameOverCheck) return;
            _pendingGameOverCheck = true;
            float waitTime = 0.5f;
            while (Time.time - _lastSandMovementTime < waitTime)
            {
                await UniTask.Yield(); 
            }
            CheckSandLosingLine();
            
            _pendingGameOverCheck = false;
        }

        private async UniTask WaitForSandToSettle()
        {
            await UniTask.NextFrame();
            bool isMoving = true;
            while (isMoving)
            {
                isMoving = _map.Tick(4);
                if (isMoving)
                {
                    _map.UpdateTexture();
                    await UniTask.NextFrame();
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        }

        private void CheckSandLosingLine()
        {
            if (_hight <= _hightGameOver) return;
            bool foundSand = false;
            bool warningSand = false;

            for (int y = _hightGameOver; y < _hight; y++)
            {
                for (int x = 0; x < _wight; x++)
                {
                    var cell = _map.GetCell(x, y);
                    if (cell.hasValue == 1)
                    {
                        foundSand = true;
                        break;
                    }
                    
                    var cellWarning = _map.GetCell(x, 75);
                    if (cellWarning.hasValue == 1)
                    {
                        warningSand = true;
                    }
                }
                if (foundSand) break;
            }

            if (foundSand)
            {
                _gameRevive.OpenPopupRevive();
            }
            
            Global.Send(new SignalWarningSand(){IsWarning = warningSand});
        }

        private async UniTask DelayGameOverCheck(List<(int x, int y)> sandCells, int sandCount)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
            int stillHighCount = 0;
            foreach (var (x, y) in sandCells)
            {
                var cell = _map.GetCell(x, y);
                if (cell.hasValue == 1 && y >= _hightGameOver)
                {
                    stillHighCount++;
                }
            }
            if (stillHighCount > sandCount / 2)
            {
                _gameRevive.OpenPopupRevive();
            }
            _isCheckingGameOver = false;
        }
        
        public void MapGameOver()
        {
            _effectBlock.CheckSandLosingLineWithEffect(_map, _hight, _wight).Forget();
            _soundManager.OnPlaySound(SoundType.GameOver);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _saveMapData?.SaveDataMap();
                _saveService.Save();
                Debug.Log("thoát");
            }
            else
            {
                Debug.Log("vào");
            }
        }
        
        private void OnApplicationQuit()
        {
            _saveMapData?.SaveDataMap();
            _saveService.Save();
        }
        
        private void OnDestroy()
        {
            _map?.Dispose();
            _sandSpawnSub?.Dispose();
        }
    }
}