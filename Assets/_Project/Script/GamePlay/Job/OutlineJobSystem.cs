using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sand
{
    public static class OutlineJobSystem
    {
        public static void RenderOutline(
            RenderMap mapArt,
            Sprite outlineSprite,
            Color artBackgroundColor,
            Color sandLineColor,
            float lineLuminanceThreshold,
            float sandLineDensity,
            int jobBatchSize = 64)
        {
            if (mapArt == null || outlineSprite == null) return;

            int mapWidth = mapArt._wight;
            int mapHeight = mapArt._hight;

            Texture2D texture = outlineSprite.texture;
            Rect spriteRect = outlineSprite.textureRect;

            int spriteWidth = (int)spriteRect.width;
            int spriteHeight = (int)spriteRect.height;
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;

            float scaleX = (float)mapWidth / spriteWidth;
            float scaleY = (float)mapHeight / spriteHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            float drawWidth = spriteWidth * scale;
            float drawHeight = spriteHeight * scale;
            float offsetX = (mapWidth - drawWidth) * 0.5f;
            float offsetY = (mapHeight - drawHeight) * 0.5f;

            Color32[] spritePixels = texture.GetPixels32();
            int fullTextureWidth = texture.width;

            int totalMapPixels = mapWidth * mapHeight;

            var mapPixels = new NativeArray<Color32>(totalMapPixels, Allocator.TempJob);
            var spritePixelsNative = new NativeArray<Color32>(spritePixels, Allocator.TempJob);

            // Clear nền
            var clearJob = new ClearMapJob
            {
                MapPixels = mapPixels,
                BackgroundColor = (Color32)artBackgroundColor
            };
            JobHandle clearHandle = clearJob.Schedule(totalMapPixels, jobBatchSize);

            // Vẽ viền
            var outlineJob = new OutlineJob
            {
                MapPixels = mapPixels,
                MapWidth = mapWidth,
                MapHeight = mapHeight,

                SpritePixels = spritePixelsNative,
                FullTextureWidth = fullTextureWidth,
                SpriteStartX = startX,
                SpriteStartY = startY,
                SpriteWidth = spriteWidth,
                SpriteHeight = spriteHeight,

                Scale = scale,
                OffsetX = offsetX,
                OffsetY = offsetY,

                LuminanceThreshold = lineLuminanceThreshold,
                SandLineDensity = sandLineDensity,
                SandColor = (Color32)sandLineColor
            };

            JobHandle outlineHandle = outlineJob.Schedule(totalMapPixels, jobBatchSize, clearHandle);
            outlineHandle.Complete();

            // Apply về Map
            for (int i = 0; i < totalMapPixels; i++)
            {
                int y = i / mapWidth;
                int x = i % mapWidth;
                mapArt._map.SetPixelCell(x, y, mapPixels[i]);
            }

            mapArt._map.UpdateTexture();

            mapPixels.Dispose();
            spritePixelsNative.Dispose();
        }

        [BurstCompile]
        private struct ClearMapJob : IJobParallelFor
        {
            public NativeArray<Color32> MapPixels;
            public Color32 BackgroundColor;

            public void Execute(int index)
            {
                MapPixels[index] = BackgroundColor;
            }
        }

        [BurstCompile]
        private struct OutlineJob : IJobParallelFor
        {
            public NativeArray<Color32> MapPixels;
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

            public float LuminanceThreshold;
            public float SandLineDensity;
            public Color32 SandColor;

            public void Execute(int index)
            {
                int mapY = index / MapWidth;
                int mapX = index % MapWidth;

                float drawWidth = SpriteWidth * Scale;
                float drawHeight = SpriteHeight * Scale;

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

                float luminance =
                    (0.2126f * pixelColor.r +
                     0.7152f * pixelColor.g +
                     0.0722f * pixelColor.b) / 255f;

                if (luminance >= LuminanceThreshold)
                    return;

                float rnd = HashTo01(mapX, mapY);
                if (rnd > SandLineDensity)
                    return;

                MapPixels[index] = SandColor;
            }

            private static float HashTo01(int x, int y)
            {
                uint hash = math.hash(new int2(x, y));
                uint v = hash & 0x00FFFFFFu;
                return v / 16777215f;
            }
        }
    }
}
