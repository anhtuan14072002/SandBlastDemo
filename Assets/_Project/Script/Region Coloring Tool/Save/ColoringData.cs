using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class lưu trữ tiến độ tô màu của một ảnh cụ thể
/// </summary>
[Serializable] 
public class ColoringProgressData
{
    /// <summary>
    /// ID duy nhất của ảnh (index trong database)
    /// </summary>
    public int pictureIndex;
    /// <summary>
    /// Danh sách các vùng đã được tô màu của ảnh này
    /// </summary>
    public List<ColoredRegionData> coloredRegions = new List<ColoredRegionData>();
}
/// <summary>
/// Class lưu thông tin một vùng đã được tô màu
/// </summary>
[Serializable]
public class ColoredRegionData
{
    /// <summary>
    /// ID của vùng (region) được tô
    /// </summary>
    public int regionId;
    /// <summary>
    /// Màu đã được tô cho vùng này
    /// </summary>
    public Color32 appliedColor;
}

/// <summary>
/// Database chính lưu trữ tất cả tiến độ tô màu
/// Nó là container của tất cả các ảnh và tiến độ tương ứng
/// </summary>
[Serializable] 
public class ColoringProgressDatabase
{
    /// <summary>
    /// Danh sách tất cả các ảnh kèm tiến độ tô màu
    /// </summary>
    public List<ColoringProgressData> progressList = new List<ColoringProgressData>();
    
    /// <summary>
    /// Tìm kiếm tiến độ của một ảnh theo index
    /// </summary>
    public ColoringProgressData GetProgress(int pictureIndex)
    {
        //tìm phần tử đầu tiên có pictureIndex trùng
        return progressList.Find(p => p.pictureIndex == pictureIndex);
    }

    /// <summary>
    /// Lưu hoặc cập nhật tiến độ của một ảnh
    /// </summary>
    /// <param name="pictureIndex">Index của ảnh</param>
    /// <param name="data">Dữ liệu tiến độ cần lưu</param>
    public void SaveProgress(int pictureIndex, ColoringProgressData data)
    {
        // Kiểm tra xem ảnh này đã có dữ liệu chưa
        var existing = GetProgress(pictureIndex);
        // Nếu dữ liệu cũ tồn tại, xóa nó đi (để tránh duplicate)
        if (existing != null) progressList.Remove(existing);
        // Thêm dữ liệu mới vào danh sách
        progressList.Add(data);
    }
}