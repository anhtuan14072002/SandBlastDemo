using HootyBird.ColoringBook.Tools;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace HootyBird.ColoringBook.Services
{
    /// <summary>
    /// Calculates pixels in RenderTexture using CPU.
    /// </summary>
    public class TextureComputeJob : ITextureCompute
    {
        private JobHandle jobHandle;

        public Color Mask { get; set; }

        public TextureComputeJob()
        {
        }

        public uint CountPixels(RenderTexture input)
        {
            RenderTexture.active = input;
            Texture2D textureCopy = new Texture2D(
                input.width,
                input.height,
                TextureFormat.R8,
                false);

            textureCopy.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0);
            textureCopy.Apply();

            // Extract texture into count job.
            CountPixelsJob countPixelsJob = new CountPixelsJob()
            {
                input = textureCopy.GetPixelData<byte>(0),
                inputTextureSize = new NativeReference<int2>(new int2(textureCopy.width, textureCopy.height), Allocator.TempJob),
            };
            countPixelsJob.Init();
            jobHandle = countPixelsJob.Schedule();
            jobHandle.Complete();

            uint count = countPixelsJob.GetPixelsCount();
            countPixelsJob.Dispose();

            // Clear texture.
            Object.DestroyImmediate(textureCopy);

            return count;
        }

        public void Dispose()
        {
        }
    }
}
