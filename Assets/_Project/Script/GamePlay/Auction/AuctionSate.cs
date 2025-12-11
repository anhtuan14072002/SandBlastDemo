using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

namespace Sand
{
    public class AuctionSate : MonoBehaviour
    {
        [Header("People")] [SerializeField] private AuctionInfo[] _people;

        [Header("Random money")] [SerializeField]
        private int _minMoney = 100;

        [SerializeField] private int _maxMoney = 500;

        [Header("Image Picture auction")] [SerializeField]
        private Image _imagePicture;

        [Header("Button Close")]
        [SerializeField] private GameObject _closeButton;
        [SerializeField] private float _targetTweenOpen;
        [SerializeField] private float _targetTweenClose;
        [SerializeField] private GameObject _groupBtn;
        
        private int _currentIndex = -1;
        private bool _isFinished = false;
        private int _auctionPictureIndex = -1;
        private int _currentMoney = 0;

        RenderPicture _renderPicture;
        UserData _userData;
        EffectGame _effectGame;
        RewardSystem _rewardSystem;

        [Inject]
        void Construct(UserData userData, RenderPicture renderPicture, EffectGame effectGame, RewardSystem rewardSystem)
        {
            _userData = userData;
            _renderPicture = renderPicture;
            _effectGame = effectGame;
            _rewardSystem = rewardSystem;
        }

        private void Awake()
        {
            if (_people == null || _people.Length == 0)
                _people = GetComponentsInChildren<AuctionInfo>(true);
        }

        private void OnEnable()
        {
            ResetState();
            _groupBtn.SetActive(true);
        }

        private void ResetState()
        {
            _isFinished = false;
            _currentIndex = 0;
            _currentMoney = 0;
            foreach (var p in _people) p.gameObject.SetActive(true);
            HideAllTalks();
            ShowCurrent();
        }

        private void HideAllTalks()
        {
            foreach (var p in _people)
                p.SetTalkVisible(false);
        }

        private void ShowCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _people.Length) return;
            var box = _people[_currentIndex];
            box.gameObject.SetActive(true);
            box.SetTalkVisible(true);

            int money = Random.Range(_minMoney, _maxMoney + 1);
            _currentMoney = money;
            box.SetMoney(money);
        }

        public void SetAuctionPicture(Sprite sprite, int pictureIndex)
        {
            _auctionPictureIndex = pictureIndex;
            if (_imagePicture != null)
                _imagePicture.sprite = sprite;
        }

        public void OnClickDecline()
        {
            if (_isFinished) return;
            if (_currentIndex >= 0 && _currentIndex < _people.Length)
                _people[_currentIndex].gameObject.SetActive(false);
            _currentIndex++;
            if (_currentIndex >= _people.Length)
            {
                _isFinished = true;
                Global.Send(new SignalActivePopupAuction() { IsActive = false });
                return;
            }

            ShowCurrent();
        }

        public void OnClickAccept()
        {
            if (_isFinished) return;
            _isFinished = true;

            if (_auctionPictureIndex >= 0 && _userData != null)
            {
                if (_userData.SoldPictureIndices == null)
                    _userData.SoldPictureIndices = new System.Collections.Generic.List<int>();

                if (!_userData.SoldPictureIndices.Contains(_auctionPictureIndex))
                    _userData.SoldPictureIndices.Add(_auctionPictureIndex);
            }
            _effectGame.OpenEffectFirework();
            _groupBtn.SetActive(false);
            _closeButton.SetActive(true);
            _effectGame.OpenEffectClaimTime().Forget();
            _rewardSystem.AddGems(_currentMoney);
            var rectBtnClose = _closeButton.GetComponent<RectTransform>();
            rectBtnClose.TweenAnchoredX(_targetTweenOpen, 0.25f, Ease.Linear);
        }

        public void OnClickClose()
        {
            var rectBtnClose = _closeButton.GetComponent<RectTransform>();
            rectBtnClose.TweenAnchoredX(_targetTweenClose, 0.25f, Ease.Linear);
            
            _effectGame.CloseEffectFirework();
            Global.Send(new SignalActivePopupAuction() { IsActive = false });
            Global.Send(new SignalTogglePopupArt() { IsActive = false });
            _renderPicture.CloseMapArt();
            Global.Send(new SignalOpenPopupCollections());
        }
    }
}       