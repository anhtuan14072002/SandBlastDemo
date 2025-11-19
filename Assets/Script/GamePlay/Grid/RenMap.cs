using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Sand
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class RenMap : MonoBehaviour
    {
        [Header("Setting")] [SerializeField] private BlockManager _blockManager;
        [SerializeField] private Color32 _backgroundColor;
        [SerializeField] public int _hight;
        [SerializeField] public int _wight;

        [HideInInspector] public SpriteRenderer _spriteRenderer;
        public Map _map;
        private bool _isSettled = false;
        private int Idx(int x, int y) => y * _wight + x;
        private List<(int x, int y)> connectedComponentDel = new();

        IDisposable _sandSpawnSub;
        IDisposable _sandUpdateSub;
        IDisposable _mouseClickSub;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _map = new Map(_wight, _hight);
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            _map.SetUpMap(_backgroundColor);
            _map.ApplyTexture(_spriteRenderer);

            _sandSpawnSub = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(1))
                .TimeInterval()
                .Chunk(2, 1)
                .Where(clicks => clicks[1].Interval.TotalSeconds <= 0.5f)
                .ThrottleFirst(TimeSpan.FromSeconds(0.25f))
                .Subscribe(_ => _blockManager.SpawnSandWithRandomShape(_map, _spriteRenderer));

            _mouseClickSub = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButton(3))
                .Subscribe(_ => LogMousePositionOnMap());
        }

        private void Update()
        {
            SandUpdate();
        }

        private void SandUpdate()
        {
            bool isTick = _map.Tick(4);
            if (isTick) _isSettled = false;
            else
            {
                if (!_isSettled)
                {
                    _map.SameColorCompleteBands(_backgroundColor).Forget();
                    _isSettled = true;
                }
            }

            _map.UpdateTexture();
        }

        private void LogMousePositionOnMap()
        {
            if (_spriteRenderer.sprite == null) return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);
            var spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            var mapX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _wight);
            var mapY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _hight);
            mapX = Mathf.Clamp(mapX, 0, _wight - 1);
            mapY = Mathf.Clamp(mapY, 0, _hight - 1);
            var cell = _map.GetCell(mapX, mapY);
            if (cell.hasValue == 1)
                RemoveSameColorCompleteBands(cell.color);
        }

        private void RemoveSameColorCompleteBands(Color32 targetColor)
        {
            for (int x = 0; x < _wight; x++)
            {
                for (int y = 0; y < _hight; y++)
                {
                    var cell = _map.GetCell(x, y);
                    if (cell.hasValue == 1 && SameColor(cell.color, targetColor))
                    {
                        _map.SetPixelCell(x, y, _backgroundColor);
                        var idx = y * _wight + x;
                        var c = _map.Cells[idx];
                        c.hasValue = 0;
                        c.color = _backgroundColor;
                    }
                }
            }
        }
        /*private void RemoveSameColorCompleteBands(Color32 targetColor)
        {
            connectedComponentDel.Clear();
            CountCellsWithColor(targetColor);
            if (connectedComponentDel.Count > 0)
                _map.ShrinkEffect(connectedComponentDel, _backgroundColor).Forget();
        }*/


        private void CountCellsWithColor(Color32 targetColor)
        {
            for (int x = 0; x < _wight; x++)
            {
                for (int y = 0; y < _hight; y++)
                {
                    var cell = _map.GetCell(x, y);
                    if (cell.hasValue == 1 && SameColor(cell.color, targetColor))
                        connectedComponentDel.Add((x, y));
                }
            }
        }


        private bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
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