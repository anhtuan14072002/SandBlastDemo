using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Sand
{
    public class Map : IDisposable
    {
        private Texture2D Texture { get; set; }
        private bool m_isMovePause;
        private NativeArray<Cell> _cells;
        public bool IsMovePause
        {
            get => m_isMovePause;
            set => m_isMovePause = value;
        }

        private bool _dirty = true;
        public bool Dirty
        {
            get => _dirty;
            set => _dirty = value;
        }
        private bool _isGameOver;
        private int m_width, m_height;
        private int _pixelsPerCell = 8;
        private int _borderThickness = 1;
        private int Idx(int x, int y) => y * m_width + x;
        private NativeArray<Color32> _pixels;
        private Color32 m_backgroundColor;

        public Map(int width, int height)
        {
            m_width = width;
            m_height = height;
            m_backgroundColor = Color.clear;

            _cells = new NativeArray<Cell>(m_width * m_height, Allocator.Persistent);

            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                _cells[Idx(x, y)] = new Cell
                {
                    x = x,
                    y = y,
                    hasValue = 0,
                    isBorder = 0,
                    color = Color.clear
                };
            }

            int texW = m_width * _pixelsPerCell;
            int texH = m_height * _pixelsPerCell;

            Texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;
            _pixels = new NativeArray<Color32>(texW * texH, Allocator.Persistent);
        }

        public void SetUpMap(Color32 color32)
        {
            m_backgroundColor = color32;

            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                _cells[Idx(x, y)] = new Cell
                {
                    x = x,
                    y = y,
                    hasValue = 0,
                    isBorder = 0,
                    color = Color.clear
                };
            }

            _dirty = true;
        }

        public void Dispose()
        {
            if (Texture != null)
                Object.Destroy(Texture);
            if (_cells.IsCreated)
                _cells.Dispose();
            if (_pixels.IsCreated)
                _pixels.Dispose();
        }

        public void ApplyTexture(SpriteRenderer render)
        {
            if (Texture == null) return;
            UpdateTexture();
            if (render == null) return;
            var sprite = Sprite.Create(
                Texture,
                new Rect(0, 0, Texture.width, Texture.height),
                Vector2.one * 0.5f,
                100
            );
            render.sprite = sprite;
        }

        public void UpdateTexture()
        {
            if (!_dirty || !Texture) return;

            var job = new RenderPixelsJob
            {
                width = m_width,
                height = m_height,
                pixelsPerCell = _pixelsPerCell,
                texWidth = Texture.width,
                texHeight = Texture.height,
                borderThickness = _borderThickness,
                backgroundColor = m_backgroundColor,
                cells = _cells,
                pixels = _pixels
            };
            
            var handle = job.Schedule(_pixels.Length, 64);
            handle.Complete();
            Profiler.BeginSample("Texture.SetPixelData+Apply");
            Texture.SetPixelData(_pixels, 0);
            Texture.Apply();
            Profiler.EndSample();
            _dirty = false;
        }

        private void CheckGameOver()
        {
            if (_isGameOver) return;
            if (m_height <= 60) return;

            for (int x = 0; x < m_width; x++)
            {
                var cell = GetCell(x, 60);
                if (cell.hasValue == 1)
                {
                    _isGameOver = true;
                    break;
                }
            }
        }
        
        public bool Tick(int iterations = 1)
        {
            if (m_isMovePause) return false;
            if (!_cells.IsCreated) return false;

            var movedArr = new NativeArray<byte>(1, Allocator.TempJob);

            var job = new SandTickJob
            {
                width = m_width,
                height = m_height,
                iterations = iterations,
                cells = _cells,
                movedOut = movedArr
            };

            var handle = job.Schedule();
            handle.Complete();

            bool moved = movedArr[0] != 0;
            movedArr.Dispose();

            if (moved) _dirty = true;
            else
            {
                CheckGameOver();
                //
            }

            return moved;
        }

        private bool OutOfBound(int x, int y)
        {
            return x < 0 || y < 0 || x >= m_width || y >= m_height;
        }

        public bool InBound(int x, int y)
        {
            return x >= 0 && y >= 0 && x < m_width && y < m_height;
        }
        
        public void SetPixelCell(int x, int y, Color32 color32)
        {
            if (OutOfBound(x, y)) return;
            int idx = Idx(x, y);
            var c = _cells[idx];
            c.color = color32;
            c.hasValue = 1;
            c.isBorder = 0;
            c.x = x;
            c.y = y;
            _cells[idx] = c;
            _dirty = true;
        }

        public void HighlightCells(List<(int x, int y)> cells)
        {
            foreach (var (cx, cy) in cells)
            {
                int idx = Idx(cx, cy);
                var c = _cells[idx];
                c.color = new Color32(255, 255, 255, 255);
                _cells[idx] = c;
            }
            _dirty = true;
        }

        public Cell GetCell(int x, int y)
        {
            if (OutOfBound(x, y))
            {
                return new Cell
                {
                    x = x,
                    y = y,
                    hasValue = 1,
                    isBorder = 1,
                    color = Color.clear
                };
            }

            return _cells[Idx(x, y)];
        }
        
        public async UniTask ShrinkEffect(List<(int x, int y)> cellsList, Color32 finalColor)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var alpha = (byte)(255 * (1f - progress));

                foreach (var (cx, cy) in cellsList)
                {
                    int idx = Idx(cx, cy);
                    var c = _cells[idx];
                    c.color = new Color32(255, 255, 255, alpha);
                    _cells[idx] = c;
                }

                _dirty = true;
                UpdateTexture();
                await UniTask.Yield();
            }

            foreach (var (cx, cy) in cellsList)
            {
                int idx = Idx(cx, cy);
                var c = _cells[idx];
                c.color = finalColor;
                c.hasValue = 0;
                _cells[idx] = c;
            }

            _dirty = true;
            UpdateTexture();
        }

        public async UniTask ShrinkEffectWave(List<(int x, int y)> cellsList, Color32 finalColor)
        {
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

            for (int i = 0; i < 10; i++)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    
                    float t = Mathf.Clamp01(elapsed / duration);

                    float threshold = Mathf.Lerp(minX - 1, maxX + 1, t);

                    foreach (var (cx, cy) in cellsList)
                    {
                        int idx = Idx(cx, cy);
                        var c = _cells[idx];
                        if (cx <= threshold)
                        {
                            c.color = m_backgroundColor;
                        }
                        else
                        {
                            c.color = new Color32(255, 255, 255, 255);
                        }

                        _cells[idx] = c;
                    }
                    _dirty = true;
                    UpdateTexture();
                    await UniTask.Yield();
                }
                await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            }

            /*foreach (var (cx, cy) in cellsList)
            {
                int idx = Idx(cx, cy);
                var c = _cells[idx];
                c.color = finalColor;
                c.hasValue = 0;
                _cells[idx] = c;
            }
            */

            _dirty = true;
            UpdateTexture();
        }
    }

    public struct Cell
    {
        public int x, y;
        public byte hasValue;
        public byte isBorder;
        public Color32 color;
    }
}