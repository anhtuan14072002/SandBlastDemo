using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HootyBird.ColoringBook.Serialized
{
    [CreateAssetMenu(fileName = "Images Coloring Book Data", menuName = "ColoringBook/Create Images Coloring Book Data Asset")]
    public class ImagesColoringBookData : ColoringBookDataBase
    {
#if UNITY_EDITOR
        [SerializeField]
        private int errorSize = 25;
        [SerializeField]
        private Vector2 panelPosition;
        [SerializeField]
        private float panelZoomValue = 1f;
#endif

        [SerializeField]
        private List<RegionData> regions = new List<RegionData>();

        public override IEnumerable<IRegionData> Regions => regions.Cast<IRegionData>();
        public override IEnumerable<Color> Colors
        {
            get
            {
                List<Color> colors = new List<Color>();
                for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
                {
                    Color c = regions[regionIndex].MainColor;
                    if (!colors.Any(color => color.Compare(c)))
                    {
                        colors.Add(c);
                    }
                }

                return colors;
            }
        }

        // Assets stored as serialized references, nothing to initialize here.
        public override void InitializeAssets() { }

        // Assets stored as serialized references, nothing to release here.
        public override void ReleaseAssets() { }
    }
}