using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColoringRegionsAsset", menuName = "ColoringBook/Coloring Regions Asset")]
public class ColoringRegionsAsset : ScriptableObject
{
    [Header("Input")]
    public Texture2D sourceTexture;

    [Header("FloodFill Settings")]
    [Range(0f, 0.5f)] public float blackThreshold = 0.18f;
    [Range(0f, 0.25f)] public float colorTolerance = 0.04f;

    [Header("Regions")]
    public List<RegionEntry> regions = new();

    [Header("Colors Cache (for UI palette)")]
    public List<Color32> colorsCache = new();
    
    [Header("Textures")]
    public Texture2D outlineTexture;   // ảnh chưa tô (chỉ viền)
    public Texture2D colorTexture;     // ảnh đã tô màu (reference)


    public int GetNextId()
    {
        int max = 0;
        for (int i = 0; i < regions.Count; i++) max = Mathf.Max(max, regions[i].id);
        return max + 1;
    }

    public RegionEntry GetRegion(int id) => regions.Find(r => r != null && r.id == id);

    public void RebuildColorsCache()
    {
        colorsCache.Clear();
        var set = new HashSet<int>();
        foreach (var r in regions)
        {
            if (r == null) continue;
            int key = (r.overrideColor.r << 24) | (r.overrideColor.g << 16) | (r.overrideColor.b << 8) | r.overrideColor.a;
            if (set.Add(key)) colorsCache.Add(r.overrideColor);
        }
    }

    [Serializable]
    public class RegionEntry
    {
        public int id;
        public string displayName;         // để hiện trong list
        public Vector2Int seed;

        public Color32 sampledColor;
        public Color32 overrideColor;

        public bool hidden;               // hide selected
        public bool mergedFlag;           // đánh dấu đã bị merge vào region khác (optional)

        public Texture2D attachedTexture; // optional: texture con/miếng cắt của vùng (nếu bro tạo)

        public RectInt bounds;            // bounding box
        public byte[] maskBits;           // bitset theo bounds (1bit/pixel)
        public bool filled;   
    }
}