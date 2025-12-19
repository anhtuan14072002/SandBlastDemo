using System;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Core;
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
        [SerializeField] private Image _fillScoreBar;
        [SerializeField] private Button _claimRewardLevelUp;
        [SerializeField] private Button _claimCoreRewardLevelUp;
        [SerializeField] private int _stepScore;
        [SerializeField] private float _fillTweenDuration = 0.3f;

        private int _currentLevelScoreValue;

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
        private int _lastCheckedScore;
        private int _level;
        private Tween _currentFillTween;

        [Inject] UserData _userData;
        [Inject] ScoreView _scoreView;
        [Inject] GameVisual _gameVisual;
        [Inject] GameResources _gameResources;
        [Inject] SoundManager _soundManager;
        [Inject] SaveService _saveService;
        [Inject] LevelModClassicData _levelModClassicData;
        [Inject] EffectGame _effectGame;

        IDisposable _subLevel;

        private void Start()
        {
            InitializeLevel();
            _claimRewardLevelUp.onClick.AddListener(() => ClaimReward().Forget());
            _claimCoreRewardLevelUp.onClick.AddListener(() => ClaimCoreReward().Forget());

            _subLevel = _userData.CurrentScore
                .Subscribe(_ => OnScoreChanged());
        }

        private void OnScoreChanged()
        {
            UpdateLevel();
            CurrentLevelScore();
            NextLevelScore();
        }

        private void InitializeLevel()
        {
            _level = _userData.LevelModClassicValue;
            _currentLevelScoreValue = _level * _stepScore;
            _nextLevelScoreValue = (_level + 1) * _stepScore;
            if (FirebaseService.Instance != null)
            {
                FirebaseService.Instance.LogEvent("level_start", new EventParameter("time", "2025"));
            }
            else
            {
                Debug.Log("FirebaseService is null");
            }
        }

        private void UpdateLevel()
        {
            bool levelChanged = false;
            while (_userData.CurrentScoreValue >= _nextLevelScoreValue)
            {
                _level++;
                _levelModClassicData.IncreaseLevelModClassic();
                _currentLevelScoreValue = _nextLevelScoreValue;
                _nextLevelScoreValue += _stepScore;
                
                if (FirebaseService.Instance != null)
                {
                    FirebaseService.Instance.LogEvent("level_up", new EventParameter("level_up", "{" + _level + "}"));
                }
                else
                {
                    Debug.Log("FirebaseService is null");
                }

                levelChanged = true;
            }

            if (levelChanged)
            {
                _currentFillTween.Stop();
                _fillScoreBar.fillAmount = 0;
                
                CurrentLevelScore();
                NextLevelScore();
                UpdateFillBarSmooth();
                DelayOpenPopupLevelUp().Forget();
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
                float targetProgress = (float)(_userData.CurrentScoreValue - _currentLevelScoreValue) / (_nextLevelScoreValue - _currentLevelScoreValue);
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
            _effectGame.OpenEffectFirework();
            _currentLevelScoreInPopup.text = _currentLevelScoreValue.ToString();
            _gameResources.ResetCoreAnimation();
        }

        private async UniTask ClosePopupLevelUp()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.25f));
            _effectGame.CloseEffectFirework();
            // DisableEffectClaimGem();
            _effectGame.CloseEffectClaimGem();
            // _popupLevelUp.SetActive(false);
        }
        
        private async UniTask DelayOpenPopupLevelUp()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.25f));
            OpenPopupLevelUp();
        }
        
        private async UniTask ClaimReward()
        {
            // EnableEffectClaimGem();
            _popupLevelUp.SetActive(false);
            _effectGame.OpenEffectClaimGem();
            await UniTask.Delay(TimeSpan.FromSeconds(1.65f));
            _gameResources.ClaimGemsLevelUp();
            ClosePopupLevelUp().Forget();
        }

        private async UniTask ClaimCoreReward()
        {
            // EnableEffectClaimGem();
            _popupLevelUp.SetActive(false); 
            _gameResources.StopCoreAnimation().Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f));
            ClosePopupLevelUp().Forget();
        }

        public void ResetLevelScore()
        {
            _currentLevelScoreValue = 0;
            _nextLevelScoreValue = _stepScore;
            CurrentLevel = 0;
            _levelModClassicData.ResetLevelModClassic();
        }
        public bool IsAnyPopupActive()
        {
            return (_popupLevelUp != null && _popupLevelUp.activeSelf);
        }
        private void OnDestroy()
        {
            _currentFillTween.Stop();
            _subLevel?.Dispose();
        }

    }
}