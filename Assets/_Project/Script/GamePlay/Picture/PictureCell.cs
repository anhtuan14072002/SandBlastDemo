using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class PictureCell : MonoBehaviour
    {
        [SerializeField] private Image _previewImage;
        [SerializeField] private Image _iconSold;
        [SerializeField] private Button _button;

        public int _index;
        private Sprite _outlineSprite;
        private Sprite _colorSprite;
        private RenderPicture _renderPicture;
        private bool _isCompleted = false;
        private bool _isSold = false;

        private UserData _userData;
        
        public void Init(Sprite outlineSprite, Sprite colorSprite, RenderPicture renderPicture, int index, UserData userData)
        {
            _outlineSprite = outlineSprite;
            _colorSprite = colorSprite;
            _renderPicture = renderPicture;
            _index = index;
            _userData = userData;

            if (_previewImage != null)
                _previewImage.sprite = _outlineSprite;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }

            ApplySoldStateFromUserData();
        }

        private void OnEnable()
        {
            ApplySoldStateFromUserData();
        }

        private void ApplySoldStateFromUserData()
        {
            _isSold = false;
            if (_userData != null && _userData.SoldPictureIndices != null)
                _isSold = _userData.SoldPictureIndices.Contains(_index);
            if (_iconSold != null) _iconSold.gameObject.SetActive(_isSold);
        }

        private void OnClick()
        {
            if (_renderPicture == null) return;
            if (_outlineSprite == null || _colorSprite == null) return;
            ApplySoldStateFromUserData();
            if (_isSold) return;

            if (_isCompleted) Global.Send(new SignalActiveLockAuction { IsActive = false });
            else Global.Send(new SignalActiveLockAuction { IsActive = true });
            
            Global.Send(new SignalClosePopupCollections());
            _renderPicture.OpenMapArt();
            Global.Send(new SignalTogglePopupArt { IsActive = true });
            _renderPicture.RenderOutLineWithPair(_outlineSprite, _colorSprite, _index);
        }

        public void ResetCell()
        {
            _isCompleted = false;
            if (_previewImage != null) _previewImage.sprite = _outlineSprite;
            _isSold = false;
            if (_iconSold != null) _iconSold.gameObject.SetActive(false);
        }

        public void SetPreviewSprite(Sprite sprite)
        {
            if (_previewImage != null) _previewImage.sprite = sprite;
            _isCompleted = true;
        }
    }
}
