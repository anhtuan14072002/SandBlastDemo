using System.Collections.Generic;
using GamesTanEncrypt;
using UnityEngine;
using UnityEngine.UI;

namespace GamesTan.UI {
    
    public class ScrollRectCell {
        public RectTransform Item;
        public int Index;
    }
    
    public abstract class BaseSuperScrollRect : ScrollRect {
        public enum EScrollDir {
            Vertical,
            Horizontal
        }

        public ISuperScrollRectDataProvider DataProvider;
        public bool IsGrid;
        public RectTransform CellPrefab;
        public Vector2 Padding;
        public Vector2 Spacing;
        public EScrollDir Direction;
        public int Segment = 1;

        private SuperScrollRectImpl _superScrollRectImpl;

        /// <summary>  All visible Cells  </summary>
        public List<ScrollRectCell> AllCells => _superScrollRectImpl.AllCells;

        /// <summary>  Do initialize  </summary>
        public void DoAwake(ISuperScrollRectDataProvider dataProvider) {
            DataProvider = dataProvider;
            ReloadData();
        }
        
        /// <summary>  Reload All cells, when layout is changed call this  </summary>
        public void ReloadData() {
            if (DataProvider == null) {
                if (Application.isPlaying) {
                    Debug.LogError("Have no dataProvider, please call DoAwake first");
                }
                return;
            }

            StopMovement();
            vertical = Direction == EScrollDir.Vertical;
            horizontal = Direction == EScrollDir.Horizontal;
            if (_superScrollRectImpl == null) {
                _superScrollRectImpl = new SuperScrollRectImpl();
            }
            _superScrollRectImpl.DoAwake(this);
            onValueChanged.RemoveListener(_OnValueChanged);
            _superScrollRectImpl.DoStart();
            onValueChanged.AddListener(_OnValueChanged);
        }

        /// <summary>  Clear pool cache  </summary>
        public void ClearCache() {
            _superScrollRectImpl.ClearCache();
        }

        /// <summary>  Jump to the cell  </summary>
        public void JumpTo(int cellIndex) {
            StopMovement();
            _superScrollRectImpl.JumpTo(cellIndex);
        }

        /// <summary>  Set Max count of creating cell in one frame  </summary>
        public void SetRefreshSpeed(int maxUpdateCountPerFrame = 6) {
            _superScrollRectImpl.MaxCellCreateCountPerFrame = maxUpdateCountPerFrame;
        }

        private void _OnValueChanged(Vector2 normalizedPos) {
            _superScrollRectImpl.OnValueChanged();
        }

    }
}