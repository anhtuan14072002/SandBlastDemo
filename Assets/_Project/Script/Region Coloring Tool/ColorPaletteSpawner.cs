using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
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
    
    private void Start()
    {
        // Build();
        Vector3 scale = _bottlePrefab.transform.localScale;
        SpawnPool.InitPool(_bottlePrefab, _poolSize, scale);
    }
    
    public void Build()
    {
        if (_asset == null || _parent == null || _buttonPrefab == null || _painter == null) return;
        if (_asset.colorsCache == null || _asset.colorsCache.Count == 0)
            _asset.RebuildColorsCache();
        for (int i = _parent.childCount - 1; i >= 0; i--)
            Destroy(_parent.GetChild(i).gameObject);
        foreach (var c in _asset.colorsCache)
        {
            var btn = Instantiate(_buttonPrefab, _parent);

            var img = btn.GetComponentInChildren<Image>();
            if (img != null) img.color = c;
            var btnTransform = btn.transform;
            Color32 captured = c;
            btn.onClick.AddListener(() =>
            {
                RunEffect(btnTransform, captured);
            });
        }
    }
    
    public void SetAsset(ColoringRegionsAsset newAsset)
    {
        _asset = newAsset;
        Build();
    }

    private void RunEffect(Transform buttonTransform, Color32 color)
    {
        if (_bottleInstance != null)
        {
            SpawnPool.Despawn(_bottlePrefab, _bottleInstance);
            _bottleInstance = null;
        }
        Vector3 scale = _bottlePrefab.transform.localScale;
        _bottleInstance = SpawnPool.Spawn(_bottlePrefab, _spawnPos.position, Quaternion.identity, scale);
        Move(buttonTransform, color).Forget();
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
        if (_endTargetButton != null && buttonTransform != null)
        {
            Tween.Position(buttonTransform, _endTargetButton.position, _buttonMoveToColorDuration, Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(_buttonMoveToColorDuration));
        }
        if (buttonTransform != null)
        {
            Tween.Scale(buttonTransform, Vector3.zero, 0.2f, Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            if (buttonTransform != null)
                buttonTransform.gameObject.SetActive(false);
        }
    }
}