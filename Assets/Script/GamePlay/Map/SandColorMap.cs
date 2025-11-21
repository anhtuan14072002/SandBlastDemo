using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sand
{
    public class SandColorMap
    {
        private readonly Map _map;
        private readonly int _width;
        private readonly int _height;
        public SandColorMap(Map map, int width, int height)
        {
            _map = map;
            _width = width;
            _height = height;
        }

        public async UniTask SameColorCompleteBands(Color32 color)
        {
            _map.IsMovePause = true;
            Dictionary<int, List<(int x, int y)>> completedCollectionMap = new();
            bool[,] visited = new bool[_width, _height];
            int count = 0;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (visited[x, y]) continue;
                    Cell cell = _map.GetCell(x, y);
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
                        if (currentX == _width - 1) touchesRight = true;

                        TryEnqueue(currentX + 1, currentY);
                        TryEnqueue(currentX - 1, currentY);
                        TryEnqueue(currentX, currentY + 1);
                        TryEnqueue(currentX, currentY - 1);

                        TryEnqueue(currentX + 1, currentY + 1);
                        TryEnqueue(currentX - 1, currentY + 1);
                        TryEnqueue(currentX - 1, currentY - 1);
                        TryEnqueue(currentX + 1, currentY - 1);

                        void TryEnqueue(int nx, int ny)
                        {
                            if (!_map.InBound(nx, ny)) return;
                            if (visited[nx, ny]) return;

                            Cell neighborCell = _map.GetCell(nx, ny);
                            if (neighborCell.hasValue == 1 && neighborCell.isBorder == 0 &&
                                SameColor(neighborCell.color, targetColor))
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
                        Global.Send(new SignalScoreOnGame() { Score = cellCountAfter });
                        count++;
                    }
                }
            }

            foreach (var (_, connectedComponent) in completedCollectionMap)
            {
                _map.HighlightCells(connectedComponent);
            }
            _map.Dirty = true;
            _map.UpdateTexture(); 

            foreach (var (_, connectedComponent) in completedCollectionMap)
            {
                await _map.ShrinkEffect(connectedComponent, color);
            }
            
            _map.IsMovePause = false;
        }

        public bool SameColor(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        private int CountCellsWithColor(Color32 targetColor)
        {
            int count = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _map.GetCell(x, y);
                    if (cell.hasValue == 1 && SameColor(cell.color, targetColor))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

    }
}