using UnityEngine;

public class ColoringBookRuntime : MonoBehaviour
{
    [Header("Database")] [SerializeField] public ColoringBookDatabase _db;
    [Header("Refs")] [SerializeField] private RegionPainterFillByButton _painter;
    [SerializeField] private ColorPaletteSpawner _palette;
    [Header("Start")] [SerializeField] private int _startIndex = 0;

    public int CurrentIndex { get; private set; } = -1;
    public ColoringBookDatabase _db_exposed => _db;

    private void Start()
    {
        if (_db == null || _db.Count == 0) return;
        if (_palette != null) _palette.SetRuntime(this);
    }

    public void SetIndex(int index)
    {
        if (_db == null || _db.Count == 0) return;
        index = Mathf.Clamp(index, 0, _db.Count - 1);
        var entry = _db.Get(index);
        if (entry == null || entry.asset == null) return;
        CurrentIndex = index;
        if (_painter != null) _painter.SetAsset(entry.asset);
        if (_palette != null) _palette.SetAsset(entry.asset);
    }

    #region Next Prev Picturte

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

    #endregion

    public void SetById(string id)
    {
        if (_db == null) return;
        int idx = _db.IndexOfId(id);
        if (idx >= 0) SetIndex(idx);
        else Debug.Log($"runtime id not found: {id}");
    }
}