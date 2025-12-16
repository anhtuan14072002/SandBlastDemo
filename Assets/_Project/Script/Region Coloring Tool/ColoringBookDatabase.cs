using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColoringBookDatabase", menuName = "ColoringBook/Database")]
public class ColoringBookDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        public ColoringRegionsAsset asset;
    }

    [SerializeField] private List<Entry> _entries = new(); 
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
            if (_entries[i] != null && _entries[i].id == id) return i;
        return -1;
    }
}