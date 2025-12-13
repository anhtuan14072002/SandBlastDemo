using UnityEngine;

namespace HootyBird.ColoringBook.Services
{
    public class TextureComputeService : MonoBehaviour
    {
        private static TextureComputeService instance;

        [SerializeField]
        private bool useComputeShader = true;

        private ITextureCompute textureCompute;

        private void Start()
        {
            // Check if compute shader can be used.
            if (SystemInfo.supportsComputeShaders)
            {
                RenderTexture prev = RenderTexture.active;
                RenderTexture temp = RenderTexture.GetTemporary(4096, 4096, 0, RenderTextureFormat.R8);
                RenderTexture.active = temp;
                GL.Clear(true, true, Color.red);
                try
                {
                    // Use GPU to compute texture.
                    textureCompute = new TextureComputeShaderR8();
                    textureCompute.CountPixels(temp);
                }
                catch (System.Exception e)
                {
                    UseJobCompute();

                    Debug.LogError($"Failed to use computeShader: {e}");
                }
                finally
                {
                    if (temp)
                    {
                        RenderTexture.ReleaseTemporary(temp);
                    }
                    RenderTexture.active = prev;

                    Debug.Log($"Use computeShader for texture evaluation: {useComputeShader}");
                }
            }
            else
            {
                UseJobCompute();
            }

            void UseJobCompute()
            {
                useComputeShader = false;

                // Use CPU to compute texture.
                textureCompute = new TextureComputeJob();
            }
        }

        private void OnDestroy()
        {
            textureCompute.Dispose();
        }

        public static uint CountPixels(RenderTexture texture, Color colorMask)
        {
            if (Application.isPlaying)
            {
                instance.textureCompute.Mask = colorMask;
                return instance.textureCompute.CountPixels(texture);
            }

#if UNITY_EDITOR
            else
            {
                // In editor, use texture compute shader with full texture resolution.
                using (TextureComputeShaderR8 textureCompute = new TextureComputeShaderR8())
                {
                    textureCompute.Mask = colorMask;
                    return textureCompute.CountPixels(texture);
                }
            }
#else
            return 0;
#endif
        }

        /// <summary>
        /// For counting pixels on a R8 texture.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static uint CountPixels(RenderTexture texture)
        {
            return instance.textureCompute.CountPixels(texture);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            GameObject go = new GameObject("TextureComputeService");
            instance = go.AddComponent<TextureComputeService>();
            DontDestroyOnLoad(go);
        }
    }
}
