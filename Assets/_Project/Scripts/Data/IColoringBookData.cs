using System.Collections.Generic;
using UnityEngine;

namespace HootyBird.ColoringBook.Data
{
    public interface IColoringBookData
    {
        Texture2D Texture { get; }
        string Name { get; }
        IEnumerable<Color> Colors { get; }
        IEnumerable<IRegionData> Regions { get; }
        ColoringStyle ColoringStyle { get; }

        int ColorIndex(Color color);
        void InitializeAssets();
        void ReleaseAssets();
    }

    public enum ColoringStyle
    {
        /// <summary>
        /// Fills coloring book regions with FillColor.
        /// </summary>
        FillRegion,

        /// <summary>
        /// Drawing will clear coloring book region.
        /// </summary>
        RemoveRegion,
    }
}
