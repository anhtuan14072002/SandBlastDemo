using System;
using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameVisual : GameElement,
        IReceive<SignalOpenPopupGameOver>
    {
        [Header("MainMenu")] [SerializeField] private LayoutElement[] _layoutElement;
        [SerializeField] private GameObject[] _focus;
        [SerializeField] private GameObject[] _iconMenu;
        [SerializeField] private GameObject[] _popupCategory;
        [SerializeField] private GameObject _groupMenu;
        [SerializeField] private GameObject _groupCategory;
        [SerializeField] private GameObject _popupGameOver;
        [SerializeField] private GameObject _currenScore;
        [SerializeField] private GameObject _backGround;
        [SerializeField] private GameObject _topUI;
        [SerializeField] private GameObject _scoreBar;
        [SerializeField] private GameObject _effectClaimGem;
        [SerializeField] private GameObject _warningSand;
        [SerializeField] private GameObject[] _skill;

        [SerializeField] private Button[] _btnSelection;
        [SerializeField] private Button _btnPlay;

        [SerializeField] private float _targetIconMenu;
        [SerializeField] private float _targetFocus;
        [SerializeField] private float _currentIconMenu;
        [SerializeField] private float _currentFocus;
        [SerializeField] private RenderMap _renderMap;
        [SerializeField] private Animator _animLoad;
        private static readonly int LoadGame = Animator.StringToHash("Load");
        private static readonly int EndMenu = Animator.StringToHash("End");

        [Header("GamePlayUI")] [SerializeField]
        private GameObject _pauseMenu;

        [SerializeField] private Button _btnPauseGame;
        [SerializeField] private Button _btnResumeGame;
        [SerializeField] private Button _btnRestartGame;
        [SerializeField] private Button _btnRestartGameOver;
        [SerializeField] private Button _btnQuitGame;
        [SerializeField] private Button _btnQuitGameOver;
        [SerializeField] private Button _btnReviveGems;

        [Inject] GameResources _gameResources;
        [Inject] GameRevive _gameRevive;
        [Inject] CheckLevelScore _checkLevelScore;
        [Inject] SoundManager _soundManager;

        private void Start()
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                var i1 = i;
                _btnSelection[i].onClick.AddListener(() =>
                {
                    OpenCategory(i1);
                    CloseCategory(i1);
                    EnableCategory(i1);
                    DisableCategory(i1);
                });
            }

            _btnPlay.onClick.AddListener(() => PlayGame().Forget());
            //popupGamePlay
            _btnPauseGame.onClick.AddListener(PauseGame);
            _btnResumeGame.onClick.AddListener(ResumeGame);

            _btnRestartGame.onClick.AddListener(ResetGame);
            _btnRestartGameOver.onClick.AddListener(ResetGame);

            _btnQuitGame.onClick.AddListener(() => ReturnHomeMenu().Forget());
            _btnQuitGameOver.onClick.AddListener(() => ReturnHomeGameOver().Forget());

            // _claimRewardLevelUp.onClick.AddListener(() => ClaimReward().Forget());
            // _claimCoreRewardLevelUp.onClick.AddListener(() => ClaimCoreReward().Forget());

            _btnReviveGems.onClick.AddListener(ReviveGems);
        }

        private void IncreaseElement(int index)
        {
            _layoutElement[index].flexibleWidth = 1.5f;

            for (int i = 0; i < _layoutElement.Length; i++)
            {
                if (i != index) _layoutElement[i].flexibleWidth = 1;
            }
        }

        private void OpenCategory(int index)
        {
            OpenHome(index);
            var focusRt = _focus[index].GetComponent<RectTransform>();
            var iconRt = _iconMenu[index].GetComponent<RectTransform>();

            focusRt.TweenAnchoredY(_targetFocus, 0.25f, Ease.Linear);
            IncreaseElement(index);
            iconRt.TweenAnchoredY(_targetIconMenu, 0.25f, Ease.Linear).OnComplete(() =>
                Tween.Scale(_iconMenu[index].transform, _iconMenu[index].transform.localScale, Vector3.one * 1.5f,
                    0.25f, Ease.Linear));
        }

        private void CloseCategory(int index)
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                if (i != index)
                {
                    var i1 = i;
                    var focusRt = _focus[i].GetComponent<RectTransform>();
                    var iconRt = _iconMenu[i].GetComponent<RectTransform>();

                    focusRt.TweenAnchoredY(_currentFocus, 0.1f, Ease.Linear);
                    iconRt.TweenAnchoredY(_currentIconMenu, 0.1f, Ease.Linear)
                        .OnComplete(() =>
                            Tween.Scale(_iconMenu[i1].transform, _iconMenu[i1].transform.localScale, Vector3.one, 0.1f,
                                Ease.Linear)
                        );
                }
            }
        }

        private void EnableCategory(int index)
        {
            _popupCategory[index].SetActive(true);
        }

        private void DisableCategory(int index)
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                if (i != index)
                {
                    _popupCategory[i].SetActive(false);
                }
            }
        }

        private void DisableAllCategory()
        {
            for (int i = 0; i < _popupCategory.Length; i++)
            {
                _popupCategory[i].SetActive(false);
            }
        }

        private void OpenHome(int index)
        {
            if (index == 2)
                _topUI.gameObject.SetActive(true);
            else
                _topUI.gameObject.SetActive(false);
        }

        public async UniTask PlayGame()
        {
            _animLoad.gameObject.SetActive(true);
            // _animLoad.SetTrigger(EndMenu);
            await UniTask.WaitForSeconds(1f);
            // _groupMenu.SetActive(false);
            DisableAllCategory();
            _groupCategory.SetActive(false);
            _backGround.SetActive(false);
            _currenScore.SetActive(true);
            _scoreBar.SetActive(true);
            _btnPauseGame.gameObject.SetActive(true);
            EnableSkill();
            _animLoad.SetTrigger(LoadGame);
            await UniTask.WaitForSeconds(1f);
            if (_renderMap != null)
                _renderMap.StartGame();
            _animLoad.gameObject.SetActive(false);
        }

        private void PauseGame()
        {
            _pauseMenu.SetActive(true);
            _btnPauseGame.gameObject.SetActive(false);
        }

        private void ResumeGame()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(true);
        }

        public void ResetGame()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(true);
            EnableSkill();
            DisablePopupGameOver();
            _gameRevive.ResetReviveUI();
            _checkLevelScore.ResetLevelScore();
            _checkLevelScore.NextLevelScoreValue = _checkLevelScore.StepScore;
            if (_renderMap != null)
                _renderMap.Reset();
            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());
        }

        public void ReviveGems()
        {
            _gameResources.ReviveGame();
            _gameRevive.SetRevived();
            if (_renderMap != null) _renderMap.Reset();
            Global.Send(new SignalResetAllBlocks());
        }

        public async UniTask ReturnHomeMenu()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(false);
            DisableSkill();
            _animLoad.gameObject.SetActive(true);
            await UniTask.WaitForSeconds(1f);
            _currenScore.SetActive(false);
            _scoreBar.SetActive(false);
            _groupMenu.SetActive(true);
            _groupCategory.SetActive(true);
            OpenPopupHome();
            _backGround.SetActive(true);
            _animLoad.SetTrigger(LoadGame);
            await UniTask.WaitForSeconds(1f);
            _animLoad.gameObject.SetActive(false);
        }

        private async UniTask ReturnHomeGameOver()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(false);
            DisableSkill();
            _animLoad.gameObject.SetActive(true);
            DisablePopupGameOver();
            _gameRevive.ResetReviveUI();
            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());
            await UniTask.WaitForSeconds(1f);
            _currenScore.SetActive(false);
            _scoreBar.SetActive(false);
            _groupMenu.SetActive(true);
            OpenPopupHome();
            _groupCategory.SetActive(true);
            _backGround.SetActive(true);
            _animLoad.SetTrigger(LoadGame);
            await UniTask.WaitForSeconds(1f);
            _animLoad.gameObject.SetActive(false);
        }

        private void DisablePopupGameOver()
        {
            if (_renderMap != null) _renderMap.Reset();
            if (_popupGameOver.activeSelf) _popupGameOver.SetActive(false);
        }

        public void Receive(in SignalOpenPopupGameOver signal)
        {
            _popupGameOver.SetActive(true);
        }

        private void OpenPopupHome()
        {
            _popupCategory[2].gameObject.SetActive(true);
        }

        public void EnableEffectClaimGem()
        {
            _effectClaimGem.SetActive(true);
            _soundManager.OnPlaySound(SoundType.Reward);
        }

        public void DisableEffectClaimGem()
        {
            _effectClaimGem.SetActive(false);
        }

        //Skill
        private void EnableSkill()
        {
            for (int i = 0; i < _skill.Length; i++)
            {
                _skill[i].SetActive(true);
            }
        }

        private void DisableSkill()
        {
            for (int i = 0; i < _skill.Length; i++)
            {
                _skill[i].SetActive(false);
            }
        }

        // 
        public void WarningSand(bool isWarningSand)
        {
            _warningSand.SetActive(isWarningSand);
        }
    }
}