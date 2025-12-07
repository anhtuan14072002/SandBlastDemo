using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class PictureCell : MonoBehaviour
    {
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _button;

        public int _index;
        private Sprite _outlineSprite;
        private Sprite _colorSprite;
        private RenderPicture _renderPicture;
        private bool _isCompleted = false;

        public void Init(Sprite outlineSprite, Sprite colorSprite, RenderPicture renderPicture, int index)
        {
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _renderPicture = renderPicture;
            _index = index;

            if (_previewImage != null)
                _previewImage.sprite = _outlineSprite;

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
            
            if (_isCompleted)
            {
                Debug.Log("Tranh số " + _index + "vẽ xong");
                return;
            }
            Global.Send(new SignalClosePopupCollections());
            _renderPicture.RenderOutLineWithPair(_outlineSprite, _colorSprite, _index);
        }
        public void ResetCell()
        {
            _isCompleted = false;
            if (_previewImage != null)
                _previewImage.sprite = _outlineSprite;
        }

        public void SetPreviewSprite(Sprite sprite)
        {
            if (_previewImage != null)
                _previewImage.sprite = sprite;
            _isCompleted = true;
        }
    }
}