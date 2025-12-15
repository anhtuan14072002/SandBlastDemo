using System.Collections.Generic;
using Core;
using Sand;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class CollectionItemUI : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Image _thumbnail;
    [SerializeField] private TMP_Text _colorCountText;
    [SerializeField] private Button _button;

    private ColoringBookRuntime _runtime;
    private int _index;
    
    public void Setup(int index, ColoringRegionsAsset asset, ColoringBookRuntime runtime)
    {
        _index = index;
        _runtime = runtime;

        if (_thumbnail != null && asset != null && asset.outlineTexture != null)
        {
            _thumbnail.sprite = Sprite.Create(
                asset.outlineTexture,
                new Rect(0, 0, asset.outlineTexture.width, asset.outlineTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        int colorCount = CountUniqueColors(asset);
        if (_colorCountText != null) _colorCountText.text = $"{colorCount}";
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        Global.Send(new SignalClosePopupCollections());
        Global.Send(new SignalTogglePopupDraw(){IsActive = true});
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
}