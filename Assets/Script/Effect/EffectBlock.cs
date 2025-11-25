using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class EffectBlock : MonoBehaviour
    {
        [Header("Shrink Effect Settings")] [SerializeField]
        private Color32 _shrinkEffectColor;

        [SerializeField] private float _shrinkEffectDuration;
        [SerializeField] private AnimationCurve _shrinkEffectCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private Color32 _shrinkEffectWaveColor;
        [Inject] GameRevive _gameRevive;
        private List<CellSnapshot> _cellSnapshots = new();

        public async UniTask CheckSandLosingLineWithEffect(Map _map, int _hight, int _wight)
        {
            _map.IsMovePause = true;
            Color32 grayColor = new Color32(128, 128, 128, 255);

            for (int y = _hight - 1; y >= 0; y--)
            {
                bool hasChangedInRow = false;

                for (int x = 0; x < _wight; x++)
                {
                    var cell = _map.GetCell(x, y);
                    if (cell.hasValue == 1)
                    {
                        _map.SetPixelCell(x, y, grayColor);
                        hasChangedInRow = true;
                    }
                }

                if (!hasChangedInRow) continue;
                _map.Dirty = true;
                _map.UpdateTexture();
                await UniTask.Delay(TimeSpan.FromMilliseconds(10));
            }

            _map.IsMovePause = false;
            // Global.Send(new SignalOpenPopupGameOver());
        }

        public async UniTask ShrinkEffect(Map map, List<(int x, int y)> cellsList, Color32 finalColor)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            while (elapsed < _shrinkEffectDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = elapsed / _shrinkEffectDuration;
                var curveValue = _shrinkEffectCurve.Evaluate(normalizedTime);
                var alpha = (byte)(255 * curveValue);

                var r = _shrinkEffectColor.r;
                var g = _shrinkEffectColor.g;
                var b = _shrinkEffectColor.b;
                var a = alpha;

                foreach (var (cx, cy) in cellsList)
                {
                    map.SetPixelCell(cx, cy, new Color32(r, g, b, a));
                }

                map.Dirty = true;
                map.UpdateTexture();
                await UniTask.Yield();
            }

            foreach (var (cx, cy) in cellsList)
            {
                map.ClearPixelCell(cx, cy, finalColor);
            }

            map.Dirty = true;
            map.UpdateTexture();
        }
        
        public async UniTask ShrinkEffectWave(Map map, List<(int x, int y)> cellsList,
            Color32 backgroundColor)
        {
            var localCells = new List<(int x, int y)>(cellsList);
            float duration = 0.4f;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            foreach (var (cx, _) in cellsList)
            {
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
            }

            if (minX > maxX)
                return;

            for (int i = 0; i < 2; i++)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float threshold = Mathf.Lerp(minX - 1, maxX + 1, t);

                    foreach (var (cx, cy) in localCells)
                    {
                        if (cx <= threshold)
                        {
                            map.SetPixelCell(cx, cy, Color.white);

                        }
                        else
                        {
                            var snap = _cellSnapshots.Find(s => s.x == cx && s.y == cy);
                            map.SetPixelCell(cx, cy, snap.color);
                            
                        }
                    }

                    map.Dirty = true;
                    map.UpdateTexture();
                    await UniTask.Yield();
                }
                RestoreOriginalColors(map, backgroundColor);;
                map.Dirty = true;
                map.UpdateTexture();

                await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            }
            RestoreOriginalColors(map, backgroundColor);

            map.Dirty = true;
            map.UpdateTexture();
        }

        public void CacheSnapshot(Map map, List<(int x, int y)> cellsList)
        {
            _cellSnapshots.Clear();

            foreach (var (cx, cy) in cellsList)
            {
                var cell = map.GetCell(cx, cy);
                _cellSnapshots.Add(new CellSnapshot
                {
                    x = cx,
                    y = cy,
                    color = cell.color,
                    hasValue = cell.hasValue
                });
            }
        }

        public async UniTask OnClickColor(Map map, List<(int x, int y)> selectedCells,
            Color32 backgroundColor)
        {
            CacheSnapshot(map, selectedCells);
            await ShrinkEffectWave(map, selectedCells, backgroundColor);
        }
        private void RestoreOriginalColors(Map map, Color32 backgroundColor)
        {
            foreach (var snap in _cellSnapshots)
            {
                if (snap.hasValue == 1)
                {
                    map.SetPixelCell(snap.x, snap.y, snap.color);
                }
                else
                {
                    map.ClearPixelCell(snap.x, snap.y, backgroundColor);
                }
            }
        }

    }

    public struct CellSnapshot
    {
        public int x, y;
        public Color32 color;
        public int hasValue;
    }
}