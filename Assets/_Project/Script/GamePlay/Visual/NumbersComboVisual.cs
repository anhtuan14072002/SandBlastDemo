using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class NumbersComboVisual : GameElement,
        IReceive<SignalIncreaseNumberCombo>,
        // IReceive<SignalShowCountDrawGameOver>,
        IReceive<SignalIncreaseCountDrawIngame>
    {
        [SerializeField] private TextMeshProUGUI _textCountDrawGamePlay;
        [SerializeField] private TextMeshProUGUI _textCountDrawGameOver;
        [SerializeField] private GameObject _drawCountBar;
        [SerializeField] private float _targetTweenDown;
        [SerializeField] private float _targetTweenUp;
        [SerializeField] private float _timeDelay;

        private int _numberCombo = 0;
        private int _countDrawIngame = 0;
        private CancellationTokenSource _comboTimerCts;
        
        UserData _userData;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }

        private void Start()
        {
            _textCountDrawGamePlay.text = _numberCombo.ToString();
        }

        public async UniTask IncreaseNumberCombo(int numberCombo)
        {
            TweenDownCombo();
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            _numberCombo += numberCombo;
            _textCountDrawGamePlay.text = _numberCombo.ToString();

            _comboTimerCts?.Cancel();
            _comboTimerCts = new CancellationTokenSource();

            await UniTask.Delay(TimeSpan.FromSeconds(_timeDelay), cancellationToken: _comboTimerCts.Token)
                .SuppressCancellationThrow();
            if (!_comboTimerCts.Token.IsCancellationRequested) TweenUpCombo();
        }

        public void IncreaseCountDrawIngame(int count)
        {
            _countDrawIngame += count;
        }

        public void ShowCountDrawGameOver()
        {
            AnimText.AnimateNumberChange(_textCountDrawGameOver, 0, _numberCombo, 0.5f, 8, _textCountDrawGameOver.gameObject, false).Forget();
            
            /*if (_countDrawIngame > 0)
            {
                _userData.CountDraw.Value += _countDrawIngame;
            }*/
        }
        public void ResetCombo()
        {
            _numberCombo = 0;
            _countDrawIngame = 0;
            _textCountDrawGamePlay.text = _numberCombo.ToString();
        }

        public void TweenDownCombo()
        {
            var rectTransform = _drawCountBar.gameObject.GetComponent<RectTransform>();
            rectTransform.TweenAnchoredY(_targetTweenDown, 0.2f, Ease.Linear);
        }

        public void TweenUpCombo()
        {
            var rectTransform = _drawCountBar.gameObject.GetComponent<RectTransform>();
            rectTransform.TweenAnchoredY(_targetTweenUp, 0.2f, Ease.Linear);
        }

        private void OnDestroy()
        {
            _comboTimerCts?.Dispose();
        }

        public void Receive(in SignalIncreaseNumberCombo signal)
        {
            IncreaseNumberCombo(signal.Count).Forget();
        }

        // public void Receive(in SignalShowCountDrawGameOver signal)
        // {
        //     ShowCountDrawGameOver();
        // }

        public void Receive(in SignalIncreaseCountDrawIngame signal)
        {
            IncreaseCountDrawIngame(signal.Count);
        }
    }
}