// Copyright 2022 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using System.Collections;
using System.Collections.Generic;
using GamesTan.UI;
using UnityEngine;

namespace GamesTanEncrypt {
    public class SuperScrollRectImpl {
        private bool _isVertical => _scrollRect.vertical;
        private RectTransform _cellPrefab => _scrollRect.CellPrefab;
        private RectTransform _viewport => _scrollRect.viewport;
        private RectTransform _content => _scrollRect.content;
        private ISuperScrollRectDataProvider _dataProvider => _scrollRect.DataProvider;
        private bool _isGrid => _scrollRect.IsGrid;
        public List<ScrollRectCell> AllCells => _allCells;

        private int _numPerRowOrCol;
        private float _cellWidth;
        private float _cellHeight;
        private BaseSuperScrollRect _scrollRect;


        private int _minIndex = 0;
        private int _maxIndex => Mathf.Min(_minIndex + _totalVisibleCellCount, _dataProvider.GetCellCount()) - 1;
        private int _totalVisibleCellCount => _totalRowOrColCount * _numPerRowOrCol;

        private int _totalRowOrColCount = 0;

        // delay load
        private Queue<Task> _allTasks = new Queue<Task>();

        // object pool
        private Stack<GameObject> _itemPool = new Stack<GameObject>();
        private List<ScrollRectCell> _allCells = new List<ScrollRectCell>();
        private HashSet<int> _allCellIds = new HashSet<int>();
        private Dictionary<int, ScrollRectCell> _id2Cells = new Dictionary<int, ScrollRectCell>();

        public int MaxCellCreateCountPerFrame = 4;

        private Coroutine _updateCor;

        public struct Task {
            public int Index;

            public override string ToString() {
                return Index.ToString();
            }
        }


        public void DoAwake(BaseSuperScrollRect scrollRect) {
            this._scrollRect = scrollRect;
            _numPerRowOrCol = _isGrid ? scrollRect.Segment : 1;

            _cellWidth = _cellPrefab.sizeDelta.x + scrollRect.Spacing.x;
            _cellHeight = _cellPrefab.sizeDelta.y + scrollRect.Spacing.y;
            _totalRowOrColCount =
                (_isVertical
                    ? Mathf.CeilToInt(_viewport.rect.height / _cellHeight)
                    : Mathf.CeilToInt(_viewport.rect.width / _cellWidth)) + 1;
            DoReset();
        }

        public void ClearCache() {
            DestroyCells(true);
            CheckVisibility();
        }

        public void DoStart() {
            SetTopLeftAnchor(_content);
            UpdateBoundInfo();
            var rawSize = _content.rect.size;
            int numOfRows = Mathf.CeilToInt(_dataProvider.GetCellCount() / _numPerRowOrCol);
            var sizeDelta = (_isVertical
                ? new Vector2(rawSize.x, numOfRows * _cellHeight)
                : new Vector2(numOfRows * _cellWidth, rawSize.y));
            sizeDelta += _scrollRect.Padding;
            _content.sizeDelta = sizeDelta;
            DoReset();
            CheckVisibility();
            _updateCor = _scrollRect.StartCoroutine(UpdateTasks());
        }

        public void JumpTo(int cellIndex) {
            cellIndex = Mathf.Clamp(cellIndex, 0, _dataProvider.GetCellCount() - 1);    
            var offset = _content.anchoredPosition;
            var rowOrCol = cellIndex / _numPerRowOrCol;
            if (_isVertical) {
                offset = new Vector2(offset.x, rowOrCol * _cellHeight);
            }
            else {
                offset = new Vector2(rowOrCol * _cellWidth, offset.y);
            }

            _content.anchoredPosition = offset;
            OnValueChanged();
        }

        private void UpdateBoundInfo() {
            _cellWidth = _cellPrefab.sizeDelta.x + _scrollRect.Spacing.x;
            _cellHeight = _cellPrefab.sizeDelta.y + _scrollRect.Spacing.y;
            _totalRowOrColCount = (_isVertical
                ? Mathf.CeilToInt(_viewport.rect.height / _cellHeight)
                : Mathf.CeilToInt(_viewport.rect.width / _cellWidth)) + 1;
            var curContentAnchor = _content.anchoredPosition;
            var count = (_isVertical
                ? Mathf.CeilToInt(curContentAnchor.y / _cellHeight)
                : Mathf.CeilToInt(-curContentAnchor.x / _cellWidth));
            _minIndex = Mathf.Max(0, count - 1) * _numPerRowOrCol;
            //Debug.Log("curContentAnchor " + curContentAnchor + " viewport " + Viewport.rect + " MinIndex " + _minIndex + "MaxIndex " + _maxIndex);
        }


