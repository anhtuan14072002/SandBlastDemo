using Core;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class CountDrawView : MonoBehaviour,
        IReceive<SignalOpenPopupLibrary>,
        IReceive<SignalIncreaseCountDrawIngame>
    {
        [SerializeField] private TextMeshProUGUI _textCountDrawPopupLibrary;
        [SerializeField] private TextMeshProUGUI _textCountDrawIngame;

        private UserData _userData;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }

        private void Start()
        {
            UpdateCountDrawDisplay();
        }

        private void UpdateCountDrawDisplay()
        {
            if (_textCountDrawPopupLibrary != null)
            {
                // _textCountDrawPopupLibrary.text = _userData.CountDraw.Value.ToString();
            }
        }

        private void UpdateCountDrawIngameDisplay(int countIngame)
        {
            if (_textCountDrawIngame != null)
            {
                _textCountDrawIngame.text = countIngame.ToString();
            }
        }

        public void Receive(in SignalOpenPopupLibrary signal)
        {
            UpdateCountDrawDisplay();
            // Debug.Log($"[CountDrawView] Hiển thị CountDraw: {_userData.CountDraw.Value}");
        }

        public void Receive(in SignalIncreaseCountDrawIngame signal)
        {
            UpdateCountDrawIngameDisplay(signal.Count);
            Debug.Log($"[CountDrawView] Tăng CountDrawIngame: {signal.Count}");
        }
    }
}