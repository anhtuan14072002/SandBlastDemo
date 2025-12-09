using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameRevive : MonoBehaviour
    {
        [Header("REVIVE")]
        [SerializeField] private TextMeshProUGUI _textRevive;
        [SerializeField] private GameObject _popupRevive;
        [SerializeField] private Button _buttonCloseRevive;       
        [SerializeField] private Button _btnReviveGems;
        [SerializeField] private Button _btnReviveAds;
        [SerializeField] private Image _imageRevive;
        [SerializeField] private int _timeRevive;
        
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isEndTimeTriggered;
        private bool _isPopupOpen;
        private bool _isRevived;
        
        RenderMap _renderMap;
        GameVisual _gameVisual;
        GameResources _gameResources;
        SaveMapData _saveMapData;
        
        [Inject]
        void Construct(RenderMap renderMap, GameVisual gameVisual, GameResources gameResources, SaveMapData saveMapData)
        {
            _renderMap = renderMap;
            _gameVisual = gameVisual;
            _gameResources = gameResources;
            _saveMapData = saveMapData;
        }

        private void Start()
        {
            _buttonCloseRevive.onClick.AddListener(SetEndTime);
            _btnReviveGems.onClick.AddListener(ReviveGems);
            _btnReviveAds.onClick.AddListener(ReviveAds);
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
                _saveMapData?.ClearMapData();
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
        public void ReviveGems()
        {
            _gameResources.ReviveGame();
            SetRevived();
            if (_renderMap != null) _renderMap.Reset();
            Global.Send(new SignalResetAllBlocks());
        }

        public void ReviveAds()
        {
            AdManager.Instance.ShowReward(OnRewardSuccess, OnRewardFail, "ads_revive_gameplay");
        }

        private void OnRewardSuccess()
        {
            SetRevived();
            if (_renderMap != null) _renderMap.Reset();
            Global.Send(new SignalResetAllBlocks());
        }
        private void OnRewardFail()
        {
            Debug.Log("Core reward ad failed to display");
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