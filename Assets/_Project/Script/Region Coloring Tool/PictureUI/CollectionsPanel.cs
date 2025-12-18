using System.Collections.Generic;
using Sand;
using UnityEngine;
using Zenject;

public class CollectionsPanel : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ColoringBookDatabase _database;

    [Header("Refs")]
    [SerializeField] private ColoringBookRuntime _runtime;

    [Header("UI")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private CollectionItemUI _itemPrefab;
    
    [Inject] private DiContainer _container;
    [Inject] private UserData _userData;
    private List<CollectionItemUI> _spawnedItems = new();
    
    private void Start()
    {
        Build();
        UpdateThumbnailCompleted();
    }

    public void UpdateThumbnailCompleted(int index)
    {
        if (_spawnedItems.Count == 0) return;
        _spawnedItems[index].UpdateThumbnailCompleted(index);
    }

    public void UpdateThumbnailCompleted()
    {
        foreach (var completedId in _userData.CompletedPictureIndices)
        {
            if (completedId >= 0 && completedId < _spawnedItems.Count)
                _spawnedItems[completedId].UpdateThumbnailCompleted(completedId);
        }

    }
    public void Build()
    {
        if (_database == null || _database.Count == 0) return;
        // clear
        for (int i = _contentParent.childCount - 1; i >= 0; i--)
            Destroy(_contentParent.GetChild(i).gameObject);
        _spawnedItems.Clear();
        
        // spawn
        for (int i = 0; i < _database.Count; i++)
        {
            var entry = _database.Get(i);
            if (entry == null || entry.asset == null) continue;
            var item = _container.InstantiatePrefabForComponent<CollectionItemUI>(_itemPrefab.gameObject, _contentParent);
            item.Setup(i, entry.asset, _runtime);
            _spawnedItems.Add(item);
        }
    }
    
}