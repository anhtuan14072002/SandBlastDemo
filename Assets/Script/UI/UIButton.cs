using Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WZ
{
    [RequireComponent(typeof(Image))]
    public class UIButton : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Scale")] [SerializeField] private Vector3 _scalePress = new Vector3(0.9f, 0.9f, 0.9f);
        [SerializeField] private Vector3 _scaleNormal = Vector3.one;
        [SerializeField] private float _duration = 0.1f;

        [Header("Color")] [SerializeField] private Color _colorNormal = Color.white;
        [SerializeField] private Color _colorDisable = Color.gray;

        [SerializeField] private Button _button;
        private Image _image;
        private RectTransform _rectTransform;

#if UNITY_EDITOR
        protected override void Reset()
        {
            _button = GetComponent<Button>();
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Đặt trạng thái ban đầu
            UpdateButtonState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            // Tween scale khi nhấn xuống
            _rectTransform.DoScaleRect(false, _scalePress, _duration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            // Tween scale khi thả ra
            _rectTransform.DoScaleRect(false, _scaleNormal, _duration, Ease.OutBack);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            Debug.Log("Button clicked!");
            // Có thể thêm hiệu ứng click đặc biệt ở đây
        }

        private void UpdateButtonState()
        {
            if (_button.interactable)
            {
                // Trạng thái bình thường
                _image.DoSetImageColor(false, _colorNormal, _duration, Ease.Linear);
                _rectTransform.DoScaleRect(false, _scaleNormal, _duration, Ease.Linear);
            }
            else
            {
                // Trạng thái vô hiệu hóa
                _image.DoSetImageColor(false, _colorDisable, _duration, Ease.Linear);
                _rectTransform.DoScaleRect(false, _scaleNormal, _duration, Ease.Linear);
            }
        }

        // Gọi hàm này khi thay đổi trạng thái interactable của button
        public void RefreshButtonState()
        {
            UpdateButtonState();
        }
    }
}