using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameRevive : MonoBehaviour
    {
        [Header("REVIVE")] [SerializeField] private TextMeshProUGUI _textRevive;
        [SerializeField] private GameObject _popupRevive;
        [SerializeField] private Image _imageRevive;
        [SerializeField] private Button _buttonCloseRevive;
        [SerializeField] private int _timeRevive;
        RenderMap _renderMap;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isEndTimeTriggered = false;
        private bool _isPopupOpen = false;
        private bool _isRevived = false;

        [Inject]
        void Construct(RenderMap renderMap)
        {
            _renderMap = renderMap;
        }

        private void Start()
        {
            // OpenPopupRevive();
            _buttonCloseRevive.onClick.AddListener(SetEndTime);
        }

        public async void OpenPopupRevive()
        {
            if (_isPopupOpen) return;
            _isPopupOpen = true;
            _isRevived = false;
            _popupRevive.SetActive(true);
            _isEndTimeTriggered = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await ReviveCountdown(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask ClosePopupRevive()
        {
            if (!_isPopupOpen) return;
            _isPopupOpen = false;
            _popupRevive.SetActive(false);
            _cancellationTokenSource?.Cancel();
            if (!_isRevived)
            {
                _renderMap.MapGameOver();
                await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
                Global.Send(new SignalOpenPopupGameOver());
                _imageRevive.fillAmount = 0f;
            }
        }

        private async UniTask ReviveCountdown(CancellationToken cancellationToken)
        {
            float currentTime = _timeRevive;
            while (currentTime > 0 && !cancellationToken.IsCancellationRequested && !_isEndTimeTriggered)
            {
                _textRevive.text = Mathf.Ceil(currentTime).ToString();
                _imageRevive.fillAmount = currentTime / _timeRevive;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                currentTime -= Time.deltaTime;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                _textRevive.text = "0";
                _imageRevive.fillAmount = 0f;
                ClosePopupRevive().Forget();
            }
        }

        private void SetEndTime()
        {
            _isEndTimeTriggered = true;
        }
        public void SetRevived()
        {
            _isRevived = true;
            _isEndTimeTriggered = true;
        }
        public void ResetReviveUI()
        {
            _imageRevive.fillAmount = 0f;
            _isEndTimeTriggered = false;
            _isPopupOpen = false;
            _isRevived = false;
            _cancellationTokenSource?.Cancel();
            _popupRevive.SetActive(false);
        }
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}