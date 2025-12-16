using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class SandColorMap
    {
        private readonly Map _map;
        private readonly int _width;
        private readonly int _height;
        private int _currentComboCount = 0;
        private int _turnsWithoutCombo = 0;
        private const int MAX_TURNS_WITHOUT_COMBO = 3;
        private const int CHANNEL_TOLERANCE = 12;

        EffectBlock _effectBlock;
        SoundManager _soundManager;
        CountDrawData _countDrawData; 
        
        public SandColorMap(Map map, int width, int height, EffectBlock effectBlock, SoundManager soundManager, CountDrawData countDrawData)
        {
            _map = map;
            _width = width;
            _height = height;
            _effectBlock = effectBlock;
            _soundManager = soundManager;
            _countDrawData = countDrawData;
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
                        Global.Send(new SignalSyncBackgroundGamePlay(){Color = targetColor});
                        _currentComboCount++;
                        _turnsWithoutCombo = 0;
                        PlayComboSound();
                        Global.Send(new SignalIncreaseNumberCombo(){Count = _currentComboCount});
                        Global.Send(new SignalScoreOnGame() { Score = cellCountAfter });
                        Global.Send(new SignalOpenEffectTextScore()
                        {
                            Score = cellCountAfter,
                            Combo = _currentComboCount,
                            Position = Vector3.zero
                        });
                        _countDrawData.IncreaseCountDrawInGame(1);
                        Global.Send(new SignalIncreaseCountDrawIngame(){ Count = 1 });
                        count++;
                    }
                }
            }

            if (completedCollectionMap.Count == 0)
            {
                _turnsWithoutCombo++;
                if (_turnsWithoutCombo >= MAX_TURNS_WITHOUT_COMBO) ResetCombo();
            }

            foreach (var (_, connectedComponent) in completedCollectionMap)
            {
                _map.HighlightCells(connectedComponent);
            }
            
            _map.Dirty = true;
            _map.UpdateTexture(); 

            foreach (var (_, connectedComponent) in completedCollectionMap)
            {
                await _effectBlock.ShrinkEffect(_map, connectedComponent, color);
            }
            
            _map.IsMovePause = false;
        }
        
        public bool SameColor(Color32 a, Color32 b)
        {
            return Mathf.Abs(a.r - b.r) <= CHANNEL_TOLERANCE &&
                   Mathf.Abs(a.g - b.g) <= CHANNEL_TOLERANCE &&
                   Mathf.Abs(a.b - b.b) <= CHANNEL_TOLERANCE;
        }

        public int CountCellsWithColor(Color32 targetColor)
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

        
        private void PlayComboSound()
        {
            int comboLevel = Mathf.Clamp(_currentComboCount, 1, 9);

            switch (comboLevel)
            {
                case 1:
                    _soundManager.OnPlaySound(SoundType.Combo1);
                    break;
                case 2:
                    _soundManager.OnPlaySound(SoundType.Combo2);
                    break;
                case 3:
                    _soundManager.OnPlaySound(SoundType.Combo3);
                    _soundManager.OnPlaySound(SoundType.Good);
                    break;
                case 4:
                    _soundManager.OnPlaySound(SoundType.Combo4);
                    _soundManager.OnPlaySound(SoundType.Superb);
                    break;
                case 5:
                    _soundManager.OnPlaySound(SoundType.Combo6);
                    _soundManager.OnPlaySound(SoundType.Great);
                    break;
                case 6:
                    _soundManager.OnPlaySound(SoundType.Combo6);
                    _soundManager.OnPlaySound(SoundType.WellDone);
                    break;
                case 7:
                    _soundManager.OnPlaySound(SoundType.Combo7);
                    _soundManager.OnPlaySound(SoundType.WellDone);
                    break;
                case 8:
                    _soundManager.OnPlaySound(SoundType.Combo8);
                    _soundManager.OnPlaySound(SoundType.Wonderful);
                    break;
                case 9:
                    _soundManager.OnPlaySound(SoundType.Combo9);
                    _soundManager.OnPlaySound(SoundType.Wonderful);
                    break;
                default:
                    _soundManager.OnPlaySound(SoundType.Combo1);
                    break;
            }
        }

        public void ResetCombo()
        {
            _currentComboCount = 0;
            _turnsWithoutCombo = 0;
        }

        public int GetCurrentComboCount()
        {
            return _currentComboCount;
        }

        public int GetTurnsWithoutCombo()
        {
            return _turnsWithoutCombo;
        }

    }
}