using UnityEngine;

namespace HootyBird.ColoringBook.Services
{
    /// <summary>
    /// Calculates pixels in RenderTexture using GPU.
    /// </summary>
    public class TextureComputeShader : ITextureCompute
    {
        private ComputeShader computeShader;
        private ComputeBuffer computeBuffer;
        private int handleInit;
        private int handleMain;
        private uint[] data;

        public Color Mask { get; set; }

        public TextureComputeShader()
        {
            computeShader = Resources.Load<ComputeShader>("ComputeShaders/countPixels");

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
