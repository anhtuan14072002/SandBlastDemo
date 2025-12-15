using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteSpawner : MonoBehaviour
{
    [Header("Data")] [SerializeField] private ColoringRegionsAsset _asset; // Asset chứa dữ liệu màu
    [Header("UI")] [SerializeField] private Transform _parent; // Container để chứa các nút màu
    [SerializeField] private Button _buttonPrefab; // Template nút màu để spawn
    [Header("Painter")] [SerializeField] private RegionPainterFillByButton _painter; // Component tô màu khi bấm nút

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (_asset == null || _parent == null || _buttonPrefab == null || _painter == null) return;
        if (_asset.colorsCache == null || _asset.colorsCache.Count == 0)
            _asset.RebuildColorsCache();
        for (int i = _parent.childCount - 1; i >= 0; i--)
            Destroy(_parent.GetChild(i).gameObject);
        foreach (var c in _asset.colorsCache)
        {
            var btn = Instantiate(_buttonPrefab, _parent);

            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = c;

            Color32 captured = c;
            btn.onClick.AddListener(() =>
            {
                _painter.FillAllRegionsOfSampledColor(captured); 
            });
        }
    }

    public void SetAsset(ColoringRegionsAsset newAsset)
    {
        _asset = newAsset;
        Build();
    }
}