using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sand
{
    public static class FillColorRenderJobs
    {
        /// <summary>
        /// Tô các vị trí có màu gần targetColor lên mapArt._map,
        /// dùng NativeArray pixel đã cache (không GetPixels32 nữa).
        /// </summary>
        public static void FillRegionColorFull(
            RenderMap mapArt,
            NativeArray<Color32> spritePixels,
            int fullTextureWidth,
            int spriteStartX,
            int spriteStartY,
            int spriteWidth,
            int spriteHeight,
            Color32 targetColor,
            int colorTolerance,
            int batchSize
        )
        {
            if (mapArt == null || !spritePixels.IsCreated) return;

            int mapWidth = mapArt._wight;
            int mapHeight = mapArt._hight;
            int totalMapPixels = mapWidth * mapHeight;

            float scaleX = (float)mapWidth / spriteWidth;
            float scaleY = (float)mapHeight / spriteHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            float drawWidth = spriteWidth * scale;
            float drawHeight = spriteHeight * scale;
            float offsetX = (mapWidth - drawWidth) * 0.5f;
            float offsetY = (mapHeight - drawHeight) * 0.5f;

            var outColors = new NativeArray<Color32>(totalMapPixels, Allocator.TempJob);

            var fillJob = new FillRegionJob
            {
                MapWidth = mapWidth,
                MapHeight = mapHeight,

                SpritePixels = spritePixels,
                FullTextureWidth = fullTextureWidth,
                SpriteStartX = spriteStartX,
                SpriteStartY = spriteStartY,
                SpriteWidth = spriteWidth,
                SpriteHeight = spriteHeight,

                Scale = scale,
                OffsetX = offsetX,
                OffsetY = offsetY,

                TargetColor = targetColor,
                Tolerance = colorTolerance,

                OutColors = outColors
            };

            JobHandle handle = fillJob.Schedule(totalMapPixels, batchSize);
            handle.Complete();

            for (int i = 0; i < totalMapPixels; i++)
            {
                Color32 c = outColors[i];
                if (c.a == 0) continue;

                int y = i / mapWidth;
                int x = i % mapWidth;
                mapArt._map.SetPixelCell(x, y, c);
            }

            mapArt._map.UpdateTexture();
            outColors.Dispose();
        }

        // =============== JOB STRUCT ===============

        [BurstCompile]
        private struct FillRegionJob : IJobParallelFor
        {
            public int MapWidth;
            public int MapHeight;

            [ReadOnly] public NativeArray<Color32> SpritePixels;
            public int FullTextureWidth;
            public int SpriteStartX;
            public int SpriteStartY;
            public int SpriteWidth;
            public int SpriteHeight;

            public float Scale;
            public float OffsetX;
            public float OffsetY;

            public Color32 TargetColor;
            public int Tolerance;

            // OutColors[index].a == 0 => không tô
            public NativeArray<Color32> OutColors;

            public void Execute(int index)
            {
                int mapY = index / MapWidth;
                int mapX = index % MapWidth;

                float drawWidth = SpriteWidth * Scale;
                float drawHeight = SpriteHeight * Scale;

                OutColors[index] = new Color32(0, 0, 0, 0);

                if (mapX < OffsetX || mapX >= OffsetX + drawWidth ||
                    mapY < OffsetY || mapY >= OffsetY + drawHeight)
                    return;

                int localX = (int)((mapX - OffsetX) / Scale);
                int localY = (int)((mapY - OffsetY) / Scale);

                if (localX < 0 || localX >= SpriteWidth ||
                    localY < 0 || localY >= SpriteHeight)
                    return;

                int pixelIndex = (SpriteStartY + localY) * FullTextureWidth + (SpriteStartX + localX);
                if (pixelIndex < 0 || pixelIndex >= SpritePixels.Length)
                    return;

                Color32 pixelColor = SpritePixels[pixelIndex];
                if (pixelColor.a < 10)
                    return;

                if (!IsColorClose(pixelColor, TargetColor, Tolerance))
                    return;

                OutColors[index] = pixelColor;
            }

            private static bool IsColorClose(Color32 a, Color32 b, int tolerance)
            {
                int dr = math.abs(a.r - b.r);
                int dg = math.abs(a.g - b.g);
                int db = math.abs(a.b - b.b);
                return (dr + dg + db) <= tolerance;
            }
        }
    }
}
