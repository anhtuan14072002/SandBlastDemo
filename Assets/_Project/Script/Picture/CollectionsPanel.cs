using UnityEngine;

public class CollectionsPanel : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ColoringBookDatabase _database;

    [Header("Refs")]
    [SerializeField] private ColoringBookRuntime _runtime;

    [Header("UI")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private CollectionItemUI _itemPrefab;

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (_database == null || _database.Count == 0)
        {
            Debug.LogWarning("[CollectionsPanel] Database empty");
            return;
        }

        // clear
        for (int i = _contentParent.childCount - 1; i >= 0; i--)
            Destroy(_contentParent.GetChild(i).gameObject);

        // spawn
        for (int i = 0; i < _database.Count; i++)
        {
            var entry = _database.Get(i);
            if (entry == null || entry.asset == null) continue;

            var item = Instantiate(_itemPrefab, _contentParent);
            item.Setup(i, entry.asset, _runtime);
        }
    }
}