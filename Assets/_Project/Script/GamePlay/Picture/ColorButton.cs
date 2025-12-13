using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class ColorButton : MonoBehaviour
    {
        private Image _buttonImage;
        private Button _button;
        private Color32 _color;
        private System.Action<Color32> _onColorSelected;

        public void Init(Color32 color, System.Action<Color32> onColorSelected)
        {
            _color = color;
            _onColorSelected = onColorSelected;

            _buttonImage = GetComponent<Image>();
            _button = GetComponent<Button>();

            if (_buttonImage != null)
            {
                _buttonImage.color = _color;
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            _onColorSelected?.Invoke(_color);
        }
    }
}