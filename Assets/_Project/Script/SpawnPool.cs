using System.Collections.Generic;
using UnityEngine;

public static class SpawnPool
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> _pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    public static void InitPool(GameObject prefab, int poolSize, Vector3 scale = default, Transform parent = null, bool active = false)
    {
        if (prefab == null)
        {
            Debug.LogError("[SpawnPool] InitPool called with null prefab!");
            return;
        }
        if (scale == default) scale = Vector3.one;

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            var obj = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            obj.transform.localScale = scale;
            obj.SetActive(active);
            _pools[prefab].Enqueue(obj);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale = default, Transform parent = null, int poolSize = 1)
    {
        if (prefab == null)
        {
            Debug.LogError("[SpawnPool] Spawn called with null prefab!");
            return null;
        }
        if (scale == default) scale = Vector3.one;
        
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        GameObject obj = null;
        while (_pools[prefab].Count > 0 && obj == null)
        {
            obj = _pools[prefab].Dequeue();
        }
        if (obj != null)
        {
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
        Vector3 scale = default, Transform parent = null, int poolSize = 1)
    {
        var spawnedObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = Spawn(prefab, pos, rot, scale, parent, poolSize);
            if (obj != null)
                spawnedObjects.Add(obj);
        }

        return spawnedObjects;
    }

    public static void Despawn(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null)
            return;

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        obj.SetActive(false);
        _pools[prefab].Enqueue(obj);
    }

    public static void DespawnMultiple(GameObject prefab, List<GameObject> objects)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            Despawn(prefab, obj);
        }
    }
}
