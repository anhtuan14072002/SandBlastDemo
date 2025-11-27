using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sand
{
    public class EffectBlock : MonoBehaviour
    {
        [Header("Shrink Effect Settings")] [SerializeField]
        private AnimationCurve _shrinkEffectCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [SerializeField] private Color32 _shrinkEffectColor;
        [SerializeField] private Color32 _shrinkEffectWaveColor;
        [SerializeField] private float _shrinkEffectDuration;

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

        public async UniTask ShrinkEffectFadeOut(Map map, List<(int x, int y)> cellsList, Color32 originalColor,
            Color32 finalColor)
        {
            float elapsed = 0f;
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
                map.SetPixelCell(cx, cy, originalColor);
                map.ClearPixelCell(cx, cy, finalColor);
            }

            map.Dirty = true;
            map.UpdateTexture();
        }

        public async UniTask ShrinkEffectWave(Map map, List<(int x, int y)> cellsList, Color32 finalColor)
        {
            float duration = 0.5f;
            int minX = int.MaxValue;
            int maxX = int.MinValue;

            foreach (var (cx, _) in cellsList)
            {
                if (cx < minX) minX = cx;
                if (cx > maxX) maxX = cx;
            }

            if (minX > maxX) return;
            map.IsMovePause = true;
            while (true)
            {
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    float threshold = Mathf.Lerp(minX - 1, maxX + 1, t);

                    foreach (var (cx, cy) in cellsList)
                    {
                        if (cx <= threshold)
                        {
                            map.SetPixelCell(cx, cy, finalColor);
                        }
                        else
                        {
                            map.SetPixelCell(cx, cy, new Color32(255, 255, 255, 255));
                        }
                    }

                    map.Dirty = true;
                    map.UpdateTexture();

                    await UniTask.Yield();
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        public async UniTask CircleEraseEffect(Map map, int centerX, int centerY, float radius, Color32 finalColor)
        {
            map.IsMovePause = true;

            List<(int x, int y)> cellsToErase = new List<(int x, int y)>();
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(centerX + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radius));
            int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(centerY + radius));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        var cell = map.GetCell(x, y);
                        if (cell.hasValue == 1)
                        {
                            cellsToErase.Add((x, y));
                        }
                    }
                }
            }

            float elapsed = 0f;
            while (elapsed < _shrinkEffectDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = elapsed / _shrinkEffectDuration;
                var curveValue = 1f - _shrinkEffectCurve.Evaluate(normalizedTime); 
                var alpha = (byte)(255 * curveValue);

                foreach (var (cx, cy) in cellsToErase)
                {
                    var cell = map.GetCell(cx, cy);
                    if (cell.hasValue == 1)
                    {
                        var r = cell.color.r;
                        var g = cell.color.g;
                        var b = cell.color.b;
                        map.SetPixelCell(cx, cy, new Color32(r, g, b, alpha));
                    }
                }

                map.Dirty = true;
                map.UpdateTexture();
                await UniTask.Yield();
            }

            foreach (var (cx, cy) in cellsToErase)
            {
                map.ClearPixelCell(cx, cy, finalColor);
            }

            map.Dirty = true;
            map.UpdateTexture();
            map.IsMovePause = false;
        }
    }
}