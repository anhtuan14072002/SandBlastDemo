using UnityEngine;

public class ColoringBookRuntime : MonoBehaviour
{
    [Header("Database")] [SerializeField] private ColoringBookDatabase _db; // Database chứa tất cả ảnh
    [Header("Start")] [SerializeField] private int _startIndex = 0; // Ảnh bắt đầu khi play
    [Header("Refs")] [SerializeField] private RegionPainterFillByButton _painter; // Component tô màu
    [SerializeField] private ColorPaletteSpawner _palette; // Component spawn palette
    [Header("Debug")] [SerializeField] private bool _logSelect = true;
    public int CurrentIndex { get; private set; } = -1; // Index ảnh hiện tại đang xem

    private void Start()
    {
        if (_db == null || _db.Count == 0)
        {
            Debug.LogError("[ColoringBookRuntime] Database null/empty");
            return;
        }
        // SetIndex(Mathf.Clamp(_startIndex, 0, _db.Count - 1));
    }

    public void SetIndex(int index)
    {
        if (_db == null || _db.Count == 0) return;
        index = Mathf.Clamp(index, 0, _db.Count - 1);

        var entry = _db.Get(index);
        if (entry == null || entry.asset == null)
        {
            Debug.LogError($"[ColoringBookRuntime] Entry/asset null at index {index}");
            return;
        }

        CurrentIndex = index;

        // đổi ảnh
        if (_painter != null) _painter.SetAsset(entry.asset);
        if (_palette != null) _palette.SetAsset(entry.asset);

        if (_logSelect)
            Debug.Log($"[ColoringBookRuntime] Selected: index={index}, name={entry.displayName}, id={entry.id}");
    }

    public void Next()
    {
        if (_db == null || _db.Count == 0) return;
        int next = (CurrentIndex + 1) % _db.Count;
        SetIndex(next);
    }

    public void Prev()
    {
        if (_db == null || _db.Count == 0) return;
        int prev = CurrentIndex - 1;
        if (prev < 0) prev = _db.Count - 1;
        SetIndex(prev);
    }

    public void SetById(string id)
    {
        if (_db == null) return;
        int idx = _db.IndexOfId(id);
        if (idx >= 0) SetIndex(idx);
        else Debug.LogWarning($"[ColoringBookRuntime] id not found: {id}");
    }
}