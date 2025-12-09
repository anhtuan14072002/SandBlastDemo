using System.Collections.Generic;
using UnityEngine;

public static class SpawnPool
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> _pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    public static void InitPool(GameObject prefab, int poolSize, Vector3 scale, Transform parent = null)
    {
        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var obj = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            obj.transform.localScale = scale;
            obj.SetActive(false);
            _pools[prefab].Enqueue(obj);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale,
        Transform parent = null, int poolSize = 1)
    {
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();

        if (_pools[prefab].Count > 0)
        {
            var obj = _pools[prefab].Dequeue();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.transform.localScale = scale;
            obj.transform.SetParent(parent);
            obj.SetActive(true);
            return obj;
        }

        for (int i = 0; i < poolSize - 1; i++)
        {
            var extraObj = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            extraObj.transform.localScale = scale;
            extraObj.SetActive(false);
            _pools[prefab].Enqueue(extraObj);
        }

        var newObj = Object.Instantiate(prefab, pos, rot, parent);
        newObj.transform.localScale = scale;
        return newObj;
    }

    public static List<GameObject> SpawnMultiple(GameObject prefab, int count, Vector3 pos, Quaternion rot,
        Vector3 scale, Transform parent = null, int poolSize = 1)
    {
        var spawnedObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = Spawn(prefab, pos, rot, scale, parent, poolSize);
            spawnedObjects.Add(obj);
        }

        return spawnedObjects;
    }

    public static void Despawn(GameObject prefab, GameObject obj)
    {
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        obj.SetActive(false);
        _pools[prefab].Enqueue(obj);
    }

    public static void DespawnMultiple(GameObject prefab, List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            Despawn(prefab, obj);
        }
    }
}