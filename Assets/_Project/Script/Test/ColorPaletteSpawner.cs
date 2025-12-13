using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ColoringRegionsAsset _asset;

    [Header("UI")]
    [SerializeField] private Transform _parent;
    [SerializeField] private Button _buttonPrefab;

    [Header("Painter")]
    [SerializeField] private RegionPainterFillByButton _painter;

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (_asset == null || _parent == null || _buttonPrefab == null || _painter == null) return;

        // đảm bảo cache palette có dữ liệu
        if (_asset.colorsCache == null || _asset.colorsCache.Count == 0)
            _asset.RebuildColorsCache();

        // clear old
        for (int i = _parent.childCount - 1; i >= 0; i--)
            Destroy(_parent.GetChild(i).gameObject);

        // spawn buttons
        foreach (var c in _asset.colorsCache)
        {
            var btn = Instantiate(_buttonPrefab, _parent);

            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = c;

            Color32 captured = c;
            btn.onClick.AddListener(() =>
            {
                _painter.FillAllRegionsOfSampledColor(captured); // ✅ bấm nút là tô luôn
            });
        }
    }
}