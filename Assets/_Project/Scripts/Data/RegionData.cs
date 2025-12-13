using HootyBird.ColoringBook.Data;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace HootyBird.ColoringBook.Serialized
{
    [Serializable]
    public class RegionData : IRegionData
    {
        [SerializeField]
        private Texture2D texture;
        [SerializeField]
        private Color mainColor;
        [SerializeField]
        private int2 location;
        [SerializeField]
        private int2 textureSize;
        [SerializeField]
        private int2 colorIndexLocation;
        [SerializeField]
        private int originalPixelCount;

        /// <summary>
        /// What downscale value was used during pixel count.
        /// </summary>
        [SerializeField]
        private int downscale;
        /// <summary>
        /// Pixel count, according to downscale value.
        /// </summary>
        [SerializeField]
        private int downscalePixelCount;

#if UNITY_EDITOR
        public bool active;
#endif

        public Texture2D Texture => texture;
        public Color MainColor => mainColor;
        public int2 Location => location;
        public int2 TextureSize => textureSize;
        public int2 ColorIndexLocation => colorIndexLocation;
        public int Downscale => downscale;
        public int DownscalePixelCount => downscalePixelCount;

        public int PixelCount => originalPixelCount;

        public void SetTexture(Texture2D texture)
        {
            this.texture = texture;
        }

        public void SetMainColor(Color mainColor)
        {
            this.mainColor = mainColor;
        }

        public void SetLocation(int2 location)
        {
            this.location = location;
        }

        public void SetTextureSize(int2 textureSize)
        {
            this.textureSize = textureSize;
        }

        public void SetColorIndexLocation(int2 colorIndexLocation)
        {
            this.colorIndexLocation = colorIndexLocation;
        }

        public void SetPixelCount(int pixelCount)
        {
            originalPixelCount = pixelCount;
        }

        public void SetDownscaleValue(int downscale)
        {
            this.downscale = downscale;
        }

        public void SetDownscalePixelCount(int downscalePixelCount)
        {
            this.downscalePixelCount = downscalePixelCount;
        }
    }
}
