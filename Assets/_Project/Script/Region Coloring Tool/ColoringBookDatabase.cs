using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColoringBookDatabase", menuName = "ColoringBook/Database")]
public class ColoringBookDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id; // optional: "frog_01"
        public string displayName; // UI name
        public ColoringRegionsAsset asset; // asset của ảnh đó
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>(); // Danh sách tất cả các ảnh trong cơ sở dữ liệu

    public IReadOnlyList<Entry> Entries => _entries;

    public int Count => _entries?.Count ?? 0;

    public Entry Get(int index)
    {
        if (_entries == null || _entries.Count == 0) return null;
        if (index < 0 || index >= _entries.Count) return null;
        return _entries[index];
    }

    public ColoringRegionsAsset GetAsset(int index) => Get(index)?.asset;

    public int IndexOfId(string id)
    {
        if (string.IsNullOrEmpty(id) || _entries == null) return -1;
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i] != null && _entries[i].id == id)
                return i;
        return -1;
    }
}