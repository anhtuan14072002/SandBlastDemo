using UnityEngine;

namespace HootyBird.ColoringBook.Services
{
    /// <summary>
    /// Calculates pixels in RenderTexture using GPU.
    /// Only Red pixels count.
    /// </summary>
    public class TextureComputeShaderR8 : ITextureCompute
    {
        private ComputeShader computeShader;
        private ComputeBuffer computeBuffer;
        private int handleInit;
        private int handleMain;
        private uint[] data;
        private Color mask = new Color(1f, 0f, 0f, 0f);

        public Color Mask
        {
            get => mask;
            set { }
        }

        public TextureComputeShaderR8()
        {
            computeShader = Resources.Load<ComputeShader>("ComputeShaders/countPixelsR8");

            // Init shader.
            handleInit = computeShader.FindKernel("CSInit");
            handleMain = computeShader.FindKernel("CSMain");
            computeBuffer = new ComputeBuffer(1, sizeof(uint));
            data = new uint[1];
            computeShader.SetBuffer(handleMain, "computeBuffer", computeBuffer);
            computeShader.SetBuffer(handleInit, "computeBuffer", computeBuffer);
        }

        public uint CountPixels(RenderTexture input)
        {
            computeShader.SetVector("mask", new Vector4(Mask.r, Mask.g, Mask.b, Mask.a));
            computeShader.SetTexture(handleMain, "image", input);

            computeShader.Dispatch(handleInit, 64, 1, 1);
            computeShader.Dispatch(handleMain, input.width / 8, input.height / 8, 1);
            computeBuffer.GetData(data);

            return data[0];
        }

        public void Dispose()
        {
            if (computeBuffer != null)
            {
                computeBuffer.Release();
            }
        }
    }
}
