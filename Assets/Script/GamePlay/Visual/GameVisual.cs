using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class GameVisual : GameElement
    {
        [Header("MainMenu")]
        [SerializeField] private LayoutElement[] _layoutElement;
        [SerializeField] private GameObject[] _focus;
        [SerializeField] private GameObject[] _iconMenu;
        [SerializeField] private GameObject[] _popupCategory;
        [SerializeField] private Button[] _btnSelection;
        [SerializeField] private Button _btnPlay;
        [SerializeField] private GameObject _groupMenu;
        [SerializeField] private GameObject _groupCategory;
        [SerializeField] private GameObject _currenScore;
        [SerializeField] private GameObject _topUI;
        [SerializeField] private GameObject _backGround;
        [SerializeField] private float _targetIconMenu;
        [SerializeField] private float _targetFocus;
        [SerializeField] private float _currentIconMenu;
        [SerializeField] private float _currentFocus;
        [SerializeField] private RenderMap _renderMap;
        [SerializeField] private Animator _animLoad;
        private static readonly int LoadGame = Animator.StringToHash("Load");
        private static readonly int EndMenu = Animator.StringToHash("End");
        
        [Header("GamePlayUI")]
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private Button _btnPauseGame;
        [SerializeField] private Button _btnResumeGame;
        [SerializeField] private Button _btnRestartGame;
        [SerializeField] private Button _btnQuitGame;
        private void Start()
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                var i1 = i;
                _btnSelection[i].onClick.AddListener(()=>
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
            _btnResumeGame.onClick.AddListener(ResumeGame );
            _btnRestartGame.onClick.AddListener(ResetGame );
            _btnQuitGame.onClick.AddListener(() => QuitGame().Forget());;
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
            Tween.PositionY(_focus[index].transform, _targetFocus, 0.25f, Ease.Linear);
            IncreaseElement(index);
            Tween.PositionY(_iconMenu[index].transform, _targetIconMenu, 0.25f, Ease.Linear).OnComplete(() =>
                Tween.Scale(_iconMenu[index].transform, _iconMenu[index].transform.localScale, Vector3.one * 1.5f, 0.25f, Ease.Linear)
            );
        }

        private void CloseCategory(int index)
        {
            for (int i = 0; i < _btnSelection.Length; i++)
            {
                if (i != index)
                {
                    var i1 = i;
                    Tween.PositionY(_focus[i].transform,_currentFocus , 0.1f, Ease.Linear);
                    Tween.PositionY(_iconMenu[i].transform,_currentIconMenu , 0.1f, Ease.Linear)
                        .OnComplete(() =>
                            Tween.Scale(_iconMenu[i1].transform, _iconMenu[i1].transform.localScale, Vector3.one, 0.1f, Ease.Linear));
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
            _groupMenu.SetActive(false);
            _groupCategory.SetActive(false);
            _backGround.SetActive(false);
            _currenScore.SetActive(true);
            _btnPauseGame.gameObject.SetActive(true);
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

        private void ResetGame()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(true);
            if (_renderMap != null)
            {
                _renderMap.Reset();
            }
            Global.Send(new SignalResetAllBlocks());
            Global.Send(new SignalRestCurrenScore());
        }
        
        private async UniTask QuitGame()
        {
            _pauseMenu.SetActive(false);
            _btnPauseGame.gameObject.SetActive(false);
            _animLoad.gameObject.SetActive(true);
            await UniTask.WaitForSeconds(1f);
            _currenScore.SetActive(false);
            _groupMenu.SetActive(true);
            _groupCategory.SetActive(true);
            _backGround.SetActive(true);
            _animLoad.SetTrigger(LoadGame);
            await UniTask.WaitForSeconds(1f);
            _animLoad.gameObject.SetActive(false);
        }
        
    }
}