using System.Collections.Generic;
using System.Threading;
using Core;
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
        [Header("Maps")] [SerializeField] private RenderMap _mapGamePlay;
        [SerializeField] private RenderMap _mapArt;

        [Header("Sprites")] private Sprite _outlineSprite;
        private Sprite _colorSprite;

        [Header("Buttons")] [SerializeField] private Button _btnDraw;

        [Header("Art Settings")] [SerializeField]
        private Color _artBackgroundColor = new(0.75f, 0.75f, 0.8f, 1f); // nền xám

        [SerializeField] private Color _sandLineColor = new(0.16f, 0.16f, 0.18f, 1f); // cát cho viền
        [SerializeField, Range(0f, 1f)] private float _lineLuminanceThreshold = 0.35f; // ngưỡng line tối
        [SerializeField, Range(0f, 1f)] private float _sandLineDensity = 0.9f; // mật độ cát trên viền

        [SerializeField, Range(0f, 1f)]
        private float _fillSandDensity = 0.9f; // mật độ cát khi tô màu (dùng khi không full)

        [SerializeField, Range(0, 255)] private int _colorTolerance = 40; // tolerance so màu vùng

        [Header("Progress")] 
        [SerializeField] private TextMeshProUGUI _textCountDraw;
        [SerializeField] private TextMeshProUGUI _textPercent;
        [SerializeField] private GameObject _popupDrawComplete;
        [SerializeField] private GameObject _progressBar;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _imageComplete;
        [SerializeField] private float _fillAnimationDuration = 0.5f;

        private readonly List<Color32> _regionColors = new();
        private int _currentColorIndex = 0;
        private bool _isCompleteShown = false;

        private int _currentPictureIndex = -1;

        EffectGame _effectGame;
        PictureDrawData _pictureDrawData;
        SaveService _saveService;
        CountDrawData _countDrawData;
        UserData _userData;
        PopupAuction _popupAuction;

        [SerializeField] private PictureBase _pictureBase;

        [Inject]
        void Construct(EffectGame effectGame, PictureDrawData pictureDrawData, SaveService saveService,
            CountDrawData countDrawData, UserData userData, PopupAuction popupAuction)
        {
            _effectGame = effectGame;
            _pictureDrawData = pictureDrawData;
            _saveService = saveService;
            _countDrawData = countDrawData;
            _userData = userData;
            _popupAuction = popupAuction;
        }

        private void Awake()
        {
            BuildRegionColorList();
        }

        private void Start()
        {
            if (_btnDraw != null)
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

        public void RenderOutLineWithPair(Sprite outlineSprite, Sprite colorSprite, int pictureIndex)
        {
            if (outlineSprite == null || colorSprite == null) return;
            // _popupAuction.CloseLock();
            _isCompleteShown = false;
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _currentPictureIndex = pictureIndex;

            _currentColorIndex = 0;

            if (!_mapArt.gameObject.activeSelf) OpenMapArt();
            _progressBar.SetActive(true);
            BuildRegionColorList();
            RenderImageToMapArt(_outlineSprite);

            int totalRegions = _regionColors.Count;
            int savedFilled = _pictureDrawData.GetFilledRegionCount(_currentPictureIndex, totalRegions);
            savedFilled = Mathf.Clamp(savedFilled, 0, totalRegions);

            if (savedFilled > 0)
            {
                for (int i = 0; i < savedFilled; i++)
                {
                    var c = _regionColors[i];
                    FillRegionColorFull(c);
                }
            }

            _currentColorIndex = savedFilled;
            UpdateUI();
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
            int total = _regionColors.Count;
            float fillValue = (total > 0) ? (float)_currentColorIndex / total : 0f;

            if (_fillImage != null)
                _fillImage.fillAmount = fillValue;

            if (_textCountDraw != null)
                _textCountDraw.text = $"{_currentColorIndex}/{total}";

            if (_textPercent != null)
                _textPercent.text = $"{(int)(fillValue * 100)}%";
        }

        private void FillColor()
        {
            if (_regionColors.Count == 0) return;
            if (_currentPictureIndex < 0) return;

            /*if (_userData.CountDrawPicture.Value <= 0)
            {
                Debug.Log("Hết lượt vẽ (CountDrawPicture = 0)");
                return;
            }*/

            // _countDrawData.DecreaseCountDrawPicture(1);

            if (_currentColorIndex >= _regionColors.Count)
            {
                ShowComplete();
                return;
            }
            Global.Send(new SignalMoveBottleDraw());
            Color32 currentColor = _regionColors[_currentColorIndex];
            FillRegionColorFull(currentColor);

            _currentColorIndex++;

            if (_textCountDraw != null)
                _textCountDraw.text = $"{_currentColorIndex}/{_regionColors.Count}";

            _pictureDrawData.UpdateFillProgress(_currentPictureIndex, _currentColorIndex, _regionColors.Count);
            AnimateFillBarAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask AnimateFillBarAsync(CancellationToken cancellationToken)
        {
            float fillTarget = (_regionColors.Count > 0)
                ? (float)_currentColorIndex / _regionColors.Count
                : 0f;

            float startFill = _fillImage != null ? _fillImage.fillAmount : 0f;

            await UniTask.DelayFrame(0, cancellationToken: cancellationToken);

            float elapsedTime = 0f;
            while (elapsedTime < _fillAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _fillAnimationDuration);
                float fillValue = Mathf.Lerp(startFill, fillTarget, progress);

                if (_fillImage != null)
                    _fillImage.fillAmount = fillValue;
                if (_textPercent != null)
                    _textPercent.text = $"{(int)(fillValue * 100)}%";

                await UniTask.Yield(cancellationToken);
            }

            if (_fillImage != null)
                _fillImage.fillAmount = fillTarget;
            if (_textPercent != null)
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
                    if (!IsColorClose(pixelColor, targetColor, _colorTolerance)) continue;

                    _mapArt._map.SetPixelCell(mapX, mapY, pixelColor);
                }
            }

            _mapArt._map.UpdateTexture();
        }

        public void ShowComplete()
        {
            if (_isCompleteShown) return;
            _isCompleteShown = true;
            _pictureDrawData.UpdatePictureCollections(_currentPictureIndex);
            CloseMapArt();
            _popupDrawComplete.SetActive(true);
            _effectGame.OpenEffectLevelUp();
            _imageComplete.sprite = _colorSprite;
            // _popupAuction.OpenAuction();
            _popupAuction.OpenLock(); 
        }

        public void HideComplete()
        {
            if (_pictureBase != null && _currentPictureIndex >= 0 && _colorSprite != null)
                _pictureBase.UpdatePictureSprite(_currentPictureIndex, _colorSprite);
            OpenMapArt();
            _effectGame.CloseEffectLevelUp();
            _popupDrawComplete.SetActive(false);
            _effectGame.OpenEffectTime().Forget();
        }

        public void Reset()
        {
            _pictureDrawData.ResetPictureCollections();
            if (_pictureBase != null) _pictureBase.ResetAllPictures();
        }

        #endregion
    }
}