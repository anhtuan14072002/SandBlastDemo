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
    public struct GrowRegionJob : IJob
    {
        [ReadOnly]
        public NativeArray<Color> input;
        [ReadOnly]
        public NativeReference<int2> textureSize;
        [ReadOnly]
        public NativeReference<int2> extentMin;
        [ReadOnly]
        public NativeReference<int2> extentMax;

        private NativeReference<int2> outputTextureSize;
        private NativeArray<Color> output;

        public void Execute()
        {
            int inputIndex;
            int targetX;
            int targetY;
            int targetIndex;
            for (int x = 0; x < textureSize.Value.x; x++)
            {
                for (int y = 0; y < textureSize.Value.y; y++)
                {
                    inputIndex = y * textureSize.Value.x + x;
                    targetX = x + extentMin.Value.x;
                    targetY = (y + extentMin.Value.y);
                    targetIndex = targetY * outputTextureSize.Value.x + targetX;

                    // Fill pixels around targetIndex;
                    if (input[inputIndex].r > .1f && 
                        input[inputIndex].g > .1f && 
                        input[inputIndex].b > .1f && 
                        input[inputIndex].a > .1f)
                    {
                        output[targetIndex] = input[inputIndex];
                        // Check if can add to the right.
                        if (targetX < outputTextureSize.Value.x - 1)
                        {
                            output[targetIndex + 1] = new Color(1f, 1f, 1f, 1f);
                        }
                        // Check left.
                        if (targetX > 0)
                        {
                            output[targetIndex - 1] = new Color(1f, 1f, 1f, 1f);
                        }
                        // Up.
                        if (targetY < outputTextureSize.Value.y - 1)
                        {
                            output[targetIndex + outputTextureSize.Value.x] = new Color(1f, 1f, 1f, 1f);
                        }
                        // Down
                        if (targetY > 0)
                        {
                            output[targetIndex - outputTextureSize.Value.x] = new Color32(255, 255, 255, 255);
                        }
                    }
                }
            }
        }

        public void Init()
        {
            outputTextureSize = new NativeReference<int2>(
                new int2(
                    textureSize.Value.x + extentMin.Value.x + extentMax.Value.x,
                    textureSize.Value.y + extentMin.Value.y + extentMax.Value.y
                ), 
                Allocator.TempJob);

            output = new NativeArray<Color>(outputTextureSize.Value.x * outputTextureSize.Value.y, Allocator.TempJob);
        }

        public Result GetResults()
        { 
            Result result = new Result();

            Texture2D texture = new Texture2D(outputTextureSize.Value.x, outputTextureSize.Value.y, TextureFormat.RGBAFloat, false);
            texture.SetPixelData(output, 0);
            texture.Apply();
            result.texture = texture;

            result.extentMin = extentMin.Value;
            result.extentMax = extentMax.Value;

            return result;
        }

        public void Dispose()
        {
            input.Dispose();
            textureSize.Dispose();
            outputTextureSize.Dispose();
            output.Dispose();

            extentMin.Dispose();
            extentMax.Dispose();
        }

        public struct Result
        {
            public Texture2D texture;
            public int2 extentMin;
            public int2 extentMax;
        }
    }
}
