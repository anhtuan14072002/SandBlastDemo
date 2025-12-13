using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace HootyBird.ColoringBook.Tools
{
    [BurstCompile(
        CompileSynchronously = true,
        FloatMode = FloatMode.Fast,
        FloatPrecision = FloatPrecision.Low,
        OptimizeFor = OptimizeFor.Performance)]
    public struct CountPixelsJob : IJob
    {
        [ReadOnly]
        public NativeArray<byte> input;
        [ReadOnly]
        public NativeReference<int2> inputTextureSize;

        private NativeReference<uint> count;

        public void Execute()
        {
            for (int x = 0; x < inputTextureSize.Value.x; x++)
            {
                for (int y = 0; y < inputTextureSize.Value.y; y++)
                {
                    int index = inputTextureSize.Value.x * y + x;

                    if (input[index] > 0)
                    {
                        count.Value += 1;
                    }
                }
            }
        }

        public void Init()
        {
            count = new NativeReference<uint>(0, Allocator.TempJob);
        }

        public void Dispose()
        {
            input.Dispose();
            inputTextureSize.Dispose();
            count.Dispose();
        }

        public uint GetPixelsCount()
        {
            return count.Value;
        }
    }
}
