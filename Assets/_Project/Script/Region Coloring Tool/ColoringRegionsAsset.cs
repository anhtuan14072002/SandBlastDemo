using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColoringRegionsAsset", menuName = "ColoringBook/Coloring Regions Asset")]
public class ColoringRegionsAsset : ScriptableObject
{
    [Header("Info")] public int imageId; 
    [Header("Input")] public Texture2D sourceTexture; // Ảnh gốc dùng để xử lý flood fill
    [Header("FloodFill Settings")] 
    [Range(0f, 0.5f)] public float blackThreshold = 0.18f; // Ngưỡng để coi pixel nào là đen (viền)
    [Range(0f, 0.25f)] public float colorTolerance = 0.04f; // Độ chênh lệch màu được chấp nhận khi tìm vùng cùng màu
    [Header("Regions")] public List<RegionEntry> regions = new(); // Danh sách tất cả các vùng tô trong ảnh
    [Header("Colors Cache (for UI palette)")]
    public List<Color32> colorsCache = new(); // Bộ nhớ đệm màu dùng cho palette UI
    [Header("Textures")] public Texture2D outlineTexture; // Ảnh đã qua xử lý (viền trắng, chưa tô)
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
        public int id; // ID duy nhất của vùng
        public string displayName; // Tên vùng
        public Vector2Int seed; // Tọa độ điểm bắt đầu flood fill
        public Color32 sampledColor; // Màu được lấy mẫu từ ảnh gốc
        public Color32 overrideColor; // Màu thực tế để tô (có thể khác sampledColor)
        public bool hidden; // Ẩn vùng này khỏi preview
        public bool mergedFlag; // Đánh dấu vùng đã được merge
        public Texture2D attachedTexture; // Texture có thể gắn vào vùng
        public RectInt bounds; // Hộp giới hạn của vùng
        public byte[] maskBits; // Bitmap biểu diễn vùng (bit = 1 → pixel thuộc vùng)
        public bool filled; // Đã tô xong vùng này chưa
    }
}