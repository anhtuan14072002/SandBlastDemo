using Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class PictureCell : MonoBehaviour
    {
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _button;

        private Sprite _outlineSprite;
        private Sprite _colorSprite;
        private RenderPicture _renderPicture;
        public void Init(Sprite outlineSprite, Sprite colorSprite, RenderPicture renderPicture)
        {
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _renderPicture = renderPicture;

            if (_previewImage != null)
            {
                _previewImage.sprite = _outlineSprite;
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_renderPicture == null) return;     
            if (_outlineSprite == null || _colorSprite == null) return;
            Global.Send(new SignalClosePopupCollections());
            _renderPicture.RenderOutLineWithPair(_outlineSprite, _colorSprite);
        }
    }
}