using Unity.Mathematics;
using UnityEngine;

namespace HootyBird.ColoringBook.Data
{
    public interface IRegionData
    {
        Texture2D Texture { get; }
        Color MainColor { get; }
        int2 Location { get; }
        int2 TextureSize { get; }
        int2 ColorIndexLocation { get; }
        int DownscalePixelCount { get; }
        int Downscale { get; }
    }
}
