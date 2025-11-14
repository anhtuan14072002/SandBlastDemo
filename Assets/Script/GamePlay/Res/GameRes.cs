using UnityEngine;

namespace Sand
{
    public static class GameRes
    {
        private const string PrefabsPath = "Prefabs/";
        private const string BlockPrefab = "BlockPrefab/";
        
        public static GameObject LoadBlockPrefab(int id)
        {
            var prefab = Resources.Load<GameObject>(PrefabsPath + BlockPrefab + id);
            if (prefab == null)
                Debug.Log("Khong tim thay " + PrefabsPath + BlockPrefab + id);
            return prefab;
        }
    }
}