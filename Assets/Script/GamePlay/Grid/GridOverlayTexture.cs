using UnityEngine;

namespace Sand
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GridOverlayTexture : MonoBehaviour
    {
        [Header("Logical grid size (cells)")]
        public int width = 10;
        public int height = 20;

        [Header("Visual")]
        [Tooltip("Số pixel cho mỗi ô (càng lớn thì line càng rõ)")]
        public int pixelsPerCell = 4;

        [Tooltip("Màu viền (nên để trắng, alpha khoảng 0.3–0.6)")]
        public Color lineColor = new Color(1f, 1f, 1f, 0.4f);

        private SpriteRenderer _sr;
        private Texture2D _tex;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            BuildTexture();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                _sr = GetComponent<SpriteRenderer>();
                BuildTexture();
            }
        }
#endif

        public void Init(int w, int h)
        {
            width = Mathf.Max(1, w);
            height = Mathf.Max(1, h);
            BuildTexture();
        }

        void BuildTexture()
        {
            if (width <= 0 || height <= 0 || pixelsPerCell <= 0) return;

            int texW = width * pixelsPerCell;
            int texH = height * pixelsPerCell;

            if (_tex != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_tex);
                else
                    Destroy(_tex);
#else
                Destroy(_tex);
#endif
            }

            _tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Point;
            _tex.wrapMode = TextureWrapMode.Clamp;

            var colors = new Color32[texW * texH];
            var lc = (Color32)lineColor;

            for (int y = 0; y < texH; y++)
            {
                int cy = y / pixelsPerCell;
                int ly = y % pixelsPerCell;

                for (int x = 0; x < texW; x++)
                {
                    int cx = x / pixelsPerCell;
                    int lx = x % pixelsPerCell;

                    bool isVertical   = (lx == 0);
                    bool isHorizontal = (ly == 0);

                    Color32 c;

                    if (isVertical || isHorizontal)
                    {
                        // pixel nằm trên đường grid
                        c = lc;
                    }
                    else
                    {
                        // nền trong suốt để nhìn thấy sand đằng sau
                        c = new Color32(0, 0, 0, 0);
                    }

                    colors[y * texW + x] = c;
                }
            }

            _tex.SetPixels32(colors);
            _tex.Apply();

            // pixelsPerUnit nhân với pixelsPerCell để cho world-size
            // của tấm grid == (width, height) đơn vị, giống Map
            float ppu = 100f * pixelsPerCell;

            var sprite = Sprite.Create(
                _tex,
                new Rect(0, 0, texW, texH),
                new Vector2(0.5f, 0.5f),
                ppu
            );

            _sr.sprite = sprite;
        }
    }
}
