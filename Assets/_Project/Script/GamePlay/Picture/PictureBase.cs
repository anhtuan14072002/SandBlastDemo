using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    [System.Serializable]
    public class PictureData
    {
        public int Index;
        public Sprite Sprite;
        public Sprite ColorSprite;
    }

    public class PictureBase : MonoBehaviour
    {
        [Header("UI")] 
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private PictureCell _cellPrefab;

        [Header("Data")] 
        [SerializeField] private List<Sprite> _sprites = new();       // outline sprites
        [SerializeField] private List<Sprite> _colorSprites = new();  // color sprites
        [SerializeField] private int _count;
        [SerializeField] private RenderPicture _renderPicture;

        private readonly List<PictureData> _datas = new();
        private readonly List<PictureCell> _cells = new();   // list các cell trong scroll
        private bool _initialized = false;

        private void Start()
        {
            if (_initialized) return;
            BuildData();
            CreateCells();
            _initialized = true;
        }

        private void BuildData()
        {
            _datas.Clear();

            for (int i = 0; i < _count; i++)
            {
                _datas.Add(new PictureData
                {
                    Index = i,
                    Sprite = i < _sprites.Count ? _sprites[i] : null,
                    ColorSprite = i < _colorSprites.Count ? _colorSprites[i] : null
                });
            }
        }

        private void CreateCells()
        {
            if (_scrollRect == null || _content == null || _cellPrefab == null) return;

            ClearCells();
            _cells.Clear();

            foreach (var data in _datas)
            {
                var cell = Instantiate(_cellPrefab, _content);
                cell.gameObject.SetActive(true);
                cell.Init(data.Sprite, data.ColorSprite, _renderPicture, data.Index);
                _cells.Add(cell);
            }

            _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void UpdatePictureSprite(int index, Sprite newSprite)
        {
            if (index < 0 || index >= _datas.Count) return;
            _datas[index].Sprite = newSprite;
            if (index < _cells.Count && _cells[index] != null)
                _cells[index].SetPreviewSprite(newSprite);
        }

        private void ClearCells()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }
        }
    }
}
