using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

namespace Sand
{
    public class LeaderBoardList : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject _objRankMain;
        [SerializeField] private GameObject _objRankBot;
        [SerializeField] private Transform _parentPost;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _btnRank;
        [SerializeField] private Button _btnTest;

        [Header("Settings")]
        [SerializeField] private int _poolSize = 20;      
        [SerializeField] private int _score = 999999;     
        [SerializeField] private float _scaleSize = 1.2f;
        [SerializeField] private float _scaleDuration = 0.25f;
        [SerializeField] private float _scrollDuration = 0.5f;
        [SerializeField] private float _moveToSlotDuration = 0.25f;

        [Header("Top Rank Views")]
        [SerializeField] private TopRankView[] _topViews;

        [Header("Bot Random Names")]
        [SerializeField] private string[] _randomNames =
        {
            "Luna", "Kaito", "Milo", "Alice", "Rin", "Nova", "Zero",
            "Ava", "Kira", "Rex", "Nyx", "Kai", "Juno", "Mira",
            "Sora", "Haru", "Leo", "Nami", "Ray", "Zane",
            "Ken", "Kari", "Max", "Yuri", "Lyn"
        };

        private List<(GameObject obj, int score)> _rankList;
        private bool _isAnimating;
        private GameObject _targetPlaceholder;

        UserData _UserData;

        [Inject]
        void Construct(UserData userData)
        {
            _UserData = userData;
        }
        
        private void Start()
        {
            Instantiate(_objRankBot, _parentPost);
            // InitPool();
            if (_btnRank != null) _btnRank.onClick.AddListener(() => SetMainPlayerScoreAnimated(_UserData.HighScoreValue));
            _btnTest.onClick.AddListener(() => SetMainPlayerScoreAnimated(_score));
        }

        //================= INIT =================

        private void InitPool()
        {
            SpawnPool.InitPool(_objRankBot, _poolSize, default, _parentPost, true);

            _rankList = new List<(GameObject, int)>();

            // Spawn bot
            for (int i = 0; i < _poolSize; i++)
            {
                var bot = SpawnPool.Spawn(_objRankBot, Vector3.zero, Quaternion.identity, default, _parentPost);
                int randomScore = Random.Range(20000, 5000001);

                var info = bot.GetComponent<InfoRank>();
                if (info != null)
                {
                    info.PlayerName = GetRandomName();
                }
                _rankList.Add((bot, randomScore));
            }
            _objRankMain.transform.SetParent(_parentPost);
            var mainInfo = _objRankMain.GetComponent<InfoRank>();
            if (mainInfo != null)
            {
                mainInfo.PlayerName = "You";
            }
            _rankList.Add((_objRankMain, 0));
            UpdateLeaderBoard();
        }

        private string GetRandomName()
        {
            if (_randomNames == null || _randomNames.Length == 0) return "Bot";
            return _randomNames[Random.Range(0, _randomNames.Length)];
        }

        //================= UPDATE LIST + TOP =================

