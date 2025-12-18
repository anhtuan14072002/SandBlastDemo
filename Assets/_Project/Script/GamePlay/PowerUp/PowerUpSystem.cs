using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class PowerUpSystem : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _iconMagicBrush;
        [SerializeField] public Image _colorMagic;

        [Header("Magic Brush Bounds (WORLD)")]
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;
        [SerializeField] private float _minY;
        [SerializeField] private float _maxY;

        private readonly List<(int x, int y)> _connectedComponentDel = new();

        private SpriteRenderer _spriteRenderer;

        private bool _magicBrushActive;
        private bool _isDragging;

        // UI drag refs
        private RectTransform _iconRT;
        private Canvas _canvas;
        private RectTransform _canvasRT;
        private Camera _uiCam;
        private Vector3 _dragOffsetWorldOnCanvas;

        RewardSystem _rewardSystem;
        EffectBlock _effectBlock;
        RenderMap _renderMaps;
        SoundManager _soundManager;
        VibrationManager _vibrationManager;
        CountDrawData _countDrawData;
        PowerUp _powerUp;

        IDisposable _mouseClickSub;
        IDisposable _mouseClickSubWave;
        IDisposable _magicBrushSub;
        IDisposable _dragSub;

        [Inject]
        void Construct(
            RenderMap renderMap,
            EffectBlock effectBlock,
            RewardSystem rewardSystem,
            SoundManager soundManager,
            VibrationManager vibrationManager,
            CountDrawData countDrawData,
            PowerUp powerUp)
        {
            _renderMaps = renderMap;
            _effectBlock = effectBlock;
            _rewardSystem = rewardSystem;
            _soundManager = soundManager;
            _vibrationManager = vibrationManager;
            _countDrawData = countDrawData;
            _powerUp = powerUp;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CacheUIRefs();
        }
        private void CacheUIRefs()
        {
            if (_iconMagicBrush == null) return;

            _iconRT = _iconMagicBrush.GetComponent<RectTransform>();
            _canvas = _iconMagicBrush.GetComponentInParent<Canvas>();
            _canvasRT = _canvas != null ? _canvas.transform as RectTransform : null;

            if (_canvas != null && _canvas.worldCamera != null) _uiCam = _canvas.worldCamera;
            else _uiCam = Camera.main;
        }

        private void Update()
        {
            UpdateMagicBrushIcon();
        }

        //================ BOOM ===================//
        public bool PowerUpBoom()
        {
            if (_spriteRenderer.sprite == null) return false;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            var spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            var mapX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renderMaps._wight);
            var mapY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renderMaps._hight);

            mapX = Mathf.Clamp(mapX, 0, _renderMaps._wight - 1);
            mapY = Mathf.Clamp(mapY, 0, _renderMaps._hight - 1);

            if (HasValidCellsInRadius(mapX, mapY, 20f))
            {
                CircleEraseAtPosition(mapX, mapY, 20f).Forget();
                return true;
            }

            return false;
        }

        private bool HasValidCellsInRadius(int centerX, int centerY, float radius)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            int maxX = Mathf.Min(_renderMaps._wight - 1, Mathf.CeilToInt(centerX + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radius));
            int maxY = Mathf.Min(_renderMaps._hight - 1, Mathf.CeilToInt(centerY + radius));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius) continue;

                    var cell = _renderMaps._map.GetCell(x, y);
                    if (cell.hasValue == 1) return true;
                }
            }

            return false;
        }

        //================ MAGIC BRUSH ===================//

        public void EnableMagicBrushIcon()
        {
            _magicBrushActive = true;
            _isDragging = false;

            if (_iconMagicBrush == null) return;
            CacheUIRefs();
            
            if (_canvasRT != null && _uiCam != null && _iconRT != null)
            {
                var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRT, screenCenter, _uiCam, out var worldOnCanvas))
                {
                    worldOnCanvas.z = _iconRT.position.z;
                    _iconRT.position = worldOnCanvas;
                }
            }

            if (_colorMagic != null)
                _colorMagic.color = Color.clear;

            _powerUp?.SetActiveButtonUseMagicBrush(false);
            FixIconLocalZAndAutoCheck().Forget();
        }

        public void DisableMagicBrushIcon()
        {
            _magicBrushActive = false;
            _isDragging = false;

            if (_colorMagic != null)
                _colorMagic.color = Color.clear;

            _powerUp?.SetActiveButtonUseMagicBrush(false);
        }

        private async UniTaskVoid FixIconLocalZAndAutoCheck()
        {
            if (_iconMagicBrush != null && _iconMagicBrush.transform is RectTransform rt)
            {
                var lp = rt.localPosition;
                lp.z = 0f;
                rt.localPosition = lp;
            }
            AutoCheckMagicColorAtIcon();

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (_iconMagicBrush != null && _iconMagicBrush.transform is RectTransform rt2)
            {
                var lp2 = rt2.localPosition;
                lp2.z = 0f;
                rt2.localPosition = lp2;
            }
            AutoCheckMagicColorAtIcon();
        }

        private void AutoCheckMagicColorAtIcon()
        {
            if (!_magicBrushActive) return;
            if (_iconRT == null) CacheUIRefs();
            if (_iconRT == null) return;
            var sampleWorld = _iconRT.position;
            sampleWorld.z = 0f;
            UpdateColorMagicFromMap(sampleWorld);
        }

        // Drag icon + update color while moving
        public void UpdateMagicBrushIcon()
        {
            if (!_magicBrushActive || _iconMagicBrush == null) return;
            if (_iconRT == null || _canvasRT == null || _uiCam == null) CacheUIRefs();
            if (_iconRT == null || _canvasRT == null || _uiCam == null) return;
            if (Input.GetMouseButtonDown(0))
            {
                bool hitIcon = RectTransformUtility.RectangleContainsScreenPoint(_iconRT, Input.mousePosition, _uiCam);
                if (!hitIcon) return;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRT, Input.mousePosition, _uiCam, out var worldOnCanvas))
                {
                    _dragOffsetWorldOnCanvas = _iconRT.position - worldOnCanvas;
                    _isDragging = true;
                }
            }

            if (_isDragging && Input.GetMouseButton(0))
            {
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRT, Input.mousePosition, _uiCam, out var worldOnCanvas))
                {
                    var targetWorld = worldOnCanvas + _dragOffsetWorldOnCanvas;

                    targetWorld.x = Mathf.Clamp(targetWorld.x, _minX, _maxX);
                    targetWorld.y = Mathf.Clamp(targetWorld.y, _minY, _maxY);

                    targetWorld.z = _iconRT.position.z; 
                    _iconRT.position = targetWorld;

                    var sampleWorld = targetWorld;
                    sampleWorld.z = 0f;
                    UpdateColorMagicFromMap(sampleWorld);
                }
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
                _isDragging = false;
        }

        private void UpdateColorMagicFromMap(Vector3 worldPosOnMapPlane)
        {
            if (_spriteRenderer.sprite == null) return;

            Vector3 localPos = transform.InverseTransformPoint(worldPosOnMapPlane);

            var spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            var mapX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renderMaps._wight);
            var mapY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renderMaps._hight);

            mapX = Mathf.Clamp(mapX, 0, _renderMaps._wight - 1);
            mapY = Mathf.Clamp(mapY, 0, _renderMaps._hight - 1);

            var cell = _renderMaps._map.GetCell(mapX, mapY);

            if (cell.hasValue == 1)
            {
                if (_colorMagic != null) _colorMagic.color = cell.color;
                _powerUp?.SetActiveButtonUseMagicBrush(true);
            }
            else
            {
                if (_colorMagic != null) _colorMagic.color = Color.clear;
                _powerUp?.SetActiveButtonUseMagicBrush(false);
            }
        }

        public async UniTask PowerUpMagicBrush(Color32 targetColor)
        {
            _renderMaps._map.IsMovePause = true;

            var colorManager = new SandColorMap(
                _renderMaps._map,
                _renderMaps._hight,
                _renderMaps._wight,
                _effectBlock,
                _soundManager,
                _countDrawData
            );

            _connectedComponentDel.Clear();

            for (int x = 0; x < _renderMaps._wight; x++)
            {
                for (int y = 0; y < _renderMaps._hight; y++)
                {
                    var cell = _renderMaps._map.GetCell(x, y);
                    if (cell.hasValue == 1 && colorManager.SameColor(cell.color, targetColor))
                        _connectedComponentDel.Add((x, y));
                }
            }

            if (_connectedComponentDel.Count > 0)
            {
                var countCellsWithColor = colorManager.CountCellsWithColor(targetColor);

                _soundManager.OnPlaySound(SoundType.Combo2);
                _vibrationManager.SelectionButton();

                await _effectBlock.ShrinkEffectFadeOut(
                    _renderMaps._map,
                    _connectedComponentDel,
                    targetColor,
                    _renderMaps._backgroundColor
                );

                Global.Send(new SignalScoreOnGame { Score = countCellsWithColor });
                Global.Send(new SignalOpenEffectTextScore { Score = countCellsWithColor });
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            _renderMaps._map.IsMovePause = false;
        }

        //================ VÒNG BOOM ===================//

        private async UniTask CircleEraseAtPosition(int centerX, int centerY, float radius)
        {
            _renderMaps._map.IsMovePause = true;

            List<(int x, int y)> cellsToErase = new();

            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            int maxX = Mathf.Min(_renderMaps._wight - 1, Mathf.CeilToInt(centerX + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radius));
            int maxY = Mathf.Min(_renderMaps._hight - 1, Mathf.CeilToInt(centerY + radius));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius) continue;

                    var cell = _renderMaps._map.GetCell(x, y);
                    if (cell.hasValue == 1) cellsToErase.Add((x, y));
                }
            }

            if (cellsToErase.Count > 0)
            {
                _soundManager.OnPlaySound(SoundType.Boom);
                await _effectBlock.ShrinkEffectFadeOut(_renderMaps._map, cellsToErase, new Color32(255, 255, 255, 255), _renderMaps._backgroundColor);
                _vibrationManager.SelectionButton();
                Global.Send(new SignalScoreOnGame { Score = cellsToErase.Count });
                Global.Send(new SignalOpenEffectTextScore { Score = cellsToErase.Count });
            }

            _renderMaps._map.IsMovePause = false;
        }

        private void OnDestroy()
        {
            _mouseClickSub?.Dispose();
            _mouseClickSubWave?.Dispose();
            _dragSub?.Dispose();
            _magicBrushSub?.Dispose();
        }
    }
}