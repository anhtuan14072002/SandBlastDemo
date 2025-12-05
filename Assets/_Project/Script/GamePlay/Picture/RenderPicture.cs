using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
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

        EffectGame _effectGame;

        [Inject]
        void Construct(EffectGame effectGame)
        {
            _effectGame = effectGame;
        }

        private void Awake()
        {
            BuildRegionColorList();
        }

        private void Start()
        {
            _btnDraw.onClick.AddListener(FillColor);
        }

        #region Map Toggle

        public void OpenMapGamePlay()
        {
            if (_mapGamePlay != null)
            {
                _mapGamePlay.gameObject.SetActive(true);
            }
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

        #region Outline

        public void RenderOutLineWithPair(Sprite outlineSprite, Sprite colorSprite)
        {
            if (outlineSprite == null || colorSprite == null) return;

            _isCompleteShown = false;
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _currentColorIndex = 0;

            if (!_mapArt.gameObject.activeSelf)
                OpenMapArt();

            _progressBar.SetActive(true);
            BuildRegionColorList();
            RenderOutline();
            UpdateUI();
        }

        public void RenderOutline()
        {
            if (_outlineSprite == null || _mapArt == null) return;

            _currentColorIndex = 0;
            if (!_mapArt.gameObject.activeSelf)
                OpenMapArt();

            // GỌI JOB VẼ OUTLINE Ở FILE RIÊNG
            OutlineJobSystem.RenderOutline(
                _mapArt,
                _outlineSprite,
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
            if (_colorSprite == null) return;

            Texture2D texture = _colorSprite.texture;
            Rect rect = _colorSprite.textureRect;

            int spriteWidth = (int)rect.width;
            int spriteHeight = (int)rect.height;
            int startX = (int)rect.x;
            int startY = (int)rect.y;

            Color32[] pixels = texture.GetPixels32();
            int fullTextureWidth = texture.width;

            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    int pixelIndex = (startY + y) * fullTextureWidth + (startX + x);
                    if (pixelIndex < 0 || pixelIndex >= pixels.Length)
                        continue;

                    Color32 c = pixels[pixelIndex];
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

            // GỌI JOB TÔ MÀU Ở FILE RIÊNG
            FillRegionJobSystem.FillRegion(
                _mapArt,
                _colorSprite,
                currentColor,
                _colorTolerance,
                _jobBatchSize
            );

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
}

/*using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

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
        [SerializeField] private Color _sandLineColor = new(0.16f, 0.16f, 0.18f, 1f); // cát cho viền
        [SerializeField, Range(0f, 1f)] private float _lineLuminanceThreshold = 0.35f; // ngưỡng line tối
        [SerializeField, Range(0f, 1f)] private float _sandLineDensity = 0.9f; // mật độ cát trên viền

        [SerializeField, Range(0f, 1f)] private float _fillSandDensity = 0.9f; // mật độ cát khi tô màu (dùng khi không full)
        [SerializeField, Range(0, 255)] private int _colorTolerance = 40; // tolerance so màu vùng
        
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
        private float _currentValue = 0f;
        private float _targetValue = 0f;
        private bool _isCompleteShown = false;

        EffectGame _effectGame;

        [Inject]
        void Construct(EffectGame effectGame)
        {
            _effectGame = effectGame;
        }
        
        private void Awake()
        {
            BuildRegionColorList();
        }

        private void Start()
        {
            // RenderOutline();
            _btnDraw.onClick.AddListener(FillColor);
        }
        #region Map Toggle

        public void OpenMapGamePlay()
        {
            if (_mapGamePlay != null)
            {
                _mapGamePlay.gameObject.SetActive(true);
            }
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
            /*_mapArt._map.SetUpMap((Color32)_artBackgroundColor);
            _mapArt._map.UpdateTexture();
            LogRegionColors();#1#
        }

        public void CloseMapArt()
        {
            if (_mapArt == null) return;
            _mapArt._spriteRenderer.sortingOrder = 0;
            _mapArt.gameObject.SetActive(false);
        }

        #endregion

        #region Outline

        public void RenderOutLineWithPair(Sprite outlineSprite, Sprite colorSprite)
        {
            if (outlineSprite == null || colorSprite == null) return;
            _isCompleteShown = false; 
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _currentColorIndex = 0;
            if (!_mapArt.gameObject.activeSelf) OpenMapArt();
            _progressBar.SetActive(true);
            BuildRegionColorList();
            RenderOutline();
            UpdateUI();
            // LogRegionColors();
        }

        public void RenderOutline()
        {
            if (_outlineSprite == null || _mapArt == null) return;
            _currentColorIndex = 0;
            if (!_mapArt.gameObject.activeSelf) OpenMapArt();
            RenderImageToMapArt(_outlineSprite);
            UpdateUI();
        }

        private void RenderImageToMapArt(Sprite sprite)
        {
            if (sprite == null || _mapArt == null) return;

            _mapArt._map.SetUpMap(_artBackgroundColor);

            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;

            int spriteWidth = (int)spriteRect.width;
            int spriteHeight = (int)spriteRect.height;
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;

            int mapWidth = _mapArt._wight;
            int mapHeight = _mapArt._hight;

            float scaleX = (float)mapWidth / spriteWidth;
            float scaleY = (float)mapHeight / spriteHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            float drawWidth = spriteWidth * scale;
            float drawHeight = spriteHeight * scale;
            float offsetX = (mapWidth - drawWidth) * 0.5f;
            float offsetY = (mapHeight - drawHeight) * 0.5f;

            Color32[] pixels = texture.GetPixels32();
            int fullTextureWidth = texture.width;

            Color32 sandColor32 = _sandLineColor;

            for (int mapY = 0; mapY < mapHeight; mapY++)
            {
                for (int mapX = 0; mapX < mapWidth; mapX++)
                {
                    if (mapX < offsetX || mapX >= offsetX + drawWidth ||
                        mapY < offsetY || mapY >= offsetY + drawHeight)
                        continue;

                    int localX = (int)((mapX - offsetX) / scale);
                    int localY = (int)((mapY - offsetY) / scale);

                    if (localX < 0 || localX >= spriteWidth ||
                        localY < 0 || localY >= spriteHeight)
                        continue;

                    int pixelIndex = (startY + localY) * fullTextureWidth + (startX + localX);
                    if (pixelIndex < 0 || pixelIndex >= pixels.Length)
                        continue;

                    Color32 pixelColor = pixels[pixelIndex];

                    if (pixelColor.a < 10)
                        continue;

                    float luminance =
                        (0.2126f * pixelColor.r +
                         0.7152f * pixelColor.g +
                         0.0722f * pixelColor.b) / 255f;

                    if (luminance < _lineLuminanceThreshold)
                    {
                        if (Random.value <= _sandLineDensity)
                        {
                            _mapArt._map.SetPixelCell(mapX, mapY, sandColor32);
                        }
                    }
                }
            }

            _mapArt._map.UpdateTexture();
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
            if (_colorSprite == null) return;

            Texture2D texture = _colorSprite.texture;
            Rect rect = _colorSprite.textureRect;

            int spriteWidth = (int)rect.width;
            int spriteHeight = (int)rect.height;
            int startX = (int)rect.x;
            int startY = (int)rect.y;

            Color32[] pixels = texture.GetPixels32();
            int fullTextureWidth = texture.width;

            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    int pixelIndex = (startY + y) * fullTextureWidth + (startX + x);
                    if (pixelIndex < 0 || pixelIndex >= pixels.Length)
                        continue;

                    Color32 c = pixels[pixelIndex];
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
            FillRegionColorFull(currentColor);
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

        
        private void FillRegionColorFull(Color32 targetColor)
        {
            if (_mapArt == null || _colorSprite == null) return;

            Texture2D texture = _colorSprite.texture;
            Rect spriteRect = _colorSprite.textureRect;

            int spriteWidth = (int)spriteRect.width;
            int spriteHeight = (int)spriteRect.height;
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;

            int mapWidth = _mapArt._wight;
            int mapHeight = _mapArt._hight;

            float scaleX = (float)mapWidth / spriteWidth;
            float scaleY = (float)mapHeight / spriteHeight;
            float scale = Mathf.Min(scaleX, scaleY);

            float drawWidth = spriteWidth * scale;
            float drawHeight = spriteHeight * scale;
            float offsetX = (mapWidth - drawWidth) * 0.5f;
            float offsetY = (mapHeight - drawHeight) * 0.5f;

            Color32[] pixels = texture.GetPixels32();
            int fullTextureWidth = texture.width;
            Color32 sandColor32 = _sandLineColor;
            for (int mapY = 0; mapY < mapHeight; mapY++)
            {
                for (int mapX = 0; mapX < mapWidth; mapX++)
                {
                    if (mapX < offsetX || mapX >= offsetX + drawWidth ||
                        mapY < offsetY || mapY >= offsetY + drawHeight)
                        continue;

                    int localX = (int)((mapX - offsetX) / scale);
                    int localY = (int)((mapY - offsetY) / scale);

                    if (localX < 0 || localX >= spriteWidth || localY < 0 || localY >= spriteHeight) continue;
                    int pixelIndex = (startY + localY) * fullTextureWidth + (startX + localX);
                    if (pixelIndex < 0 || pixelIndex >= pixels.Length) continue;
                    Color32 pixelColor = pixels[pixelIndex];
                    if (pixelColor.a < 10) continue;
                    // if (IsColorClose(pixelColor, sandColor32, _colorTolerance)) continue;
                    if (!IsColorClose(pixelColor, targetColor, _colorTolerance)) continue;
                    // if (_mapArt._map.HasValue(mapX, mapY)) continue;
                    _mapArt._map.SetPixelCell(mapX, mapY, pixelColor);
                }
            }
            _mapArt._map.UpdateTexture();
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