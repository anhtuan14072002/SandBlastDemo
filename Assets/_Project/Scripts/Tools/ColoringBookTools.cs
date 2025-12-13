using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace HootyBird.ColoringBook.Tools
{
    public static class ColoringBookTools
    {
        private static Material countPixelsMaterial;
        private static Shader colorMaskShader;
        private static Material alphaOnlyMaterial;
        private static Shader alphaOnlyShader;

        public static bool TextureHavePixelAt(Texture2D inputTexture, int2 location)
        {
            // Create texture copy.
            Texture2D copy = GetTextureCopy(inputTexture, singleChannel: true);
            // Clear texture copy.
            Color result = copy.GetPixel(location.x, location.y);
            if (Application.isEditor)
            {
                Object.DestroyImmediate(copy);
            }
            else
            {
                Object.Destroy(copy);
            }

            return result.r > .1f;
        }

        public static RenderTexture GetDownscaleTexture(Texture input, Color colorMask, int downscale)
        {
            // Extract selected colorMask.
            RenderTexture temp = RenderTexture.GetTemporary(
                input.width / downscale,
                input.height / downscale,
                0,
                RenderTextureFormat.R8);

            BlitR8ToRenderTexture(input, temp, colorMask);

            return temp;
        }

        public static void BlitR8ToRenderTexture(Texture input, RenderTexture target, Color colorMask)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.clear);

            if (!colorMaskShader)
            {
                colorMaskShader = Resources.Load<Shader>("Shaders/ColorMask");
            }
            if (!countPixelsMaterial)
            {
                countPixelsMaterial = new Material(colorMaskShader);
            }
            countPixelsMaterial.SetColor("_Color", colorMask);
            Graphics.Blit(input, target, countPixelsMaterial);
            RenderTexture.active = previous;
        }

        public static int GetDownscaleValue(int pixelsCount)
        {
            for (int index = 0; index < Settings.InternalAppSettings.DownscaleSettings.Length; index++)
            {
                if (pixelsCount < Settings.InternalAppSettings.DownscaleSettings[index].size)
                {
                    return Settings.InternalAppSettings.DownscaleSettings[index].downscale;
                }
            }

            return 32;
        }

        public static int GetPixelErrorSize(int pixelsCount)
        {
            for (int index = 0; index < Settings.InternalAppSettings.RegionsErrorSize.Length; index++)
            {
                if (pixelsCount < Settings.InternalAppSettings.RegionsErrorSize[index].size)
                {
                    return Mathf.RoundToInt(pixelsCount * Settings.InternalAppSettings.RegionsErrorSize[index].errorSize);
                }
            }

            // Default error size of 1%.
            return Mathf.RoundToInt(pixelsCount * .99f);
        }

        public static Texture2D GetTextureCopy(
            Texture inputTexture, 
            bool singleChannel = false)
        {
            int width = inputTexture.width;
            int height = inputTexture.height;

            RenderTexture renderTex =
                singleChannel ?
                RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.R8)
                :
                RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB);

            if (singleChannel)
            {
                if (!alphaOnlyShader)
                {
                    alphaOnlyShader = Resources.Load<Shader>("Shaders/AlphaOnly");
                }
                if (!alphaOnlyMaterial)
                {
                    alphaOnlyMaterial = new Material(alphaOnlyShader);
                }
                Graphics.Blit(inputTexture, renderTex, alphaOnlyMaterial);
            }
            else
            {
                Graphics.Blit(inputTexture, renderTex);
            }
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D textureCopy = new Texture2D(
                renderTex.width,
                renderTex.height, 
                singleChannel ? TextureFormat.R8 : TextureFormat.RGBAFloat, 
                false);
            textureCopy.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);

            textureCopy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(renderTex);

            return textureCopy;
        }

        public static Color FindSimilar(Color target, List<Color> colors, float error = .1f)
        {
            for (int index = 0; index < colors.Count; index++)
            {
                if (target.Compare(colors[index], error))
                {
                    return colors[index];
                }
            }

            return target;
        }

        public static bool Compare(this Color a, Color b, float error = .01f)
        {
            return
                Mathf.Abs(a.r - b.r) <= error &&
                Mathf.Abs(a.g - b.g) <= error &&
                Mathf.Abs(a.b - b.b) <= error;
        }
    }
}
