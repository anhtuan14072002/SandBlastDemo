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
        private BorderCell[,] m_borderCells;
        private Cell[,] m_cells;
        private Color32 m_backgroundColor;
        private bool m_isMovePause;
        public bool IsMovePause => m_isMovePause;
        public Texture2D Texture { get; set; }
        int m_width, m_height;

        //Tạo Sprite cho texture
        public Map(int width, int height, Color32 backgroundColor)
        {
            m_width = width;
            m_height = height;
            m_backgroundColor = backgroundColor;
            m_cells = new Cell[width, height];
            m_borderCells = new BorderCell[width, height];

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

                m_borderCells[x, y] = new BorderCell
                {
                    hasBorder = 0,
                    color = backgroundColor
                };
            }

            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;
            BuildBorderLayer();
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
            if (Texture != null)
                Object.Destroy(Texture);
        }

        public void ApplyTexture(SpriteRenderer render)
        {
            //Write color to texture
            if (Texture == null) return;
            var colors = new Color32[m_width * m_height];
            for (int y = 0; y < m_height; y++)
            for (int x = 0; x < m_width; x++)
                colors[y * m_width + x] = m_cells[x, y].color;

            Texture.SetPixels32(colors);
            Texture.Apply();

            if (render == null) return;
            var sprite = Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height),
                Vector2.one * 0.5f, 100);
            render.sprite = sprite;
        }

        public void UpdateTexture()
        {
            if (Texture == null) return;
            for (int y = 0; y < m_height; y++)
            for (int x = 0; x < m_width; x++)
            {
                Color32 baseColor;
                var cell = m_cells[x, y];
                if (cell.hasValue == 1 && cell.isBorder == 0)
                {
                    baseColor = cell.color;
                }
                else
                {
                    baseColor = m_backgroundColor;
                }
                var b = m_borderCells[x, y];
                if (b.hasBorder == 1)
                {
                    baseColor = BlendOverlay(baseColor, b.color, 0.7f); 
                }
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
                if (!movedThisIter) break;
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
                            else
                            {
                                // visited[nx, ny] = true;
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
            // m_isMove = true;
            // await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            // m_isMove = false;

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

        public void BuildBorderLayer()
        {
            var bg = m_backgroundColor;

            Color32 borderColor = new Color32(
                (byte)(bg.r * 0.8f),
                (byte)(bg.g * 0.8f),
                (byte)(bg.b * 0.8f)
                , 255);
            for (int x = 0; x < m_width; x++)
            {
                for (int y = 0; y < m_height; y++)
                {
                    m_borderCells[x, y].hasBorder = 1;
                    m_borderCells[x, y].color = borderColor;
                }
            }
        }

        public Color32 BlendOverlay(Color32 color1, Color32 color2, float alpha)
        {
            var alphaMix = 1f - alpha;
            var r = (byte)(color1.r * alphaMix + color2.r * alpha);
            var g = (byte)(color1.g * alphaMix + color2.g * alpha);
            var b = (byte)(color1.b * alphaMix + color2.b * alpha);
            return new Color32(r, g, b, 255);
        }
    }

    public struct Cell
    {
        public int x, y;
        public byte hasValue;
        public byte isBorder;
        public Color32 color;
    }

    public struct BorderCell
    {
        public byte hasBorder;
        public Color32 color;
    }
}