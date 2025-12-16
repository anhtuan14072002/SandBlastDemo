using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Sand;
using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteSpawner : MonoBehaviour
{
    [Header("Data")] [SerializeField] private ColoringRegionsAsset _asset;
    [Header("UI")] [SerializeField] private Transform _parent;
    [SerializeField] private Button _buttonPrefab;
    [Header("Painter")] [SerializeField] private RegionPainterFillByButton _painter;
    
    [Header("EffectBottle")]
    [SerializeField] private GameObject _bottlePrefab;

    [Header("Pool Settings")]
    [SerializeField] private int _poolSize = 3;

    [Header("Positions")]
    [SerializeField] private Transform _spawnPos;
    [SerializeField] private Transform _targetButton;
    [SerializeField] private Transform _endTargetButton;

    [Header("Settings")]
    [SerializeField] private float _moveDuration = 0.25f;
    [SerializeField] private float _returnDuration = 0.25f;
    [SerializeField] private float _delayBeforeReturn = 1f;
    [SerializeField] private float _pourAngle = 90f;
    [SerializeField] private float _bottleHeightOffset = 5f;
    [SerializeField] private float _bottleXOffset = 0.5f;
    [SerializeField] private float _buttonMoveToColorDuration = 0.5f;
    
    private GameObject _bottleInstance;
    private ColoringBookRuntime _runtime;
    private ColoringProgressDatabase _progressDb;
    
    private Dictionary<int, HashSet<int>> _colorToRegionMap = new Dictionary<int, HashSet<int>>();
    private List<Button> _spawnedButtons = new List<Button>();
    
    private void Start()
    {
        Vector3 scale = _bottlePrefab.transform.localScale;
        SpawnPool.InitPool(_bottlePrefab, _poolSize, scale);
        _progressDb = ColoringSave.LoadProgress();
    }
    
    public void SetRuntime(ColoringBookRuntime runtime)
    {
        _runtime = runtime;
    }
    public void SetAsset(ColoringRegionsAsset newAsset)
    {
        _asset = newAsset;
        Build();
    }

    public void Build()
    {
        if (_asset == null || _parent == null || _buttonPrefab == null || _painter == null) return;
        if (_asset.colorsCache == null || _asset.colorsCache.Count == 0)
            _asset.RebuildColorsCache();
        for (int i = _parent.childCount - 1; i >= 0; i--) Destroy(_parent.GetChild(i).gameObject);
        
        _spawnedButtons.Clear();
        _colorToRegionMap.Clear();
        
        //  kiểm tra ảnh hiện tại có dữ liệu hay k
        int currentPictureId = GetCurrentPictureIndex();
        ColoringProgressData savedProgress = _progressDb.GetProgress(currentPictureId);
        
        // Nếu có dữ liệu lưu thì đưa ra
        if (savedProgress != null && savedProgress.coloredRegions.Count > 0)
            ApplySavedProgress(savedProgress);

        // Spawn btn cho các màu chưa được tô
        foreach (var color in _asset.colorsCache)
        {
            if (IsColorFullyPainted(color)) continue;
            var btn = Instantiate(_buttonPrefab, _parent);
            _spawnedButtons.Add(btn);
            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = color;
            var btnTransform = btn.transform;
            Color32 captured = color;
            btn.onClick.AddListener(() =>
            {
                OnColorButtonClicked(btnTransform, captured);
            });
        }
    }
    
  
    private void OnColorButtonClicked(Transform btnTransform, Color32 color)
    {
        if (_bottleInstance != null)
        {
            SpawnPool.Despawn(_bottlePrefab, _bottleInstance);
            _bottleInstance = null;
        }
        Vector3 scale = _bottlePrefab.transform.localScale;
        _bottleInstance = SpawnPool.Spawn(_bottlePrefab, _spawnPos.position, Quaternion.identity, scale);
        Move(btnTransform, color).Forget();
    }

    private async UniTask Move(Transform buttonTransform, Color32 color)
    {
        if (_bottleInstance == null) return;
        Vector3 buttonPos = buttonTransform.position;
        Vector3 spawnPos = _spawnPos.position;
        
        Vector3 targetPos = buttonPos + Vector3.up * _bottleHeightOffset + new Vector3(_bottleXOffset, 0, 0);
        Tween.Position(_bottleInstance.transform, targetPos, _moveDuration, Ease.OutQuad);
        Tween.LocalRotation(_bottleInstance.transform, Quaternion.Euler(0, 0, _pourAngle), 0.2f);
        await UniTask.Delay(TimeSpan.FromSeconds(_moveDuration + _delayBeforeReturn));
       
        if (_bottleInstance == null) return;
        Tween.LocalRotation(_bottleInstance.transform, Quaternion.identity, 0.2f);
        Tween.Position(_bottleInstance.transform, spawnPos, _returnDuration, Ease.OutQuad);
        await UniTask.Delay(TimeSpan.FromSeconds(_returnDuration));
        
        if (_bottleInstance != null)
        {
            SpawnPool.Despawn(_bottlePrefab, _bottleInstance);
            _bottleInstance = null;
        }
        await MoveButtonToColoredRegion(buttonTransform, color);
    }

    private async UniTask MoveButtonToColoredRegion(Transform buttonTransform, Color32 color)
    {
        if (buttonTransform == null) return;
        if (_targetButton != null)
        {
            Tween.Position(buttonTransform, _targetButton.position, _buttonMoveToColorDuration, Ease.InQuad);
            Tween.LocalRotation(buttonTransform, Quaternion.Euler(0, 0, _pourAngle), 0.3f);
            await UniTask.Delay(TimeSpan.FromSeconds(_buttonMoveToColorDuration));
        }
        
        _painter.FillAllRegionsOfSampledColor(color);
        SaveColoredRegions(color);
        
        if (_endTargetButton != null && buttonTransform != null)
        {
            Tween.Position(buttonTransform, _endTargetButton.position, _buttonMoveToColorDuration, Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(_buttonMoveToColorDuration));
        }
        if (buttonTransform != null)
        {
            Tween.Scale(buttonTransform, Vector3.zero, 0.2f, Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            if (buttonTransform != null) buttonTransform.gameObject.SetActive(false);
        }
    }
    
    private void SaveColoredRegions(Color32 color)
    {
        if (_painter == null || _asset == null) return;

        int currentPictureIndex = GetCurrentPictureIndex();
        ColoringProgressData progress = _progressDb.GetProgress(currentPictureIndex);
        if (progress == null)
            progress = new ColoringProgressData { pictureIndex = currentPictureIndex };

        foreach (var region in _asset.regions)
        {
            if (region == null || !region.filled) continue;
            
            if (CloseColor(region.sampledColor, color, _asset.colorTolerance))
            {
                if (!progress.coloredRegions.Exists(cr => cr.regionId == region.id))
                {
                    progress.coloredRegions.Add(new ColoredRegionData
                    {
                        regionId = region.id,
                        appliedColor = color
                    });
                }
            }
        }
        
        _progressDb.SaveProgress(currentPictureIndex, progress);
        ColoringSave.SaveProgress(_progressDb);
    }

    private void ApplySavedProgress(ColoringProgressData progress)
    {
        if (_painter == null || _asset == null) return;
        foreach (var coloredRegion in progress.coloredRegions)
        {
            var region = _asset.GetRegion(coloredRegion.regionId);
            if (region == null) continue;
            region.filled = true;
            _painter.FillAllRegionsOfSampledColor(coloredRegion.appliedColor);
        }
    }

    private bool IsColorFullyPainted(Color32 color)
    {
        if (_asset == null || _asset.regions == null) return false;

        foreach (var region in _asset.regions)
        {
            if (region == null || region.maskBits == null || region.maskBits.Length == 0) 
                continue;
            
            if (CloseColor(region.sampledColor, color, _asset.colorTolerance) && !region.filled)
                return false;
        }
        return true;
    }
    
    private int GetCurrentPictureIndex()
    {
        if (_runtime == null) return -1;
        return _runtime.CurrentIndex;
    }

    private static bool CloseColor(Color32 a, Color32 b, float tol)
    {
        float dr = (a.r - b.r) / 255f;
        float dg = (a.g - b.g) / 255f;
        float db = (a.b - b.b) / 255f;
        return (dr * dr + dg * dg + db * db) <= (tol * tol);
    }

    public void ClearAllProgress()
    {
        ColoringSave.ClearAllProgress(_progressDb);
    }

    public void DespawnAllButtons()
    {
        for (int i = _parent.childCount - 1; i >= 0; i--)
        {
            var child = _parent.GetChild(i).gameObject;
            child.SetActive(false);
        }
    }
}