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

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (_database == null || _database.Count == 0) return;
        // clear
        for (int i = _contentParent.childCount - 1; i >= 0; i--)
            Destroy(_contentParent.GetChild(i).gameObject);

        // spawn
        for (int i = 0; i < _database.Count; i++)
        {
            var entry = _database.Get(i);
            if (entry == null || entry.asset == null) continue;

            var item = _container.InstantiatePrefabForComponent<CollectionItemUI>(_itemPrefab.gameObject, _contentParent);
            item.Setup(i, entry.asset, _runtime);
        }
    }
}