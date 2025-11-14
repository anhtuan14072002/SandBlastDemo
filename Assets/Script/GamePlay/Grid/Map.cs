using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sand
{
    public class Map : IDisposable
    {
        public Texture2D Texture { get; set; }
        private Cell[,] m_cells;

        int m_width, m_height;

        //Tạo Sprite cho texture
        public Map(int width, int height)
        {
            m_width = width;
            m_height = height;
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
                Texture.SetPixel(x, y, m_cells[x, y].color);
            }

            Texture.Apply();
        }
    
        public void Tick()
        {
            for (int iteration = 0; iteration < 2; iteration++)
            {
                for (int y = 1; y < m_height; y++)
                {
                    for (int x = 0; x < m_width; x++)
                    {
                        if (m_cells[x, y].hasValue != 1) continue;
                        if (CanMove(x, y, x, y - 1))
                            Swap(x, y, x, y - 1);
                        else if (CanMove(x, y, x - 1, y - 1))
                            Swap(x, y, x - 1, y - 1);
                        else if (CanMove(x, y, x + 1, y - 1))
                            Swap(x, y, x + 1, y - 1);
                    }
                }
            }
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
    }

    public struct Cell
    {
        public int x, y;
        public byte hasValue;
        public byte isBorder;
        public Color32 color;
    }
}