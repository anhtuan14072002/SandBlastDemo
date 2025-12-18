using System.Collections.Generic;
using Core;
using Sand;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class CollectionItemUI : GameElement,
    IReceive<SignalUpdateSoldIcon>
{
    [Header("UI")] [SerializeField] private TextMeshProUGUI _colorCountText;
    [SerializeField] private Button _button;
    [SerializeField] private Image _thumbnail;
    [SerializeField] private Image _iconSold;
    private ColoringBookRuntime _runtime;
    private ColoringRegionsAsset _asset;
    private int _index;
    public int Index => _index;
    private bool _isSold = false;

    private UserData _userData;

    [Inject]
    void Construct(UserData userData)
    {
        _userData = userData;
    }

    public void Setup(int index, ColoringRegionsAsset asset, ColoringBookRuntime runtime)
    {
        _index = index;
        _runtime = runtime;
        _asset = asset;

        if (_thumbnail != null && asset != null && asset.outlineTexture != null)
        {
            _thumbnail.sprite = Sprite.Create(asset.outlineTexture,
                new Rect(0, 0, asset.outlineTexture.width, asset.outlineTexture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        int colorCount = CountUniqueColors(asset);
        if (_colorCountText != null) _colorCountText.text = $"{colorCount}";
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }

        UpdateSoldIcon();
    }

    public void UpdateSoldIcon()
    {
        if (_iconSold == null) return;

        bool isSold = false;
        if (_userData != null && _userData.PictureIsSoldState != null)
        {
            bool val;
            if (_userData.PictureIsSoldState.TryGetValue(_index, out val))
                isSold = val;
        }

        _iconSold.gameObject.SetActive(isSold);
        _isSold = isSold;
    }

    public void UpdateThumbnailCompleted(int index)
    { 
        if (_thumbnail == null || _asset == null) return;
        if (index != _index) return;
        _thumbnail.sprite = Sprite.Create(_asset.sourceTexture,
            new Rect(0, 0, _asset.sourceTexture.width, _asset.sourceTexture.height), new Vector2(0.5f, 0.5f), 100f);
        Debug.Log("UpdateThumbnailCompleted");
    }

    private void OnClick()
    {
        if (_isSold) return;
        Global.Send(new SignalClosePopupCollections());
        Global.Send(new SignalTogglePopupDraw() { IsActive = true });
        _runtime.SetIndex(_index);
    }

    private int CountUniqueColors(ColoringRegionsAsset asset)
    {
        if (asset == null || asset.regions == null) return 0;

        var set = new HashSet<int>();
        foreach (var r in asset.regions)
        {
            if (r == null) continue;
            int key = (r.sampledColor.r << 16) | (r.sampledColor.g << 8) | r.sampledColor.b;
            set.Add(key);
        }

        return set.Count;
    }

    public void Receive(in SignalUpdateSoldIcon signal)
    {
        if (signal.PictureIndex == _index)
            UpdateSoldIcon();
    }
}