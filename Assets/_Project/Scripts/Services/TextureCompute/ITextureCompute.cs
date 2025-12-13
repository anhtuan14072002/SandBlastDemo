using UnityEngine;

namespace HootyBird.ColoringBook.Services
{
    public interface ITextureCompute : System.IDisposable
    {
        Color Mask { get; set; }
        uint CountPixels(RenderTexture input);
    }
}
