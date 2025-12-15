using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Sand
{
    [BurstCompile]
    public struct RenderPixelsJob : IJobParallelFor
    {
        public int width;
        public int height;
        public int pixelsPerCell;
        public int texWidth;
        public int texHeight;
        public int borderThickness;
        public Color32 backgroundColor;

        [ReadOnly] public NativeArray<Cell> cells;
        public NativeArray<Color32> pixels;

        public void Execute(int index)
        {
            int texX = index % texWidth;
            int texY = index / texWidth;

            int cx = texX / pixelsPerCell;
            int cy = texY / pixelsPerCell;

            if (cx < 0 || cy < 0 || cx >= width || cy >= height)
            {
                pixels[index] = backgroundColor;
                return;
            }

            int cellIdx = cy * width + cx;
            Cell cell = cells[cellIdx];
            bool hasSand = (cell.hasValue == 1 && cell.isBorder == 0);

            if (!hasSand)
            {
                pixels[index] = backgroundColor;
                return;
            }

            int localX = texX - cx * pixelsPerCell;
            int localY = texY - cy * pixelsPerCell;

            bool isBorderPixel =
                localX < borderThickness ||
                localY < borderThickness ||
                localX >= pixelsPerCell - borderThickness ||
                localY >= pixelsPerCell - borderThickness;
            
            Color32 baseColor = hasSand ? cell.color : backgroundColor;

            float cellNoise = Hash01(cx, cy);
            float brightness;

            if (cellNoise < 0.9f)
                brightness = Mathf.Lerp(1.1f, 1.2f, cellNoise / 0.9f);
            else
                brightness = Mathf.Lerp(0.8f, 1.0f, (cellNoise - 0.9f) / 0.1f);

            Color32 cellColor = MulColor(baseColor, brightness);

            if (isBorderPixel)
                pixels[index] = MulColor(cellColor, 0.8f); // cellColor // baseColor
            else
                pixels[index] = cellColor; // cellColor // baseColor
        }

        private static Color32 MulColor(Color32 c, float mul)
        {
            byte r = (byte)Mathf.Clamp(c.r * mul, 0, 255);
            byte g = (byte)Mathf.Clamp(c.g * mul, 0, 255);
            byte b = (byte)Mathf.Clamp(c.b * mul, 0, 255);
            return new Color32(r, g, b, c.a);
        }

        private static float Hash01(int x, int y)
        {
            unchecked
            {
                int h = x * 73428767 ^ y * 91293199;
                h ^= (h >> 13);
                h *= 1274126177;
                h ^= (h >> 16);

                uint uh = (uint)h;
                return (uh & 0xFFFFFFu) / 16777215f;
            }
        }
    }
}