        private bool IsInRange(int index) {
            return index >= _minIndex && index <= _maxIndex;
        }

        private Vector2 GetAnchorPos(int index) {
            var row = index / _numPerRowOrCol;
            var col = index % _numPerRowOrCol;
            if (!_isVertical) {
                row = index % _numPerRowOrCol;
                col = index / _numPerRowOrCol;
            }

            return new Vector2(col * _cellWidth, -row * _cellHeight) +
                   new Vector2(_scrollRect.Padding.x, -_scrollRect.Padding.y);
        }

        private IEnumerator UpdateTasks() {
            while (true) {
                for (int i = 0; i < MaxCellCreateCountPerFrame;) {
                    if (_allTasks.Count == 0) {
                        break;
                    }

                    var task = _allTasks.Dequeue();
                    if (IsInRange(task.Index) && !_allCellIds.Contains(task.Index)) {
                        var item = GetOrCreateCell();
                        item.anchoredPosition = GetAnchorPos(task.Index);
                        _allCells.Add(new ScrollRectCell() {Item = item, Index = task.Index});
                        _allCellIds.Add(task.Index);
                        _dataProvider.SetCell(item.gameObject, task.Index);
                        i++;
                    }
                }

                yield return null;
            }
        }

        private void AddTask(int index) {
            _allTasks.Enqueue(new Task() {Index = index});
        }

        private void CheckVisibility() {
            _id2Cells.Clear();
            foreach (var cell in _allCells) {
                _id2Cells[cell.Index] = cell;
            }

            _allCells.Clear();
            // load visible cells 
            for (int i = _minIndex; i <= _maxIndex; i++) {
                if (!_id2Cells.ContainsKey(i)) {
                    AddTask(i);
                }
                else {
                    _allCells.Add(_id2Cells[i]);
                }

                _id2Cells.Remove(i);
            }

            // destroy invisible cells
            foreach (var pair in _id2Cells) {
                var cell = pair.Value;
                _allCellIds.Remove(pair.Key);
                ReturnCell(cell);
                //Debug.Log("Return " + pair.Key);
            }
        }

        private void DoReset() {
            DestroyCells();
            _cellPrefab.gameObject.SetActive(true);
            SetTopLeftAnchor(_cellPrefab);

            _cellPrefab.gameObject.SetActive(false);
            if (_updateCor != null) {
                _scrollRect.StopCoroutine(_updateCor);
            }

            _allTasks.Clear();
            _allCellIds.Clear();
            _allCells.Clear();
            _id2Cells.Clear();
        }

        public void DestroyCells(bool isClearPool = false) {
            _allTasks.Clear();
            _allCellIds.Clear();
            foreach (var cell in _allCells) {
                ReturnCell(cell);
            }
            _allCells.Clear();
            if (isClearPool) {
                var count = _itemPool.Count;
                for (int i = 0; i < count; i++) {
                    var item = _itemPool.Pop();
                    if (item != null) {
                        GameObject.Destroy(item);
                    }
                }
                _itemPool.Clear();
            }
            else {
                // Auto shrink pool size
                int maxCount = _totalRowOrColCount * _numPerRowOrCol;
                var needDeleteCacheCount = _itemPool.Count - maxCount;
                for (int i = 0; i < needDeleteCacheCount; i++) {
                    var item = _itemPool.Pop();
                    if (item != null) {
                        GameObject.Destroy(item);
                    }
                }
            }



        }


        RectTransform GetOrCreateCell() {
            if (_itemPool.Count > 0) {
                var item = _itemPool.Pop();
                if (item != null) {
                    item.gameObject.SetActive(true);
                    return item.transform as RectTransform;
                }
            }

            var tran = (UnityEngine.Object.Instantiate(_cellPrefab.gameObject)).GetComponent<RectTransform>();
            //tran.name = "Cell";
            tran.gameObject.SetActive(true);
            tran.SetParent(_content, false);
            return tran;
        }

        private void ReturnCell(ScrollRectCell cell) {
            var go = cell.Item.gameObject;
            if (go == null) return;
            go.SetActive(false);
            _itemPool.Push(go);
        }

        public void OnValueChanged() {
            UpdateBoundInfo();
            CheckVisibility();
        }


        private void SetTopLeftAnchor(RectTransform rectTransform) {
            SetAnchor(rectTransform, new Vector2(0, 1));
        }

        private void SetTopAnchor(RectTransform rectTransform) {
            SetAnchor(rectTransform, new Vector2(0.5f, 1));
        }

        private void SetLeftAnchor(RectTransform rectTransform) {
            SetAnchor(rectTransform, new Vector2(0, 0.5f));
        }

        private void SetAnchor(RectTransform rectTransform, Vector2 pos) {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}