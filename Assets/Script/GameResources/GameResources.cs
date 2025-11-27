using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameResources : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _textPriceBuyGem;
        [SerializeField] private TextMeshProUGUI[] _textAmoutGems;
        [SerializeField] private TextMeshProUGUI _coreText;
        [SerializeField] private GameObject _pointerCore;
        [SerializeField] private Button[] _btnBuyGem;
        [SerializeField] private int[] _priceBuyGem;
        [SerializeField] private int[] _amountGem;
        [SerializeField] private float _targetX;
        [SerializeField] private int _rewardGemsLevelUp;
        [SerializeField] private int _gemsRevive = 100;
        [SerializeField] private int _stepCore = 80;

        private RectTransform _pointerRectTransform;
        private bool _isPauseCoreReward;
        private Tween _currentTween;
        private int _currentCore = 1;

        RewardSystem _rewardSystem;
        UserData _userData;
        IDisposable _subCore;

        [Inject]
        public void Construct(UserData userData, RewardSystem rewardSystem)
        {
            _userData = userData;
            _rewardSystem = rewardSystem;
        }

        private void Start()
        {
            _pointerRectTransform = _pointerCore.GetComponent<RectTransform>();
            StartCoreAnimation();
            _subCore = Observable.EveryUpdate().Subscribe(_ =>
                CheckCoreReward(_pointerRectTransform.anchoredPosition.x));
            _btnBuyGem[0].onClick.AddListener(BuyNoAds);

            for (int i = 1; i < _btnBuyGem.Length; i++)
            {
                int index = i;
                _btnBuyGem[i].onClick.AddListener(() => BuyGems(_amountGem[index]));
            }

            for (int i = 0; i < _textPriceBuyGem.Length; i++)
            {
                _textPriceBuyGem[i].text = _priceBuyGem[i].ToString();
            }

            for (int i = 1; i < _textAmoutGems.Length; i++)
            {
                _textAmoutGems[i].text = _amountGem[i].ToString();
            }
        }

        private void BuyNoAds()
        {
            Debug.Log("BuyNoAds");
        }

        private void BuyGems(int gems)
        {
            _rewardSystem.AddGems(gems);
        }
        
        public void ClaimGemsLevelUp()
        {
            _rewardSystem.AddGems(_rewardGemsLevelUp);
            
        }
        
        private void ClaimCoreGemsLevelUp(int core)
        {
            _rewardSystem.AddGems(_rewardGemsLevelUp * core);
        }

        private void StartCoreAnimation()
        {
            if (_isPauseCoreReward) return;
            _currentTween = _pointerRectTransform.TweenAnchoredX(_targetX, 0.75f, Ease.Linear, -1, CycleMode.Yoyo);
        }

        public async UniTask StopCoreAnimation()
        {
            _isPauseCoreReward = true;
            if (_currentTween.isAlive)
            {
                _currentTween.Stop();
            }

            float currentX = _pointerRectTransform.anchoredPosition.x;
            CheckCoreReward(currentX);
            await UniTask.Delay(TimeSpan.FromSeconds(1.65f));
            ClaimCoreGemsLevelUp(_currentCore);
        }

        public void ResetCoreAnimation()
        {
            if (_currentTween.isAlive) _currentTween.Stop();
            _isPauseCoreReward = false;
            _currentCore = 1;

            var pos = _pointerRectTransform.anchoredPosition;
            _pointerRectTransform.anchoredPosition = new Vector2(0f, pos.y);
            StartCoreAnimation();
        }

        private void CheckCoreReward(float positionX)
        {
            float absX = Mathf.Abs(positionX);

            if (absX >= 0 && absX < _stepCore)
            {
                _coreText.text = "X2";
                _currentCore = 2;
            }
            else if (absX >= _stepCore && absX < _stepCore * 2)
            {
                _coreText.text = "X3";
                _currentCore = 3;
            }
            else if (absX >= _stepCore * 2 && absX < _stepCore * 3)
            {
                _coreText.text = "X4";
                _currentCore = 4;
            }
            else if (absX >= _stepCore * 3 && absX < _stepCore * 4)
            {
                _coreText.text = "X5";
                _currentCore = 5;
            }
            else if (absX >= _stepCore * 4 && absX < _stepCore * 5)
            {
                _coreText.text = "X4";
                _currentCore = 4;
            }
            else if (absX >= _stepCore * 5 && absX < _stepCore * 6)
            {
                _coreText.text = "X3";
                _currentCore = 3;
            }
            else if (absX >= _stepCore * 6 && absX < _stepCore * 7)
            {
                _coreText.text = "X2";
                _currentCore = 2;
            }
            else
            {
                _coreText.text = "X1";
                _currentCore = 1;
            }
        }

        public void ReviveGame()
        {
            _rewardSystem.DeductGems(_gemsRevive);
        }

        public void ResetGem()
        {
            _rewardSystem.DeductGems(_userData.Gems.Value);;
        }
        private void OnDestroy()
        {
            _subCore?.Dispose();
            if (_currentTween.isAlive)
            {
                _currentTween.Stop();
            }
        }
    }
}