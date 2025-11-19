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
        public Texture2D Texture { get; private set; }

        private NativeArray<Cell> _cells;
        public NativeArray<Cell> Cells => _cells;
        private bool m_isMovePause;
        public bool IsMovePause
        {
            get => m_isMovePause;
            set => m_isMovePause = value;
        }

        private bool _isGameOver;
        private bool _dirty = true;
        private int m_width, m_height;

        private int _pixelsPerCell = 10;
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

            // _pixels = new Color32[texW * texH];
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
                    Global.Send(new SignalUpdateHighScore { });
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
                Global.Send(new SignalUpdateHighScore());
            }

            return moved;
        }

        private bool OutOfBound(int x, int y)
        {
            return x < 0 || y < 0 || x >= m_width || y >= m_height;
        }

        private bool InBound(int x, int y)
        {
            return x >= 0 && y >= 0 && x < m_width && y < m_height;
        }

        private bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
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

        public bool MoveCell(int x, int y, int moveX, int moveY)
        {
            if (OutOfBound(x, y) || OutOfBound(moveX, moveY)) return false;

            int i1 = Idx(x, y);
            int i2 = Idx(moveX, moveY);

            var c1 = _cells[i1];
            var c2 = _cells[i2];

            if (c1.x == c2.x && c1.y == c2.y) return false;
            if (c2.hasValue == 1 || c2.isBorder == 1) return false;

            var tmp = c1;

            c1.x = c2.x;
            c1.y = c2.y;
            c2.x = tmp.x;
            c2.y = tmp.y;

            _cells[i1] = c2;
            _cells[i2] = c1;

            _dirty = true;
            return true;
        }

        public async UniTask SameColorCompleteBands(Color32 color)
        {
            m_isMovePause = true;
            Dictionary<int, List<(int x, int y)>> completedCollectionMap = new();
            bool[,] visited = new bool[m_width, m_height];
            int count = 0;

            for (int y = 0; y < m_height; y++)
            {
                for (int x = 0; x < m_width; x++)
                {
                    if (visited[x, y]) continue;
                    Cell cell = _cells[Idx(x, y)];
                    if (cell.hasValue != 1 || cell.isBorder == 1)
                    {
                        visited[x, y] = true;
                        continue;
                    }

                    Color32 targetColor = cell.color;
                    Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
                    List<(int x, int y)> connectedComponent = new List<(int x, int y)>();

                    bool touchesLeft = false;
                    bool touchesRight = false;

                    visited[x, y] = true;
                    queue.Enqueue((x, y));
                    while (queue.Count > 0)
                    {
                        var (currentX, currentY) = queue.Dequeue();

                        connectedComponent.Add((currentX, currentY));
                        if (currentX == 0) touchesLeft = true;
                        if (currentX == m_width - 1) touchesRight = true;

                        TryEnqueue(currentX + 1, currentY);
                        TryEnqueue(currentX - 1, currentY);
                        TryEnqueue(currentX, currentY + 1);
                        TryEnqueue(currentX, currentY - 1);

                        TryEnqueue(currentX + 1, currentY + 1);
                        TryEnqueue(currentX - 1, currentY + 1);
                        TryEnqueue(currentX - 1, currentY - 1);
                        TryEnqueue(currentX + 1, currentY - 1);

                        void TryEnqueue(int nx, int ny)
                        {
                            if (!InBound(nx, ny)) return;
                            if (visited[nx, ny]) return;

                            Cell n = _cells[Idx(nx, ny)];
                            if (n.hasValue == 1 && n.isBorder == 0 && SameColor(n.color, targetColor))
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }

                    if (touchesLeft && touchesRight)
                    {
                        completedCollectionMap.Add(count, connectedComponent);
                        var cellCountAfter = CountCellsWithColor(targetColor);
                        Global.Send(new SignalCountScore() { Amout = cellCountAfter });
                        count++;
                    }
                }
            }

            foreach (var (_, connectedComponent) in completedCollectionMap)
            {
                foreach (var (cx, cy) in connectedComponent)
                {
                    var idx = Idx(cx, cy);
                    var c = _cells[idx];
                    c.color = new Color32(255, 255, 255, 255);
                    _cells[idx] = c;
                }

                _dirty = true;
                UpdateTexture();
                await ShrinkEffect(connectedComponent, color);
            }

            m_isMovePause = false;
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

        public int CountCellsWithColor(Color32 targetColor)
        {
            int count = 0;
            for (int x = 0; x < m_width; x++)
            {
                for (int y = 0; y < m_height; y++)
                {
                    var cell = GetCell(x, y);
                    if (cell.hasValue == 1 && SameColor(cell.color, targetColor))
                    {
                        count++;
                    }
                }
            }

            return count;
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