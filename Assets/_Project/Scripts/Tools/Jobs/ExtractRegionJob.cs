using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace HootyBird.ColoringBook.Tools
{
    [BurstCompile(
        CompileSynchronously = true, 
        FloatMode = FloatMode.Fast, 
        FloatPrecision = FloatPrecision.Low, 
        OptimizeFor = OptimizeFor.Performance)]
    public struct ExtractRegionJob : IJob
    {
        [ReadOnly]
        public NativeArray<Color> input;
        [ReadOnly]
        public NativeReference<int2> targetLocation;
        [ReadOnly]
        public NativeReference<int2> inputTextureSize;
        [ReadOnly]
        public NativeReference<int> errorSize;

        private NativeQueue<int2> locationsQueue;
        private NativeHashSet<int2> regionLocations;

        private NativeReference<int2> textureMin;
        private NativeReference<int2> textureMax;

        public void Execute()
        {
            // Get color at target location.
            Color32 targetColor = input[targetLocation.Value.y * inputTextureSize.Value.x + targetLocation.Value.x];

            textureMin.Value = targetLocation.Value;
            textureMax.Value = targetLocation.Value;

            locationsQueue.Enqueue(targetLocation.Value);
            while (locationsQueue.Count > 0)
            {
                int2 loc = locationsQueue.Dequeue();
                int pIndex = loc.y * inputTextureSize.Value.x + loc.x;
                if (regionLocations.Contains(loc) ||
                    math.min(loc.x, loc.y) < 0 ||
                    loc.x >= inputTextureSize.Value.x ||
                    loc.y >= inputTextureSize.Value.y ||
                    !CompareColors(targetColor, input[pIndex], errorSize.Value, errorSize.Value, errorSize.Value))
                {
                    continue;
                }
                else
                {
                    regionLocations.Add(loc);

                    locationsQueue.Enqueue(new int2(loc.x + 1, loc.y));
                    locationsQueue.Enqueue(new int2(loc.x - 1, loc.y));
                    locationsQueue.Enqueue(new int2(loc.x, loc.y + 1));
                    locationsQueue.Enqueue(new int2(loc.x, loc.y - 1));

                    // Check location min/max.
                    if (loc.x < textureMin.Value.x)
                    {
                        textureMin.Value = new int2(loc.x, textureMin.Value.y);
                    }
                    else if (loc.x > textureMax.Value.x) 
                    {
                        textureMax.Value = new int2(loc.x, textureMax.Value.y);
                    }

                    if (loc.y < textureMin.Value.y)
                    {
                        textureMin.Value = new int2(textureMin.Value.x, loc.y);
                    }
                    else if (loc.y > textureMax.Value.y)
                    {
                        textureMax.Value = new int2(textureMax.Value.x, loc.y);
                    }
                }
            }
        }

        public void Init()
        {
            regionLocations = new NativeHashSet<int2>(0, Allocator.TempJob);
            locationsQueue = new NativeQueue<int2>(Allocator.TempJob);

            textureMin = new NativeReference<int2>(0, Allocator.TempJob);
            textureMax = new NativeReference<int2>(0, Allocator.TempJob);
        }

        public Result GetJobResult()
        {
            Result result = new Result()
            {
                textureLocation = textureMin.Value,
                mainColor = input[targetLocation.Value.y * inputTextureSize.Value.x + targetLocation.Value.x],
#if UNITY_6000_0_OR_NEWER
                pixelCount = regionLocations.Count,
#else
                pixelCount = regionLocations.Count(),
#endif
            };

            int2 textureSize = textureMax.Value - textureMin.Value + new int2(1, 1);
            Texture2D texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBAFloat, false);

            UnityEngine.Debug.Log($"Region pixels count: {result.pixelCount}. Texture size: {textureSize}");

            // Extract pixels.
            NativeArray<Color> resultImageData = new NativeArray<Color>(textureSize.x * textureSize.y, Allocator.Temp);
            for (int x = 0; x < textureSize.x; x++)
            {
                for (int y = 0; y < textureSize.y; y++)
                {
                    int2 locationAtInputTexture = result.textureLocation + new int2(x, y);
                    if (regionLocations.Contains(locationAtInputTexture))
                    {
                        resultImageData[y * textureSize.x + x] = new Color(1f, 1f, 1f, 1f);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            texture.SetPixelData(resultImageData, 0);
            texture.Apply();
            result.texture = texture;

            resultImageData.Dispose();

            return result;
        }

        public void Dispose()
        {
            input.Dispose();
            targetLocation.Dispose();
            inputTextureSize.Dispose();
            errorSize.Dispose();

            regionLocations.Dispose();
            locationsQueue.Dispose();

            textureMin.Dispose();
            textureMax.Dispose();
        }

        private bool CompareColors(Color32 c1, Color32 c2, int rOffset, int gOffset, int bOffset)
        {
            return
                CompareValue(c1.r, c2.r, -rOffset, rOffset) &&
                CompareValue(c1.g, c2.g, -gOffset, gOffset) &&
                CompareValue(c1.b, c2.b, -bOffset, bOffset);
        }

        private bool CompareValue(
            int value,
            int target,
            int offsetLeft,
            int offsetRight)
        {
            return value + offsetLeft <= target && target <= value + offsetRight;
        }

        public struct Result
        {
            public Texture2D texture;
            public int2 textureLocation;
            public Color mainColor;
            public int pixelCount;
        }
    }
}
