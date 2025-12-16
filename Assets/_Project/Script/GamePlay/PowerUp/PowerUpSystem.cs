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
        [SerializeField] private GameObject _iconMagicBrush;
        [SerializeField] public Image _colorMagic;

        [Header("Magic Brush Bounds")]
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;
        [SerializeField] private float _minY;
        [SerializeField] private float _maxY;

        private readonly List<(int x, int y)> _connectedComponentDel = new();

        private SpriteRenderer _spriteRenderer;

        private bool _isDragging;
        private Vector3 _offset;
        private bool _magicBrushActive;

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
            CountDrawData countDrawData, PowerUp PowerUp)
        {
            _renderMaps = renderMap;
            _effectBlock = effectBlock;
            _rewardSystem = rewardSystem;
            _soundManager = soundManager;
            _vibrationManager = vibrationManager;
            _countDrawData = countDrawData;
            _powerUp = PowerUp;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
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
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (!(distance <= radius)) continue;

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

            var cam = Camera.main;
            if (cam != null && _iconMagicBrush != null)
            {
                var screenPos = new Vector3(
                    Screen.width / 2f,
                    Screen.height / 2f,
                    Mathf.Abs(cam.transform.position.z - _iconMagicBrush.transform.position.z));

                var worldPos = cam.ScreenToWorldPoint(screenPos);
                worldPos.z = 0f;
                _iconMagicBrush.transform.position = worldPos;
            }

            if (_colorMagic != null)
                _colorMagic.color = Color.clear;
        }

        public void DisableMagicBrushIcon()
        {
            _magicBrushActive = false;
            _isDragging = false;

            if (_colorMagic != null)
                _colorMagic.color = Color.clear;
        }

        //------MagicBrush Icon Drag & Pick Color-----//
        public void UpdateMagicBrushIcon()
        {
            if (!_magicBrushActive || _iconMagicBrush == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;

                Vector3 iconPos = _iconMagicBrush.transform.position;
                _offset = iconPos - mouseWorldPos;
                _isDragging = true;
            }

            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 mouseForBounds = mouseWorldPos + _offset;
                mouseForBounds.z = 0;

                mouseForBounds.x = Mathf.Clamp(mouseForBounds.x, _minX, _maxX);
                mouseForBounds.y = Mathf.Clamp(mouseForBounds.y, _minY, _maxY);

                _iconMagicBrush.transform.position = mouseForBounds;
                UpdateColorMagicFromMap(mouseForBounds);
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;
            }
        }

        private void UpdateColorMagicFromMap(Vector3 worldPosition)
        {
            if (_spriteRenderer.sprite == null) return;

            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            var spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            var mapX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renderMaps._wight);
            var mapY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renderMaps._hight);

            mapX = Mathf.Clamp(mapX, 0, _renderMaps._wight - 1);
            mapY = Mathf.Clamp(mapY, 0, _renderMaps._hight - 1);

            var cell = _renderMaps._map.GetCell(mapX, mapY);

            if (cell.hasValue == 1)
            {
                _colorMagic.color = cell.color;
                _powerUp.SetActiveButtonUseMagicBrush(true);
            }
            else
            {
                _colorMagic.color = Color.clear;
                _powerUp.SetActiveButtonUseMagicBrush(false);
            }
        }

        public async UniTask PowerUpMagicBrush(Color32 targetColor)
        {
            _renderMaps._map.IsMovePause = true;

            var colorManager = new SandColorMap(_renderMaps._map, _renderMaps._hight,
                _renderMaps._wight, _effectBlock, _soundManager, _countDrawData);
            _connectedComponentDel.Clear();

            for (int x = 0; x < _renderMaps._wight; x++)
            {
                for (int y = 0; y < _renderMaps._hight; y++)
                {
                    var cell = _renderMaps._map.GetCell(x, y);
                    if (cell.hasValue == 1 && colorManager.SameColor(cell.color, targetColor))
                    {
                        _connectedComponentDel.Add((x, y));
                    }
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
                    _renderMaps._backgroundColor);

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
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        var cell = _renderMaps._map.GetCell(x, y);
                        if (cell.hasValue == 1)
                        {
                            cellsToErase.Add((x, y));
                        }
                    }
                }
            }

            if (cellsToErase.Count > 0)
            {
                _soundManager.OnPlaySound(SoundType.Boom);

                await _effectBlock.ShrinkEffectFadeOut(
                    _renderMaps._map,
                    cellsToErase,
                    new Color32(255, 255, 255, 255),
                    _renderMaps._backgroundColor);

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