        private void UpdateLeaderBoard()
        {
            _rankList = _rankList.OrderByDescending(x => x.score).ToList();

            for (int i = 0; i < _rankList.Count; i++)
            {
                var info = _rankList[i].obj.GetComponent<InfoRank>();
                if (info != null)
                {
                    info.Index = i + 1;
                    info.Score = _rankList[i].score;
                    info.SetInfo();
                }

                _rankList[i].obj.transform.SetSiblingIndex(i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_parentPost);
            Canvas.ForceUpdateCanvases();

            UpdateTopViews();
        }

        private void UpdateTopViews()
        {
            if (_topViews == null || _topViews.Length == 0)
                return;

            for (int i = 0; i < _topViews.Length; i++)
            {
                var view = _topViews[i];
                if (view == null)
                    continue;

                if (i < _rankList.Count)
                {
                    var info = _rankList[i].obj.GetComponent<InfoRank>();
                    if (info != null)
                    {
                        // dùng PlayerName thay vì số hạng
                        view.SetFromInfo(info.PlayerName, info.Score);
                        view.gameObject.SetActive(true);
                    }
                    else
                    {
                        view.gameObject.SetActive(false);
                    }
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        //================= RANK CALC + SCORE APPLY =================

        private int CalculateTargetIndex(int newScore, out int oldIndex)
        {
            var tempList = _rankList.ToList();

            oldIndex = tempList.FindIndex(t => t.obj == _objRankMain);
            if (oldIndex < 0) oldIndex = 0;

            tempList[oldIndex] = (_objRankMain, newScore);
            tempList = tempList.OrderByDescending(x => x.score).ToList();

            int targetIndex = tempList.FindIndex(t => t.obj == _objRankMain);
            return targetIndex;
        }
        
        private void ApplyNewScoreToMain(int newScore)
        {
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == _objRankMain)
                {
                    _rankList[i] = (_objRankMain, newScore);
                    break;
                }
            }
        }

        //================= PUBLIC API =================

        public void SetMainPlayerScore(int newScore)
        {
            ApplyNewScoreToMain(newScore);
            UpdateLeaderBoard();

            Tween.Scale(_objRankMain.transform, Vector3.one * _scaleSize, _scaleDuration, Ease.OutBack)
                .OnComplete(() =>
                {
                    Tween.Scale(_objRankMain.transform, Vector3.one, _scaleDuration);
                });

            ScrollToMainPlayer();
        }
        
        public void SetMainPlayerScoreAnimated(int newScore)
        {
            if (_isAnimating)
                return;

            int oldIndex;
            int targetIndex = CalculateTargetIndex(newScore, out oldIndex);

            if (targetIndex == oldIndex)
            {
                SetMainPlayerScore(newScore);
                return;
            }

            _isAnimating = true;
            if (_btnRank != null)
                _btnRank.interactable = false;

            Vector3 oldWorldPos = _objRankMain.transform.position;

            ApplyNewScoreToMain(newScore);
            UpdateLeaderBoard(); 

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_parentPost);
            Canvas.ForceUpdateCanvases();

            int mainIndexAfterSort = _rankList.FindIndex(t => t.obj == _objRankMain);
            if (mainIndexAfterSort < 0)
                mainIndexAfterSort = targetIndex;

            int targetSiblingIndex = mainIndexAfterSort;

            Vector3 firstTargetPos = _objRankMain.transform.position;

            _targetPlaceholder = SpawnPool.Spawn(
                _objRankBot,
                firstTargetPos,
                Quaternion.identity,
                default,
                _parentPost);

            _targetPlaceholder.transform.SetSiblingIndex(targetSiblingIndex);

            _objRankMain.transform.SetParent(_scrollRect.viewport, true);
            _objRankMain.transform.position = oldWorldPos;

            float targetNormalized = CalculateScrollPositionForIndex(targetSiblingIndex);
            float startNormalized = _scrollRect.verticalNormalizedPosition;

            Sequence seq = Sequence.Create();

            seq.Group(Tween.Custom(
                startNormalized,
                targetNormalized,
                _scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value));

            seq.Group(Tween.Scale(
                _objRankMain.transform,
                Vector3.one * _scaleSize,
                _scrollDuration,
                Ease.OutBack));

            seq.OnComplete(() =>
            {
                if (_targetPlaceholder == null)
                {
                    _isAnimating = false;
                    if (_btnRank != null)
                        _btnRank.interactable = true;
                    return;
                }

                Vector3 currentTargetPos = _targetPlaceholder.transform.position;

                Tween.Position(
                        _objRankMain.transform,
                        currentTargetPos,
                        _moveToSlotDuration,
                        Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        _objRankMain.transform.SetParent(_parentPost, true);
                        _objRankMain.transform.SetSiblingIndex(_targetPlaceholder.transform.GetSiblingIndex());

                        SpawnPool.Despawn(_objRankBot, _targetPlaceholder);
                        _targetPlaceholder = null;

                        Tween.Scale(_objRankMain.transform, Vector3.one, _scaleDuration)
                            .OnComplete(() =>
                            {
                                _isAnimating = false;
                                if (_btnRank != null) _btnRank.interactable = true;
                            });
                    });
            });
        }

        //================= SCROLL HELPER =================

        private float CalculateScrollPositionForIndex(int index)
        {
            RectTransform contentRect = _parentPost as RectTransform;
            RectTransform viewportRect = _scrollRect.viewport;
            RectTransform mainRect = _objRankMain.GetComponent<RectTransform>();

            if (contentRect == null || viewportRect == null || mainRect == null)
                return _scrollRect.verticalNormalizedPosition;

            float elementHeight = mainRect.rect.height;
            float contentHeight = contentRect.rect.height;
            float viewportHeight = viewportRect.rect.height;

            float scrollableHeight = Mathf.Max(contentHeight - viewportHeight, 1f);
            float targetY = index * elementHeight;

            float normalized = 1f - Mathf.Clamp01(targetY / scrollableHeight);
            return normalized;
        }

