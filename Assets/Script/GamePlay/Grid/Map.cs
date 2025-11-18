using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sand
{
    public class Map : IDisposable
    {
        public Texture2D Texture { get; private set; }

        private Cell[,] m_cells;
        private bool m_isMovePause;
        private bool _isGameOver;
        public bool IsMovePause => m_isMovePause;

        private int m_width, m_height;
        private Color32 m_backgroundColor;

        private int _pixelsPerCell = 10;
        private int _borderThickness = 1;

        private Color32[] _pixels;
        private bool _dirty = true;

        public Map(int width, int height)
        {
            m_width = width;
            m_height = height;
            m_backgroundColor = Color.clear;

            m_cells = new Cell[width, height];
            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y] = new Cell
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

            _pixels = new Color32[texW * texH];
        }

        public void SetUpMap(Color32 color32)
        {
            m_backgroundColor = color32;
            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y].color = Color.clear;
                m_cells[x, y].hasValue = 0;
                m_cells[x, y].isBorder = 0;
            }
            _dirty = true;
        }

        public void Dispose()
        {
            if (Texture != null)
                Object.Destroy(Texture);
        }

        public void ApplyTexture(SpriteRenderer render)
        {
            if (Texture == null) return;
            RedrawTextureFull();

            if (render == null) return;
            var sprite = Sprite.Create(
                Texture,
                new Rect(0, 0, Texture.width, Texture.height),
                Vector2.one * 0.5f,
                100 
            );
            render.sprite = sprite;
        }
        
        private void RedrawTextureFull()
        {
            if (Texture == null) return;

            int texW = Texture.width;
            int texH = Texture.height;

            for (int cy = 0; cy < m_height; cy++)
            {
                for (int cx = 0; cx < m_width; cx++)
                {
                    Cell cell = m_cells[cx, cy];
                    bool hasSand = (cell.hasValue == 1 && cell.isBorder == 0);

                    int startX = cx * _pixelsPerCell;
                    int startY = cy * _pixelsPerCell;

                    for (int py = 0; py < _pixelsPerCell; py++)
                    {
                        for (int px = 0; px < _pixelsPerCell; px++)
                        {
                            int texX = startX + px;
                            int texY = startY + py;
                            int idx = texY * texW + texX;

                            if (!hasSand)
                            {
                                _pixels[idx] = m_backgroundColor;
                                continue;
                            }

                            bool isBorderPixel =
                                px < _borderThickness ||
                                py < _borderThickness ||
                                px >= _pixelsPerCell - _borderThickness ||
                                py >= _pixelsPerCell - _borderThickness;

                            Color32 baseColor = cell.color;

                            if (isBorderPixel)
                            {
                                byte r = (byte)(baseColor.r * 0.7f + (baseColor.r * 0.3f) * 0.5f);
                                byte g = (byte)(baseColor.g * 0.7f + (baseColor.g * 0.3f) * 0.5f);
                                byte b = (byte)(baseColor.b * 0.7f + (baseColor.b * 0.3f) * 0.5f);
                                _pixels[idx] = new Color32(r, g, b, baseColor.a);
                            }
                            else
                            {
                                _pixels[idx] = baseColor;
                            }
                        }
                    }
                }
            }

            Texture.SetPixels32(_pixels);
            Texture.Apply();
        }

        public void UpdateTexture()
        {
            if (!_dirty) return;
            RedrawTextureFull();
            _dirty = false;
        }

        private void CheckGameOver()
        {
            for (int x = 0; x < m_width; x++)
            {
                if (GetCell(x, 60).hasValue == 1)
                {
                    _isGameOver = true;
                    Debug.Log("Game Over");
                    Global.Send(new SignalUpdateHighScore { });
                    break;
                }
            }
        }

        public bool Tick(int iterations = 1)
        {
            if (m_isMovePause) return false;
            bool moved = false;

            for (int it = 0; it < iterations; it++)
            {
                bool movedThisIter = false;

                for (int y = 1; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (m_cells[x, y].hasValue != 1) continue;

                        if (CanMove(x, y, x, y - 1))
                        {
                            Swap(x, y, x, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x - 1, y - 1))
                        {
                            Swap(x, y, x - 1, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x + 1, y - 1))
                        {
                            Swap(x, y, x + 1, y - 1);
                            movedThisIter = true;
                        }
                    }
                }

                if (movedThisIter) moved = true;
                if (!movedThisIter)
                {
                    CheckGameOver();
                    Global.Send(new SignalUpdateHighScore());
                    break;
                }
            }

            if (moved) _dirty = true;
            return moved;
        }

        private bool CanMove(int fromX, int fromY, int toX, int toY)
        {
            if (toX < 0 || toY < 0 || toX >= m_width || toY >= m_height) return false;
            return m_cells[toX, toY].hasValue == 0;
        }

        public bool MoveCell(int x, int y, int moveX, int moveY)
        {
            Cell cell = GetCell(x, y);
            Cell target = GetCell(moveX, moveY);
            bool isSwap = Swap(cell, target);
            if (isSwap) _dirty = true;
            return isSwap;
        }

        private void Swap(int x1, int y1, int x2, int y2)
        {
            (m_cells[x1, y1], m_cells[x2, y2]) = (m_cells[x2, y2], m_cells[x1, y1]);

            m_cells[x1, y1].x = x1;
            m_cells[x1, y1].y = y1;
            m_cells[x2, y2].x = x2;
            m_cells[x2, y2].y = y2;

            _dirty = true;
        }

        bool Swap(Cell c1, Cell c2)
        {
            if (c1.x == c2.x && c1.y == c2.y) return false;
            if (c2.hasValue == 1 || c2.isBorder == 1) return false;

            var tempC1 = c1;
            var tempC2 = c2;

            c1.x = tempC2.x;
            c1.y = tempC2.y;

            c2.x = tempC1.x;
            c2.y = tempC1.y;

            m_cells[tempC1.x, tempC1.y] = c2;
            m_cells[tempC2.x, tempC2.y] = c1;

            _dirty = true;
            return true;
        }

        bool OutOfBound(int x, int y)
        {
            return x < 0 || y < 0 || x >= m_width || y >= m_height;
        }

        bool InBound(int x, int y)
        {
            return x >= 0 && y >= 0 && x < m_width && y < m_height;
        }

        bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        public void SetPixelCell(int x, int y, Color32 color32)
        {
            if (x < 0 || y < 0 || x >= m_width || y >= m_height) return;
            m_cells[x, y].color = color32;
            m_cells[x, y].hasValue = 1;
            m_cells[x, y].x = x;
            m_cells[x, y].y = y;
            _dirty = true;
        }

        public Cell GetCell(int x, int y)
        {
            if (!OutOfBound(x, y))
            {
                return m_cells[x, y];
            }

            return new Cell()
            {
                x = x,
                y = y,
                hasValue = 1,
                isBorder = 1,
            };
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
                    Cell cell = m_cells[x, y];
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

                        TryEnqueue(currentX + 1, currentY); // phải
                        TryEnqueue(currentX - 1, currentY); // trái
                        TryEnqueue(currentX, currentY + 1); // trên
                        TryEnqueue(currentX, currentY - 1); // dưới

                        TryEnqueue(currentX + 1, currentY + 1); // chéo trên phải
                        TryEnqueue(currentX - 1, currentY + 1); // chéo trên trái
                        TryEnqueue(currentX - 1, currentY - 1); // chéo dưới trái
                        TryEnqueue(currentX + 1, currentY - 1); // chéo dưới phải

                        void TryEnqueue(int nx, int ny)
                        {
                            if (!InBound(nx, ny)) return;
                            if (visited[nx, ny]) return;

                            Cell n = m_cells[nx, ny];
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
                    m_cells[cx, cy].color = new Color32(255, 255, 255, 255);
                }

                _dirty = true;
                UpdateTexture();
                await ShrinkEffect(connectedComponent, color);
            }

            m_isMovePause = false;
        }

        async UniTask ShrinkEffect(List<(int x, int y)> cells, Color32 finalColor)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var alpha = (byte)(255 * (1f - progress));
                foreach (var (cx, cy) in cells)
                {
                    m_cells[cx, cy].color = new Color32(255, 255, 255, alpha);
                }

                _dirty = true;
                UpdateTexture();
                await UniTask.Yield();
            }

            foreach (var (cx, cy) in cells)
            {
                m_cells[cx, cy].color = finalColor;
                m_cells[cx, cy].hasValue = 0;
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
/*using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sand
{
    public class Map : IDisposable
    {
        public Texture2D Texture { get; set; }
        public Mesh Mesh { get; set; }
        private Cell[,] m_cells;
        private bool m_isMovePause;
        private bool _isGameOver;
        public bool IsMovePause => m_isMovePause;

        int
            m_width,
            m_height;

        public Map(int width, int height)
        {
            m_width = width;
            m_height = height;
            m_cells = new Cell[width, height];
            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y] = new Cell { x = x, y = y, hasValue = 0, isBorder = 0, color = Color.clear };
            }

            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;
        }

        public void SetUpMap(Color32 color32)
        {
            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y].color = color32;
                m_cells[x, y].hasValue = 0;
            }
        }

        public void Dispose()
        {
            if (Texture != null) Object.Destroy(Texture);
        }

        public void ApplyTexture(SpriteRenderer render)
        {
            if (Texture == null) return;
            var colors = new Color32[m_width * m_height];
            for (int y = 0; y < m_height; y++)
            for (int x = 0; x < m_width; x++)
                colors[y * m_width + x] = m_cells[x, y].color;
            Texture.SetPixels32(colors);
            Texture.Apply();
            if (render == null) return;
            var sprite = Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), Vector2.one * 0.5f, 100);
            render.sprite = sprite;
        }

        public void UpdateTexture()
        {
            if (Texture == null) return;
            for (int y = 0; y < m_height; y++)
            for (int x = 0; x < m_width; x++)
            {
                Texture.SetPixel(x, y, m_cells[x, y].color);
            }

            Texture.Apply();
        }

        public bool Tick(int iterations = 1)
        {
            if (m_isMovePause) return false;
            bool moved = false;
            for (int it = 0; it < iterations; it++)
            {
                bool movedThisIter = false;
                for (int y = 1; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (m_cells[x, y].hasValue != 1) continue;
                        if (CanMove(x, y, x, y - 1))
                        {
                            Swap(x, y, x, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x - 1, y - 1))
                        {
                            Swap(x, y, x - 1, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x + 1, y - 1))
                        {
                            Swap(x, y, x + 1, y - 1);
                            movedThisIter = true;
                        }
                    }
                }

                if (movedThisIter) moved = true;
                if (!movedThisIter)
                {
                    CheckGameOver();
                    Global.Send(new SignalUpdateHighScore());
                    break;
                }
            }

            return moved;
        }

        private bool CanMove(int fromX, int fromY, int toX, int toY)
        {
            if (toX < 0 || toY < 0 || toX >= m_width || toY >= m_height) return false;
            return m_cells[toX, toY].hasValue == 0;
        }

        public bool MoveCell(int x, int y, int moveX, int moveY)
        {
            Cell cell = GetCell(x, y);
            Cell target = GetCell(moveX, moveY);
            return Swap(cell, target);
        }

        private void Swap(int x1, int y1, int x2, int y2)
        {
            (m_cells[x1, y1], m_cells[x2, y2]) = (m_cells[x2, y2], m_cells[x1, y1]);
            m_cells[x1, y1].x = x1;
            m_cells[x1, y1].y = y1;
            m_cells[x2, y2].x = x2;
            m_cells[x2, y2].y = y2;
        }

        bool Swap(Cell c1, Cell c2)
        {
            if (c1.x == c2.x && c1.y == c2.y) return false;
            if (c2.hasValue == 1 || c2.isBorder == 1) return false;
            var tempC1 = c1;
            var tempC2 = c2;
            c1.x = tempC2.x;
            c1.y = tempC2.y;
            c2.x = tempC1.x;
            c2.y = tempC1.y;
            m_cells[tempC1.x, tempC1.y] = c2;
            m_cells[tempC2.x, tempC2.y] = c1;
            return true;
        }

        bool OutOfBound(int x, int y)
        {
            return x < 0 || y < 0 || x >= m_width || y >= m_height;
        }

        bool InBound(int x, int y)
        {
            return x >= 0 && y >= 0 && x < m_width && y < m_height;
        }

        bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        public void SetPixelCell(int x, int y, Color32 color32)
        {
            if (x < 0 || y < 0 || x >= m_width || y >= m_height) return;
            m_cells[x, y].color = color32;
            m_cells[x, y].hasValue = 1;
            m_cells[x, y].x = x;
            m_cells[x, y].y = y;
        }

        public Cell GetCell(int x, int y)
        {
            if (!OutOfBound(x, y))
            {
                return m_cells[x, y];
            }

            return new Cell() { x = x, y = y, hasValue = 1, isBorder = 1, };
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
                    Cell cell = m_cells[x, y];
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
                        TryEnqueue(currentX + 1,
                            currentY);
                        TryEnqueue(currentX - 1, currentY);
                        TryEnqueue(currentX, currentY + 1);
                        TryEnqueue(currentX, currentY - 1);
                        TryEnqueue(currentX + 1,
                            currentY +
                            1);
                        TryEnqueue(currentX - 1, currentY + 1);
                        TryEnqueue(currentX - 1, currentY - 1);
                        TryEnqueue(currentX + 1, currentY - 1);

                        void TryEnqueue(int nx, int ny)
                        {
                            if (!InBound(nx, ny)) return;
                            if (visited[nx, ny]) return;
                            Cell n = m_cells[nx, ny];
                            if (n.hasValue == 1 && n.isBorder == 0 && SameColor(n.color, targetColor))
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                            else
                            {
                                visited[nx, ny] = true;
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

            foreach (var (key, connectedComponent) in completedCollectionMap)
            {
                foreach (var (cx, cy) in connectedComponent)
                {
                    m_cells[cx, cy].color = new Color32(255, 255, 255, 255);
                }

                UpdateTexture();
                await ShrinkEffect(connectedComponent, color);
            }

            m_isMovePause = false;
        }

        async UniTask ShrinkEffect(List<(int x, int y)> cells, Color32 finalColor)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var alpha = (byte)(255 * (1f - progress));
                foreach (var (cx, cy) in cells)
                {
                    m_cells[cx, cy].color = new Color32(255, 255, 255, alpha);
                }

                UpdateTexture();
                await UniTask.Yield();
            }
            foreach (var (cx, cy) in cells)
            {
                m_cells[cx, cy].color = finalColor;
                m_cells[cx, cy].hasValue = 0;
            }

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
        private void CheckGameOver()
        {
            for (int x = 0; x < m_width; x++)
            {
                if (GetCell(x, 60).hasValue == 1)
                {
                    _isGameOver = true;
                    Debug.Log("Game Over");
                    Global.Send(new SignalUpdateHighScore { });
                    break;
                }
            }
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

/*using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sand
{
    public class Map : IDisposable
    {
        public Texture2D Texture { get; private set; }

        private Cell[,] m_cells;
        private bool m_isMovePause;
        public bool IsMovePause => m_isMovePause;

        private int m_width, m_height;
        private Color32 m_backgroundColor;

        private Color32[] _pixels;
        private bool _dirty = true;
        private bool _isGameOver = false;

        public Map(int width, int height, Color32 backgroundColor)
        {
            m_width = width;
            m_height = height;
            m_backgroundColor = backgroundColor;

            m_cells = new Cell[width, height];
            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y] = new Cell
                {
                    x = x,
                    y = y,
                    hasValue = 0,
                    isBorder = 0,
                    color = Color.clear
                };
            }

            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;

            _pixels = new Color32[m_width * m_height];
        }

        public void SetUpMap(Color32 color32)
        {
            m_backgroundColor = color32;

            for (int x = 0; x < m_width; x++)
            for (int y = 0; y < m_height; y++)
            {
                m_cells[x, y].color = Color.clear;
                m_cells[x, y].hasValue = 0;
                m_cells[x, y].isBorder = 0;
            }

            _dirty = true;
        }

        public void Dispose()
        {
            if (Texture != null)
                Object.Destroy(Texture);
        }

        public void ApplyTexture(SpriteRenderer render)
        {
            if (Texture == null) return;
            RedrawTextureFull();

            if (render == null) return;
            var sprite = Sprite.Create(
                Texture,
                new Rect(0, 0, Texture.width, Texture.height),
                Vector2.one * 0.5f,
                100
            );
            render.sprite = sprite;
        }

        public void RedrawTextureFull()
        {
            if (Texture == null) return;

            for (int y = 0; y < m_height; y++)
            {
                int rowOffset = y * m_width;
                for (int x = 0; x < m_width; x++)
                {
                    var cell = m_cells[x, y];

                    Color32 color;
                    if (cell.hasValue == 1 && cell.isBorder == 0)
                    {
                        color = cell.color;
                        color = JitterSandColor(color, x, y);
                    }
                    else
                    {
                        color = m_backgroundColor;
                    }

                    _pixels[rowOffset + x] = color;
                }
            }

            Texture.SetPixels32(_pixels);
            Texture.Apply();
        }

        public void UpdateTexture()
        {
            if (!_dirty) return;
            RedrawTextureFull();
            _dirty = false;
        }

        private void CheckGameOver()
        {
            for (int x = 0; x < m_width; x++)
            {
                if (GetCell(x, 60).hasValue == 1)
                {
                    _isGameOver = true;
                    Debug.Log("Game Over");
                    Global.Send(new SignalUpdateHighScore { });
                    break;
                }
            }
        }

        public bool Tick(int iterations = 1)
        {
            if (m_isMovePause) return false;
            bool moved = false;

            for (int it = 0; it < iterations; it++)
            {
                bool movedThisIter = false;

                for (int y = 1; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (m_cells[x, y].hasValue != 1) continue;

                        if (CanMove(x, y, x, y - 1))
                        {
                            Swap(x, y, x, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x - 1, y - 1))
                        {
                            Swap(x, y, x - 1, y - 1);
                            movedThisIter = true;
                        }
                        else if (CanMove(x, y, x + 1, y - 1))
                        {
                            Swap(x, y, x + 1, y - 1);
                            movedThisIter = true;
                        }
                    }
                }


                if (movedThisIter) moved = true;
                if (!movedThisIter)
                {
                    CheckGameOver();
                    Global.Send(new SignalUpdateHighScore());
                    break;
                }
            }

            if (moved) _dirty = true;
            return moved;
        }

        private bool CanMove(int fromX, int fromY, int toX, int toY)
        {
            if (toX < 0 || toY < 0 || toX >= m_width || toY >= m_height) return false;
            return m_cells[toX, toY].hasValue == 0;
        }

        public bool MoveCell(int x, int y, int moveX, int moveY)
        {
            Cell cell = GetCell(x, y);
            Cell target = GetCell(moveX, moveY);
            bool isSwap = Swap(cell, target);
            if (isSwap) _dirty = true;
            return isSwap;
        }

        private void Swap(int x1, int y1, int x2, int y2)
        {
            (m_cells[x1, y1], m_cells[x2, y2]) = (m_cells[x2, y2], m_cells[x1, y1]);

            m_cells[x1, y1].x = x1;
            m_cells[x1, y1].y = y1;
            m_cells[x2, y2].x = x2;
            m_cells[x2, y2].y = y2;

            _dirty = true;
        }

        bool Swap(Cell c1, Cell c2)
        {
            if (c1.x == c2.x && c1.y == c2.y) return false;
            if (c2.hasValue == 1 || c2.isBorder == 1) return false;

            var tempC1 = c1;
            var tempC2 = c2;

            c1.x = tempC2.x;
            c1.y = tempC2.y;

            c2.x = tempC1.x;
            c2.y = tempC1.y;

            m_cells[tempC1.x, tempC1.y] = c2;
            m_cells[tempC2.x, tempC2.y] = c1;

            _dirty = true;
            return true;
        }

        bool OutOfBound(int x, int y)
        {
            return x < 0 || y < 0 || x >= m_width || y >= m_height;
        }

        bool InBound(int x, int y)
        {
            return x >= 0 && y >= 0 && x < m_width && y < m_height;
        }

        bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        public void SetPixelCell(int x, int y, Color32 color32)
        {
            if (x < 0 || y < 0 || x >= m_width || y >= m_height) return;
            m_cells[x, y].color = color32;
            m_cells[x, y].hasValue = 1;
            m_cells[x, y].x = x;
            m_cells[x, y].y = y;
            _dirty = true;
        }

        public Cell GetCell(int x, int y)
        {
            if (!OutOfBound(x, y))
            {
                return m_cells[x, y];
            }

            return new Cell()
            {
                x = x,
                y = y,
                hasValue = 1,
                isBorder = 1,
            };
        }

        Color32 JitterSandColor(Color32 baseColor, int x, int y)
        {
            int h = x * 73856093 ^ y * 19349663;
            h &= 0xFF;

            float t = h / 255f;
            float factor = 0.85f + t * 0.30f;
            byte r = (byte)Mathf.Clamp(baseColor.r * factor, 0f, 255f);
            byte g = (byte)Mathf.Clamp(baseColor.g * factor, 0f, 255f);
            byte b = (byte)Mathf.Clamp(baseColor.b * factor, 0f, 255f);
            return new Color32(r, g, b, baseColor.a);
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
                    Cell cell = m_cells[x, y];
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

                        TryEnqueue(currentX + 1, currentY); // phải
                        TryEnqueue(currentX - 1, currentY); // trái
                        TryEnqueue(currentX, currentY + 1); // trên
                        TryEnqueue(currentX, currentY - 1); // dưới

                        TryEnqueue(currentX + 1, currentY + 1); // chéo trên phải
                        TryEnqueue(currentX - 1, currentY + 1); // chéo trên trái
                        TryEnqueue(currentX - 1, currentY - 1); // chéo dưới trái
                        TryEnqueue(currentX + 1, currentY - 1); // chéo dưới phải

                        void TryEnqueue(int nx, int ny)
                        {
                            if (!InBound(nx, ny)) return;
                            if (visited[nx, ny]) return;

                            Cell n = m_cells[nx, ny];
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
                    m_cells[cx, cy].color = new Color32(255, 255, 255, 255);
                }

                _dirty = true;
                UpdateTexture();
                await ShrinkEffect(connectedComponent, color);
            }

            m_isMovePause = false;
        }

        async UniTask ShrinkEffect(List<(int x, int y)> cells, Color32 finalColor)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var alpha = (byte)(255 * (1f - progress));
                foreach (var (cx, cy) in cells)
                {
                    m_cells[cx, cy].color = new Color32(255, 255, 255, alpha);
                }

                _dirty = true;
                UpdateTexture();
                await UniTask.Yield();
            }

            foreach (var (cx, cy) in cells)
            {
                m_cells[cx, cy].color = finalColor;
                m_cells[cx, cy].hasValue = 0;
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
}#1#*/