using Core;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Runtime;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameVisual : GameElement,
        IReceive<SignalChangTextBtnSwitchPlay>
    {
        [Header("MainMenu")] [SerializeField] private LayoutElement[] _layoutElement;
        [SerializeField] private GameObject[] _focus;
        [SerializeField] private GameObject[] _iconMenu;
        [SerializeField] private GameObject[] _popupCategory;
        [SerializeField] private GameObject[] _skill;
        [SerializeField] private GameObject _groupMenu;
        [SerializeField] private GameObject _groupCategory;
        [SerializeField] private GameObject _popupShop;
        [SerializeField] private GameObject _currenScore;
        [SerializeField] private GameObject _backGround;
        [SerializeField] private GameObject _topUI;
        [SerializeField] private GameObject _scoreBar;
        [SerializeField] private GameObject _puStart;

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

        [SerializeField] private Button _btnPauseGame;
        [SerializeField] private Button _btnResumeGame;
        [SerializeField] private Button _btnRestartGame;
        [SerializeField] private Button _btnRestartGameOver;
        [SerializeField] private Button _btnQuitGame;
        [SerializeField] private Button _btnQuitGameOver;
        [SerializeField] private Button _btnNewGame;

        [SerializeField] private TextMeshProUGUI _textBtnSwitchPlay;

        [Header("GamePlayUI")] [SerializeField]
        private GameObject _pauseMenu;

        [Inject] GameResources _gameResources;
        [Inject] CheckLevelScore _checkLevelScore;
        [Inject] GameRevive _gameRevive;
        [Inject] SoundManager _soundManager;
        [Inject] ScoreData _scoreData;
        // [Inject] RenderPicture _renderPicture;

        private void Start()
        {
            // _gdprScript.CallGDPR();
            // AdManager.Instance.LoadBanner();
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
            _btnNewGame.onClick.AddListener(() => { NewGame().Forget(); });
            //popupGamePlay
            _btnPauseGame.onClick.AddListener(PauseGame);
            _btnResumeGame.onClick.AddListener(ResumeGame);

            _btnRestartGame.onClick.AddListener(InterResetGame);
            _btnRestartGameOver.onClick.AddListener(InterResetGame);

            _btnQuitGame.onClick.AddListener(() => ReturnHomeMenu().Forget());
            _btnQuitGameOver.onClick.AddListener(() => ReturnHomeGameOver().Forget());

            // _claimRewardLevelUp.onClick.AddListener(() => ClaimReward().Forget());
            // _claimCoreRewardLevelUp.onClick.AddListener(() => ClaimCoreReward().Forget());
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
            Global.Send(new SignalClosePopupCollections());

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
                                Ease.Linear));
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
                if (i == index) continue;
                _popupCategory[i].SetActive(false);
                CloseArtMenu();
                Global.Send(new SignalTogglePopupDraw() { IsActive = false });
            }
        }

        private void DisableAllCategory()
        {
            for (int i = 0; i < _popupCategory.Length; i++)
            {
                _popupCategory[i].SetActive(false);
                Global.Send(new SignalTogglePopupDraw() { IsActive = false });
            }
        }

        private void OpenHome(int index)
        {
            if (index == 2)
                _topUI.gameObject.SetActive(true);
            else
                _topUI.gameObject.SetActive(false);
        }

        //----------------popupShop----------------//
        public void OpenShopMenu()
        {
            CloseAllCategory();
            OpenCategory(3);
            // _renderPicture.CloseMapArt(); // map art
            Global.Send(new SignalTogglePopupDraw(){IsActive = false});//popup draw picture
            _popupCategory[2].gameObject.SetActive(false);
            _popupCategory[3].gameObject.SetActive(true);
        }

        public void CloseShopMenu()
        {
            CloseAllCategory();
            OpenCategory(2);
            _popupCategory[3].gameObject.SetActive(false);
            _popupCategory[2].gameObject.SetActive(true);
        }

        public void CloseAllCategory()
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                CloseCategory(i);
            }
        }
        //------------------------------------------------//

        //----------------popupArt-------------------------//
        public void CloseArtMenu()
        {
            // _renderPicture.CloseMapArt();
        }

        //===============Game=================//
        public async UniTask PlayGame()
        {
            _animLoad.gameObject.SetActive(true);
            // _animLoad.SetTrigger(EndMenu);
            CloseArtMenu();
            await UniTask.WaitForSeconds(1f);
            
            // _renderPicture.OpenMapGamePlay(); // change mapgameplay
            Global.Send(new SignalOpenGemBarIngame());
            Global.Send(new SignalTogglePopupDraw(){IsActive = false});
            Global.Send(new SignalToggleGemBarMenu() { IsActivate = false });
            Global.Send(new SignalToggleGemBarInGame() { IsActivate = true });

            // _groupMenu.SetActive(false);
            _puStart.SetActive(true);
            DisableAllCategory();
            _groupCategory.SetActive(false);
            _backGround.SetActive(false);
            _currenScore.SetActive(true);
            _scoreBar.SetActive(true);
            _btnPauseGame.gameObject.SetActive(true);
            EnableSkill();
            _animLoad.SetTrigger(LoadGame);
            await UniTask.WaitForSeconds(1f);

            AdManager.Instance.ShowBanner();

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

        public void InterResetGame()
        {
            if (AdManager.Instance.IsMrecReady())
                AdManager.Instance.HideMrec();
            AdManager.Instance.ShowInterstitial(ResetGame, null, "replay_game");
        }

        public void ResetGame()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(true);
            ChangTextBtnSwitchPlay(false);
            EnableSkill();
            Global.Send(new SignalClosePopupGameOver());
            _gameRevive.ResetReviveUI();
            _checkLevelScore.ResetLevelScore();
            _scoreData.ResetCurrentScore();
            _checkLevelScore.NextLevelScoreValue = _checkLevelScore.StepScore;
            if (_renderMap != null) _renderMap.Reset();
            ResetAllBoxSpawnLocks();
            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());
        }

        public async UniTask ReturnHomeMenu()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(false);
            DisableSkill();
            _animLoad.gameObject.SetActive(true);
            ChangTextBtnSwitchPlay(true);
            Global.Send(new SignalCloseGemBarIngame());
            _puStart.SetActive(false);
            await UniTask.WaitForSeconds(1f);

            Global.Send(new SignalToggleGemBarMenu() { IsActivate = true });
            Global.Send(new SignalToggleGemBarInGame() { IsActivate = false });

            AdManager.Instance.HideBanner();
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
            Global.Send(new SignalCloseGemBarIngame());
            Global.Send(new SignalClosePopupGameOver());
            _gameRevive.ResetReviveUI();

            ChangTextBtnSwitchPlay(false);

            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());
            _puStart.SetActive(false);
            await UniTask.WaitForSeconds(1f);

            Global.Send(new SignalToggleGemBarMenu() { IsActivate = true });
            Global.Send(new SignalToggleGemBarInGame() { IsActivate = false });

            AdManager.Instance.HideBanner();

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

        private void OpenPopupHome()
        {
            _popupCategory[2].gameObject.SetActive(true);
        }

        public async UniTask NewGame()
        {
            PlayGame().Forget();
            await UniTask.WaitForSeconds(1f);
            ResetGame();
        }

        //---------------------CheckSwitchButtonPlay-------------------//

        public void ChangTextBtnSwitchPlay(bool change)
        {
            _textBtnSwitchPlay.text = change ? "CONTINUE" : "PLAY";
        }

        //---------------------------------------------------------//
        //Skill
        private void EnableSkill()
        {
            /*for (int i = 0; i < _skill.Length; i++)
            {
                _skill[i].SetActive(true);
            }*/
        }

        private void DisableSkill()
        {
            /*for (int i = 0; i < _skill.Length; i++)
            {
                _skill[i].SetActive(false);
            }*/
        }

        public void ResetGameStart()
        {
            ChangTextBtnSwitchPlay(false);
            _gameRevive.ResetReviveUI();
            _checkLevelScore.ResetLevelScore();
            _scoreData.ResetCurrentScore();
            ResetAllBoxSpawnLocks();
            _checkLevelScore.NextLevelScoreValue = _checkLevelScore.StepScore;
            if (_renderMap != null) _renderMap.Reset();
            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());

        }
        private void ResetAllBoxSpawnLocks()
        {
            var boxSpawns = FindObjectsOfType<BoxSpawn>();
            foreach (var boxSpawn in boxSpawns)
            {
                boxSpawn.ResetLockState();
            }
        }
        //--------------------Signal----------------------//

        public void Receive(in SignalChangTextBtnSwitchPlay signal)
        {
            ChangTextBtnSwitchPlay(signal.IsChange);
        }
    }
}