using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using PrimeTween;

namespace Sand
{
    public class CheckLevelScore : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currentLevelScore;
        [SerializeField] private TextMeshProUGUI _currentLevelScoreInPopup;
        [SerializeField] private TextMeshProUGUI _nextLevelScore;
        [SerializeField] private GameObject _popupLevelUp;
        [SerializeField] private GameObject _effectLevelUp;
        [SerializeField] private Image _fillScoreBar;
        [SerializeField] private Button _claimRewardLevelUp;
        [SerializeField] private Button _claimCoreRewardLevelUp;
        [SerializeField] private int _stepScore;
        [SerializeField] private float _fillTweenDuration = 0.3f;

        private int _currentLevelScoreValue;
        public int LevelScoreValue
        {
            get => _currentLevelScoreValue;
            set => _currentLevelScoreValue = value;
        }

        public int NextLevelScoreValue
        {
            get => _nextLevelScoreValue;
            set => _nextLevelScoreValue = value;
        }
        public int StepScore => _stepScore;
        public int CurrentLevel
        {
            get => _level;
            set => _level = value;
        }

        private int _nextLevelScoreValue;
        private int _lastCheckedScore = 0;
        private int _level = 0;
        private Tween _currentFillTween;

        [Inject] ScoreView _scoreView;
        [Inject] GameVisual _gameVisual;
        [Inject] GameResources _gameResources;
        [Inject] SoundManager _soundManager;

        IDisposable _subCurrentScore;
        IDisposable _subNextScore;
        IDisposable _subLevel;
        
        private void Start()
        {
            _currentLevelScoreValue = 0;
            _nextLevelScoreValue = _stepScore;
            
            _claimRewardLevelUp.onClick.AddListener(() => ClaimReward().Forget());
            _claimCoreRewardLevelUp.onClick.AddListener(() => ClaimCoreReward().Forget());
            
            _subCurrentScore = Observable.EveryUpdate().Subscribe(_ => CurrentLevelScore());
            _subNextScore = Observable.EveryUpdate().Subscribe(_ => NextLevelScore());
            _subLevel = Observable.EveryUpdate().Subscribe(_ =>
            {
                if (_scoreView._score != _lastCheckedScore)
                {
                    _lastCheckedScore = _scoreView._score;
                    UpdateLevel();
                }
            });
        }

        private void UpdateLevel()
        {
            bool levelChanged = false;
            while (_scoreView._score >= _nextLevelScoreValue)
            {
                _level++;
                _currentLevelScoreValue = _nextLevelScoreValue;
                _nextLevelScoreValue += _stepScore;
                levelChanged = true;
            }

            if (levelChanged)
            {
                _currentFillTween.Stop();
                _fillScoreBar.fillAmount = 0;
                UpdateFillBarSmooth();
                // _gameVisual.OpenPopupLevelUp();
                OpenPopupLevelUp();
            }
            else
            {
                UpdateFillBarSmooth();
            }
        }

        private void UpdateFillBarSmooth()
        {
            if (_nextLevelScoreValue > _currentLevelScoreValue)
            {
                float targetProgress = (float)(_scoreView._score - _currentLevelScoreValue) / (_nextLevelScoreValue - _currentLevelScoreValue);
                targetProgress = Mathf.Clamp01(targetProgress);
                _currentFillTween.Stop();
                _currentFillTween = Tween.Custom(_fillScoreBar.fillAmount, targetProgress, _fillTweenDuration,
                     value => _fillScoreBar.fillAmount = value, Ease.OutQuad);
            }
        }

        private void CurrentLevelScore()
        {
            var currentLevelScoreShow = _currentLevelScoreValue / 1000;
            _currentLevelScore.text = currentLevelScoreShow.ToString() + "K";
        }

        private void NextLevelScore()
        {
            var nextLevelScoreShow = _nextLevelScoreValue / 1000;
            _nextLevelScore.text = nextLevelScoreShow.ToString() + "K";
        }

        public void OpenPopupLevelUp()
        {
            _popupLevelUp.SetActive(true);
            _soundManager.OnPlaySound(SoundType.LevelUp);
            _effectLevelUp.SetActive(true);
            _currentLevelScoreInPopup.text = _currentLevelScoreValue.ToString();
            _gameResources.ResetCoreAnimation(); 
        }

        private async UniTask ClosePopupLevelUp()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.25f));
            _effectLevelUp.SetActive(false);
            // DisableEffectClaimGem();
            _gameVisual.DisableEffectClaimGem();
            // _popupLevelUp.SetActive(false);
        }
        private void OnDestroy()
        {
            _currentFillTween.Stop();
            _subCurrentScore?.Dispose();
            _subNextScore?.Dispose();
            _subLevel?.Dispose();
        }
        private async UniTask ClaimReward()
        {
            // EnableEffectClaimGem();
            _popupLevelUp.SetActive(false);
            _gameVisual.EnableEffectClaimGem();
            await UniTask.Delay(TimeSpan.FromSeconds(1.65f));
            _gameResources.ClaimGemsLevelUp();
            ClosePopupLevelUp().Forget();
        }
        
        private async UniTask ClaimCoreReward()
        {
            // EnableEffectClaimGem();
            _popupLevelUp.SetActive(false);
            _gameVisual.EnableEffectClaimGem();
            _gameResources.StopCoreAnimation().Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f));
            ClosePopupLevelUp().Forget();
        }
    }
}