        private void ScrollToMainPlayer()
        {
            int mainPlayerIndex = -1;
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == _objRankMain)
                {
                    mainPlayerIndex = i;
                    break;
                }
            }

            if (mainPlayerIndex == -1)
                return;

            float targetNormalized = CalculateScrollPositionForIndex(mainPlayerIndex);

            Tween.Custom(
                _scrollRect.verticalNormalizedPosition,
                targetNormalized,
                _scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value);
        }
        
        public void SetBotScore(GameObject botObj, int newScore)
        {
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == botObj)
                {
                    _rankList[i] = (botObj, newScore);
                    break;
                }
            }

            UpdateLeaderBoard();
        }
    }
}

#region  Old Code 
/*
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using UnityEngine.UI;

namespace Sand
{
    public class LeaderBoardList : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject _objRankMain;
        [SerializeField] private GameObject _objRankBot;
        [SerializeField] private Transform _parentPost;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _btnTest;

        [Header("Settings")]
        [SerializeField] private int _poolSize = 20;      
        [SerializeField] private int _score = 999999;     
        [SerializeField] private float _scaleSize = 1.2f;
        [SerializeField] private float _scaleDuration = 0.25f;
        [SerializeField] private float _scrollDuration = 0.5f;
        [SerializeField] private float _moveToSlotDuration = 0.25f;

        [Header("Top Rank Views")]
        [SerializeField] private TopRankView[] _topViews; 

        private List<(GameObject obj, int score)> _rankList;
        private bool _isAnimating;
        private GameObject _targetPlaceholder;

        private void Start()
        {
            InitPool();

            if (_btnTest != null)
                _btnTest.onClick.AddListener(() => SetMainPlayerScoreAnimated(_score));
        }

        private void InitPool()
        {
            SpawnPool.InitPool(_objRankBot, _poolSize, default, _parentPost, true);

            _rankList = new List<(GameObject, int)>();

            for (int i = 0; i < _poolSize; i++)
            {
                var bot = SpawnPool.Spawn(_objRankBot, Vector3.zero, Quaternion.identity, default, _parentPost);
                int randomScore = Random.Range(20000, 5000001);
                _rankList.Add((bot, randomScore));
            }

            _objRankMain.transform.SetParent(_parentPost);
            _rankList.Add((_objRankMain, 0));

            UpdateLeaderBoard();
        }

        private void UpdateLeaderBoard()
        {
            _rankList = _rankList.OrderByDescending(x => x.score).ToList();

            for (int i = 0; i < _rankList.Count; i++)
            {
                var info = _rankList[i].obj.GetComponent<InfoRank>();
                if (info != null)
                {
                    info.Index = i + 1;
                    info.Score = _rankList[i].score;
                    info.SetInfo();
                }

                _rankList[i].obj.transform.SetSiblingIndex(i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_parentPost);
            Canvas.ForceUpdateCanvases();

            UpdateTopViews();
        }

        private void UpdateTopViews()
        {
            if (_topViews == null || _topViews.Length == 0)
                return;

            for (int i = 0; i < _topViews.Length; i++)
            {
                var view = _topViews[i];
                if (view == null)
                    continue;

                if (i < _rankList.Count)
                {
                    var info = _rankList[i].obj.GetComponent<InfoRank>();
                    if (info != null)
                    {
                        view.SetFromInfo(info.Index, info.Score);
                        view.gameObject.SetActive(true);
                    }
                    else
                    {
                        view.gameObject.SetActive(false);
                    }
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }
        }
        private int CalculateTargetIndex(int newScore, out int oldIndex)
        {
            var tempList = _rankList.ToList();

            oldIndex = tempList.FindIndex(t => t.obj == _objRankMain);
            if (oldIndex < 0) oldIndex = 0;
            tempList[oldIndex] = (_objRankMain, newScore);
            tempList = tempList.OrderByDescending(x => x.score).ToList();
            int targetIndex = tempList.FindIndex(t => t.obj == _objRankMain);
            return targetIndex;
        }
        
        private void ApplyNewScoreToMain(int newScore)
        {
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == _objRankMain)
                {
                    _rankList[i] = (_objRankMain, newScore);
                    break;
                }
            }
        }
        
        public void SetMainPlayerScore(int newScore)
        {
            ApplyNewScoreToMain(newScore);
            UpdateLeaderBoard();

            Tween.Scale(_objRankMain.transform, Vector3.one * _scaleSize, _scaleDuration, Ease.OutBack)
                .OnComplete(() =>
                {
                    Tween.Scale(_objRankMain.transform, Vector3.one, _scaleDuration);
                });

            ScrollToMainPlayer();
        }
        
        public void SetMainPlayerScoreAnimated(int newScore)
        {
            if (_isAnimating)
                return;

            int oldIndex;
            int targetIndex = CalculateTargetIndex(newScore, out oldIndex);

            if (targetIndex == oldIndex)
            {
                SetMainPlayerScore(newScore);
                return;
            }

            _isAnimating = true;
            if (_btnTest != null)
                _btnTest.interactable = false;

            Vector3 oldWorldPos = _objRankMain.transform.position;
            ApplyNewScoreToMain(newScore);
            UpdateLeaderBoard(); 

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_parentPost);
            Canvas.ForceUpdateCanvases();

            // tìm index mới của main sau sort
            int mainIndexAfterSort = _rankList.FindIndex(t => t.obj == _objRankMain);
            if (mainIndexAfterSort < 0)
                mainIndexAfterSort = targetIndex;

            int targetSiblingIndex = mainIndexAfterSort;

            Vector3 firstTargetPos = _objRankMain.transform.position;

            // Tạo 1 placeholder đứng ở vị trí rank mới
            _targetPlaceholder = SpawnPool.Spawn(
                _objRankBot,
                firstTargetPos,
                Quaternion.identity,
                default,
                _parentPost);

            _targetPlaceholder.transform.SetSiblingIndex(targetSiblingIndex);

            _objRankMain.transform.SetParent(_scrollRect.viewport, true);
            _objRankMain.transform.position = oldWorldPos;

            float targetNormalized = CalculateScrollPositionForIndex(targetSiblingIndex);
            float startNormalized = _scrollRect.verticalNormalizedPosition;

            Sequence seq = Sequence.Create();

            seq.Group(Tween.Custom(
                startNormalized,
                targetNormalized,
                _scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value));

            seq.Group(Tween.Scale(
                _objRankMain.transform,
                Vector3.one * _scaleSize,
                _scrollDuration,
                Ease.OutBack));

            seq.OnComplete(() =>
            {
                if (_targetPlaceholder == null)
                {
                    _isAnimating = false;
                    if (_btnTest != null)
                        _btnTest.interactable = true;
                    return;
                }

                Vector3 currentTargetPos = _targetPlaceholder.transform.position;

                Tween.Position(
                        _objRankMain.transform,
                        currentTargetPos,
                        _moveToSlotDuration,
                        Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        _objRankMain.transform.SetParent(_parentPost, true);
                        _objRankMain.transform.SetSiblingIndex(_targetPlaceholder.transform.GetSiblingIndex());

                        SpawnPool.Despawn(_objRankBot, _targetPlaceholder);
                        _targetPlaceholder = null;

                        Tween.Scale(_objRankMain.transform, Vector3.one, _scaleDuration)
                            .OnComplete(() =>
                            {
                                _isAnimating = false;
                                if (_btnTest != null)
                                    _btnTest.interactable = true;
                            });
                    });
            });
        }
        
        private float CalculateScrollPositionForIndex(int index)
        {
            RectTransform contentRect = _parentPost as RectTransform;
            RectTransform viewportRect = _scrollRect.viewport;
            RectTransform mainRect = _objRankMain.GetComponent<RectTransform>();

            if (contentRect == null || viewportRect == null || mainRect == null)
                return _scrollRect.verticalNormalizedPosition;

            float elementHeight = mainRect.rect.height;
            float contentHeight = contentRect.rect.height;
            float viewportHeight = viewportRect.rect.height;

            float scrollableHeight = Mathf.Max(contentHeight - viewportHeight, 1f);
            float targetY = index * elementHeight;

            float normalized = 1f - Mathf.Clamp01(targetY / scrollableHeight);
            return normalized;
        }

        private void ScrollToMainPlayer()
        {
            int mainPlayerIndex = -1;
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == _objRankMain)
                {
                    mainPlayerIndex = i;
                    break;
                }
            }

            if (mainPlayerIndex == -1)
                return;

            float targetNormalized = CalculateScrollPositionForIndex(mainPlayerIndex);

            Tween.Custom(
                _scrollRect.verticalNormalizedPosition,
                targetNormalized,
                _scrollDuration,
                value => _scrollRect.verticalNormalizedPosition = value);
        }
        
        public void SetBotScore(GameObject botObj, int newScore)
        {
            for (int i = 0; i < _rankList.Count; i++)
            {
                if (_rankList[i].obj == botObj)
                {
                    _rankList[i] = (botObj, newScore);
                    break;
                }
            }

            UpdateLeaderBoard();
        }
    }
}
*/
#endregion
