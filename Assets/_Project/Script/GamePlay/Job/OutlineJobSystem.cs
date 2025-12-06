/*using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sand
{
    public static class OutlineRenderJobs
    {
        /// <summary>
        /// Dùng NativeArray pixel đã cache cho outline sprite.
        /// </summary>
        public static void RenderOutline(
            RenderMap mapArt,
            NativeArray<Color32> spritePixels,
            int fullTextureWidth,
            int spriteStartX,
            int spriteStartY,
            int spriteWidth,
            int spriteHeight,
            Color artBackgroundColor,
            Color sandLineColor,
            float lineLuminanceThreshold,
            float sandLineDensity,
            int batchSize
        )
        {
            if (mapArt == null || !spritePixels.IsCreated) return;

            int mapWidth = mapArt._wight;
            int mapHeight = mapArt._hight;
            int totalMapPixels = mapWidth * mapHeight;

            float scaleX = (float)mapWidth / spriteWidth;
            float scaleY = (float)mapHeight / spriteHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            float drawWidth = spriteWidth * scale;
            float drawHeight = spriteHeight * scale;
            float offsetX = (mapWidth - drawWidth) * 0.5f;
            float offsetY = (mapHeight - drawHeight) * 0.5f;

            var mapPixels = new NativeArray<Color32>(totalMapPixels, Allocator.TempJob);

            // Clear nền
            var clearJob = new ClearMapJob
            {
                MapPixels = mapPixels,
                BackgroundColor = (Color32)artBackgroundColor
            };
            JobHandle clearHandle = clearJob.Schedule(totalMapPixels, batchSize);

            // Job vẽ outline
            var outlineJob = new OutlineJob
            {
                MapPixels = mapPixels,
                MapWidth = mapWidth,
                MapHeight = mapHeight,

                SpritePixels = spritePixels,
                FullTextureWidth = fullTextureWidth,
                SpriteStartX = spriteStartX,
                SpriteStartY = spriteStartY,
                SpriteWidth = spriteWidth,
                SpriteHeight = spriteHeight,

                Scale = scale,
                OffsetX = offsetX,
                OffsetY = offsetY,

                LuminanceThreshold = lineLuminanceThreshold,
                SandLineDensity = sandLineDensity,
                SandColor = (Color32)sandLineColor
            };

            JobHandle outlineHandle = outlineJob.Schedule(totalMapPixels, batchSize, clearHandle);
            outlineHandle.Complete();

            // Apply sang map thực
            for (int i = 0; i < totalMapPixels; i++)
            {
                int y = i / mapWidth;
                int x = i % mapWidth;
                mapArt._map.SetPixelCell(x, y, mapPixels[i]);
            }

            mapArt._map.UpdateTexture();
            mapPixels.Dispose();
        }

        // ================= JOB STRUCTS =================

        [BurstCompile]
        private struct ClearMapJob : IJobParallelFor
        {
            public NativeArray<Color32> MapPixels;
            public Color32 BackgroundColor;

            public void Execute(int index)
            {
                MapPixels[index] = BackgroundColor;
            }
        }

        [BurstCompile]
        private struct OutlineJob : IJobParallelFor
        {
            public NativeArray<Color32> MapPixels;
            public int MapWidth;
            public int MapHeight;

            [ReadOnly] public NativeArray<Color32> SpritePixels;
            public int FullTextureWidth;
            public int SpriteStartX;
            public int SpriteStartY;
            public int SpriteWidth;
            public int SpriteHeight;

            public float Scale;
            public float OffsetX;
            public float OffsetY;

            public float LuminanceThreshold;
            public float SandLineDensity;
            public Color32 SandColor;

            public void Execute(int index)
            {
                int mapY = index / MapWidth;
                int mapX = index % MapWidth;

                float drawWidth = SpriteWidth * Scale;
                float drawHeight = SpriteHeight * Scale;

                if (mapX < OffsetX || mapX >= OffsetX + drawWidth ||
                    mapY < OffsetY || mapY >= OffsetY + drawHeight)
                    return;

                int localX = (int)((mapX - OffsetX) / Scale);
                int localY = (int)((mapY - OffsetY) / Scale);

                if (localX < 0 || localX >= SpriteWidth ||
                    localY < 0 || localY >= SpriteHeight)
                    return;

                int pixelIndex = (SpriteStartY + localY) * FullTextureWidth + (SpriteStartX + localX);
                if (pixelIndex < 0 || pixelIndex >= SpritePixels.Length)
                    return;

                Color32 pixelColor = SpritePixels[pixelIndex];
                if (pixelColor.a < 10)
                    return;

                float luminance =
                    (0.2126f * pixelColor.r +
                     0.7152f * pixelColor.g +
                     0.0722f * pixelColor.b) / 255f;

                if (luminance >= LuminanceThreshold)
                    return;

                float rnd = HashTo01(mapX, mapY);
                if (rnd > SandLineDensity)
                    return;

                MapPixels[index] = SandColor;
            }

            private static float HashTo01(int x, int y)
            {
                uint hash = math.hash(new int2(x, y));
                uint v = hash & 0x00FFFFFFu;
                return v / 16777215f; // 2^24 - 1
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class RenderPicture : MonoBehaviour
    {
        [Header("Maps")]
        [SerializeField] private RenderMap _mapGamePlay;
        [SerializeField] private RenderMap _mapArt;

        [Header("Sprites")]
        private Sprite _outlineSprite;
        private Sprite _colorSprite;

        [Header("Buttons")]
        [SerializeField] private Button _btnDraw;

        [Header("Art Settings")]
        [SerializeField] private Color _artBackgroundColor = new(0.75f, 0.75f, 0.8f, 1f); // nền xám
        [SerializeField] private Color _sandLineColor = new(0.16f, 0.16f, 0.18f, 1f);      // cát cho viền
        [SerializeField, Range(0f, 1f)] private float _lineLuminanceThreshold = 0.35f;      // ngưỡng line tối
        [SerializeField, Range(0f, 1f)] private float _sandLineDensity = 0.9f;              // mật độ cát trên viền

        [SerializeField, Range(0f, 1f)] private float _fillSandDensity = 0.9f;              // (chưa dùng)
        [SerializeField, Range(0, 255)] private int _colorTolerance = 40;                   // tolerance so màu vùng

        [Header("Jobs")]
        [SerializeField] private int _jobBatchSize = 64; // batch size cho IJobParallelFor

        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI _textCountDraw;
        [SerializeField] private TextMeshProUGUI _textPercent;
        [SerializeField] private Image _fillImage;
        [SerializeField] private float _fillAnimationDuration = 0.5f;
        [SerializeField] private GameObject _progressBar;
        [SerializeField] private GameObject _popupDrawComplete;
        [SerializeField] private Image _imageComplete;

        private readonly List<Color32> _regionColors = new();
        private int _currentColorIndex = 0;
        private bool _isCompleteShown = false;

        // ==== CACHE PIXEL ====
        private NativeArray<Color32> _outlinePixels; // Persistent
        private NativeArray<Color32> _colorPixels;   // Persistent

        private Rect _outlineRect;
        private int _outlineFullTextureWidth;

        private Rect _colorRect;
        private int _colorFullTextureWidth;

        EffectGame _effectGame;

        [Inject]
        void Construct(EffectGame effectGame)
        {
            _effectGame = effectGame;
        }

        private void Awake()
        {
            // ban đầu chưa có sprite thì chưa build list
            // BuildRegionColorList() sẽ được gọi sau khi set pair
        }

        private void Start()
        {
            _btnDraw.onClick.AddListener(FillColor);
        }

        private void OnDestroy()
        {
            if (_outlinePixels.IsCreated)
                _outlinePixels.Dispose();

            if (_colorPixels.IsCreated)
                _colorPixels.Dispose();
        }

        #region Map Toggle

        public void OpenMapGamePlay()
        {
            if (_mapGamePlay != null)
                _mapGamePlay.gameObject.SetActive(true);
        }

        public void CloseMapGamePlay()
        {
            if (_mapGamePlay != null)
                _mapGamePlay.gameObject.SetActive(false);
        }

        public void OpenMapArt()
        {
            if (_mapArt == null) return;
            _mapArt.gameObject.SetActive(true);
            _mapArt._spriteRenderer.sortingOrder = 99;
        }

        public void CloseMapArt()
        {
            if (_mapArt == null) return;
            _mapArt._spriteRenderer.sortingOrder = 0;
            _mapArt.gameObject.SetActive(false);
        }

        #endregion

        #region Cache Sprites

        private void CacheOutlineSprite(Sprite sprite)
        {
            if (_outlinePixels.IsCreated)
                _outlinePixels.Dispose();

            _outlineSprite = sprite;
            if (_outlineSprite == null) return;

            Texture2D tex = _outlineSprite.texture;
            Rect rect = _outlineSprite.textureRect;

            Color32[] pixels = tex.GetPixels32(); // chỉ gọi 1 lần khi đổi sprite
            _outlinePixels = new NativeArray<Color32>(pixels, Allocator.Persistent);

            _outlineRect = rect;
            _outlineFullTextureWidth = tex.width;
        }

        private void CacheColorSprite(Sprite sprite)
        {
            if (_colorPixels.IsCreated)
                _colorPixels.Dispose();

            _colorSprite = sprite;
            if (_colorSprite == null) return;

            Texture2D tex = _colorSprite.texture;
            Rect rect = _colorSprite.textureRect;

            Color32[] pixels = tex.GetPixels32(); // chỉ gọi 1 lần khi đổi sprite
            _colorPixels = new NativeArray<Color32>(pixels, Allocator.Persistent);

            _colorRect = rect;
            _colorFullTextureWidth = tex.width;
        }

        #endregion

        #region Outline

        public void RenderOutLineWithPair(Sprite outlineSprite, Sprite colorSprite)
        {
            if (outlineSprite == null || colorSprite == null) return;

            _isCompleteShown = false;
            _currentColorIndex = 0;

            // Cache pixel cho 2 sprite (GetPixels32 chỉ chạy đúng ở đây)
            CacheOutlineSprite(outlineSprite);
            CacheColorSprite(colorSprite);

            if (!_mapArt.gameObject.activeSelf)
                OpenMapArt();

            _progressBar.SetActive(true);

            BuildRegionColorList();
            RenderOutline();
            UpdateUI();
        }

        public void RenderOutline()
        {
            if (_mapArt == null) return;
            if (!_outlinePixels.IsCreated) return;

            _currentColorIndex = 0;
            if (!_mapArt.gameObject.activeSelf)
                OpenMapArt();

            int spriteWidth = (int)_outlineRect.width;
            int spriteHeight = (int)_outlineRect.height;
            int startX = (int)_outlineRect.x;
            int startY = (int)_outlineRect.y;

            OutlineRenderJobs.RenderOutline(
                _mapArt,
                _outlinePixels,
                _outlineFullTextureWidth,
                startX,
                startY,
                spriteWidth,
                spriteHeight,
                _artBackgroundColor,
                _sandLineColor,
                _lineLuminanceThreshold,
                _sandLineDensity,
                _jobBatchSize
            );

            UpdateUI();
        }

        #endregion

        #region Fill Color Regions

        private bool IsColorClose(Color32 a, Color32 b, int tolerance)
        {
            int dr = Mathf.Abs(a.r - b.r);
            int dg = Mathf.Abs(a.g - b.g);
            int db = Mathf.Abs(a.b - b.b);
            return (dr + dg + db) <= tolerance;
        }

        private void BuildRegionColorList()
        {
            _regionColors.Clear();
            if (!_colorPixels.IsCreated) return;

            int spriteWidth = (int)_colorRect.width;
            int spriteHeight = (int)_colorRect.height;
            int startX = (int)_colorRect.x;
            int startY = (int)_colorRect.y;

            int fullTextureWidth = _colorFullTextureWidth;

            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    int pixelIndex = (startY + y) * fullTextureWidth + (startX + x);
                    if (pixelIndex < 0 || pixelIndex >= _colorPixels.Length)
                        continue;

                    Color32 c = _colorPixels[pixelIndex];
                    if (c.a < 10) continue;

                    bool exists = false;
                    for (int i = 0; i < _regionColors.Count; i++)
                    {
                        if (IsColorClose(_regionColors[i], c, _colorTolerance))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists) _regionColors.Add(c);
                }
            }
        }

        private void UpdateUI()
        {
            _fillImage.fillAmount = 0f;
            _textCountDraw.text = $"{_currentColorIndex}/{_regionColors.Count}";
            _textPercent.text = "0%";
        }

        private void FillColor()
        {
            if (_regionColors.Count == 0) return;

            if (_currentColorIndex >= _regionColors.Count)
            {
                Debug.Log("tô xong");
                ShowComplete();
                return;
            }

            Color32 currentColor = _regionColors[_currentColorIndex];

            if (_colorPixels.IsCreated)
            {
                int spriteWidth = (int)_colorRect.width;
                int spriteHeight = (int)_colorRect.height;
                int startX = (int)_colorRect.x;
                int startY = (int)_colorRect.y;

                FillColorRenderJobs.FillRegionColorFull(
                    _mapArt,
                    _colorPixels,
                    _colorFullTextureWidth,
                    startX,
                    startY,
                    spriteWidth,
                    spriteHeight,
                    currentColor,
                    _colorTolerance,
                    _jobBatchSize
                );
            }

            _currentColorIndex++;
            _textCountDraw.text = $"{_currentColorIndex}/{_regionColors.Count}";
            AnimateFillBarAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask AnimateFillBarAsync(CancellationToken cancellationToken)
        {
            float fillTarget = (float)_currentColorIndex / _regionColors.Count;
            float startFill = _fillImage.fillAmount;

            await UniTask.DelayFrame(0, cancellationToken: cancellationToken);

            float elapsedTime = 0f;
            while (elapsedTime < _fillAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _fillAnimationDuration);
                float fillValue = Mathf.Lerp(startFill, fillTarget, progress);
                _fillImage.fillAmount = fillValue;
                _textPercent.text = $"{(int)(fillValue * 100)}%";
                await UniTask.Yield(cancellationToken);
            }

            _fillImage.fillAmount = fillTarget;
            _textPercent.text = $"{(int)(fillTarget * 100)}%";
        }

        public void ShowComplete()
        {
            if (_isCompleteShown) return;

            _isCompleteShown = true;
            CloseMapArt();
            _popupDrawComplete.SetActive(true);
            _effectGame.OpenEffectLevelUp();
            _imageComplete.sprite = _colorSprite;
        }

        public void HideComplete()
        {
            OpenMapArt();
            _effectGame.CloseEffectLevelUp();
            _popupDrawComplete.SetActive(false);
        }

        private void LogRegionColors()
        {
            for (int i = 0; i < _regionColors.Count; i++)
            {
                Color32 color = _regionColors[i];
                Debug.Log($"[{i}] RGBA: ({color.r}, {color.g}, {color.b}, {color.a})");
            }
        }

        #endregion
    }
}*/