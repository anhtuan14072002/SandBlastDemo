using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Sand
{
    [BurstCompile]
    public struct SandTickJob : IJob
    {
        public int width;
        public int height;
        public int iterations;

        public NativeArray<Cell> cells;
        public NativeArray<byte> movedOut;

        public void Execute()
        {
            bool moved = false;

            for (int it = 0; it < iterations; it++)
            {
                bool movedThisIter = false;

                for (int y = 1; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = Idx(x, y);
                        Cell c = cells[idx];
                        if (c.hasValue != 1 || c.isBorder == 1) continue;

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
                else break;
            }

            movedOut[0] = (byte)(moved ? 1 : 0);
        }

        private int Idx(int x, int y) => y * width + x;

        private bool In(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        private bool CanMove(int fromX, int fromY, int toX, int toY)
        {
            if (!In(toX, toY)) return false;
            var to = cells[Idx(toX, toY)];
            return to.hasValue == 0 && to.isBorder == 0;
        }

        private void Swap(int x1, int y1, int x2, int y2)
        {
            int i1 = Idx(x1, y1);
            int i2 = Idx(x2, y2);
            if (i1 == i2) return;

            Cell c1 = cells[i1];
            Cell c2 = cells[i2];

            var tmp = c1;

            c1.x = c2.x;
            c1.y = c2.y;
            c2.x = tmp.x;
            c2.y = tmp.y;

            cells[i1] = c2;
            cells[i2] = c1;
        }
    }
}