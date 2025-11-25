using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class PowerUpClickDelColor : MonoBehaviour
    {
        [SerializeField] private GameObject _iconMagicBrush;
        [SerializeField] private GameObject _popupSkillMagicBrush;
        [SerializeField] private Button _btnOpenSkillMagicBrush;
        [SerializeField] private Button _btnCloseSkillMagicBrush;
        [SerializeField] private Image _colorMagic;

        [Header("Magic Brush Bounds")] 
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;
        [SerializeField] private float _minY;
        [SerializeField] private float _maxY;

        [Header("Button Skill")] 
        [SerializeField] private Button _buttonUseRemove;
        [SerializeField] private int _priceSkillMagicBrush;

        private List<(int x, int y)> connectedComponentDel = new();
        private SpriteRenderer _spriteRenderer;
        private bool _isDragging;
        private Vector3 _startPos;
        private Vector3 _offset;

        RewardSystem _rewardSystem;
        EffectBlock _effectBlock;
        RenderMap _renderMaps;
        
        IDisposable _mouseClickSub;
        IDisposable _mouseClickSubWave;
        IDisposable _magicBrushSub;
        IDisposable _dragSub;

        [Inject]
        void Construct(RenderMap renderMap, EffectBlock effectBlock, RewardSystem rewardSystem)
        {
            _renderMaps = renderMap;
            _effectBlock = effectBlock;
            _rewardSystem = rewardSystem;
        }

        private void Start()
        {
            _buttonUseRemove.onClick.AddListener(() => RemoveSameColorCompleteBands(_colorMagic.color).Forget());
            _btnOpenSkillMagicBrush.onClick.AddListener(OpenSkillMagicBrush);
            _btnCloseSkillMagicBrush.onClick.AddListener(CloseSkillMagicBrush);
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
            /*_mouseClickSubWave = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButtonDown(3))
                .Subscribe(_ => MousePositionOnMap());*/
            _magicBrushSub = Observable.EveryUpdate().Subscribe(_ => UpdateMagicBrushIcon());
        }

        public void UpdateMagicBrushIcon()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
                UpdateColorMagicFromMap(mouseForBounds).Forget();
            }
            
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;
            }
        }

        private async UniTask UpdateColorMagicFromMap(Vector3 worldPosition)
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
            }
        }

        private void MousePositionOnMap()
        {
            if (_spriteRenderer.sprite == null) return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);
            var spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            var mapX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _renderMaps._wight);
            var mapY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _renderMaps._hight);
            mapX = Mathf.Clamp(mapX, 0, _renderMaps._wight - 1);
            mapY = Mathf.Clamp(mapY, 0, _renderMaps._hight - 1);

            var cell = _renderMaps._map.GetCell(mapX, mapY);
            if (cell.hasValue == 1) RemoveSameColorCompleteBands(cell.color).Forget();
        }

        private async UniTask RemoveSameColorCompleteBands(Color32 targetColor)
        {
            UseSkill();
            _renderMaps._map.IsMovePause = true;
            var colorManager = new SandColorMap(_renderMaps._map, _renderMaps._hight, _renderMaps._wight, _effectBlock);
            connectedComponentDel.Clear();
            for (int x = 0; x < _renderMaps._wight; x++)
            {
                for (int y = 0; y < _renderMaps._hight; y++)
                {
                    var cell = _renderMaps._map.GetCell(x, y);
                    if (cell.hasValue == 1 && colorManager.SameColor(cell.color, targetColor))
                    {
                        connectedComponentDel.Add((x, y));
                    }
                }
            }

            if (connectedComponentDel.Count > 0)
            {
                await _effectBlock.ShrinkEffectFadeOut(_renderMaps._map, connectedComponentDel, targetColor,
                    _renderMaps._backgroundColor);
                // await _effectBlock.ShrinkEffectWave(_renderMaps._map, connectedComponentDel, targetColor);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            _renderMaps._map.IsMovePause = false;
        }
        private void OpenSkillMagicBrush()
        {
            _popupSkillMagicBrush.SetActive(true);
        }
        private void UseSkill()
        {
            _rewardSystem.DeductGems(_priceSkillMagicBrush);
            _popupSkillMagicBrush.SetActive(false);
        }

        private void CloseSkillMagicBrush()
        {
            _popupSkillMagicBrush.SetActive(false);
        }
        private void OnDestroy()
        {
            _mouseClickSub?.Dispose();
            _mouseClickSubWave?.Dispose();
            _magicBrushSub?.Dispose();
            _dragSub?.Dispose();
        }
    }
}