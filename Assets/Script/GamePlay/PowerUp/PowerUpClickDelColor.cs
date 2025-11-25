using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class PowerUpClickDelColor : MonoBehaviour
    {
        [SerializeField] private RenderMap _renderMaps;
        private SpriteRenderer _spriteRenderer;
        private List<(int x, int y)> connectedComponentDel = new();

        [Inject] private EffectBlock _effectBlock;

        IDisposable _mouseClickSub;
        IDisposable _mouseClickSubWave;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            /*_mouseClickSub = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(2))
                .Subscribe(_ => MousePositionOnMap());*/

            _mouseClickSubWave = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(3))
                .Subscribe(_ => MousePositionOnMap());
        }

        private void OnDestroy()
        {
            _mouseClickSub?.Dispose();
            _mouseClickSubWave?.Dispose();
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
                // await _effectBlock.ShrinkEffectWave(_renderMaps._map, connectedComponentDel, _renderMaps._backgroundColor);
                await _effectBlock.OnClickColor(_renderMaps._map, connectedComponentDel, _renderMaps._backgroundColor);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            _renderMaps._map.IsMovePause = false;
        }
    }
}