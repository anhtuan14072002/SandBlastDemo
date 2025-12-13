using System.Collections.Generic;
using System.Linq;
using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Tools;
using UnityEngine;

namespace HootyBird.ColoringBook.Serialized
{
    public abstract class ColoringBookDataBase : ScriptableObject, IColoringBookData
    {
        [SerializeField]
        protected Texture2D texture;
        [SerializeField]
        protected ColoringStyle coloringStyle;

        public Texture2D Texture => texture;

        public string Name => name;

        public virtual IEnumerable<Color> Colors { get; }

        public virtual IEnumerable<IRegionData> Regions { get; }

        public ColoringStyle ColoringStyle => coloringStyle;

        public virtual int ColorIndex(Color color)
        {
            List<Color> colors = Colors.ToList();

            for (int colorIndex = 0; colorIndex < colors.Count(); colorIndex++)
            {
                if (colors[colorIndex].Compare(color))
                {
                    return colorIndex;
                }
            }

            return -1;
        }

        public abstract void InitializeAssets();

        public abstract void ReleaseAssets();
    }
}
