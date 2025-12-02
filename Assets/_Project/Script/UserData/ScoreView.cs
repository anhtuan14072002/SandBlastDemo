using System;
using Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class ScoreView : GameElement,
        IReceive<SignalScoreOnGame>,
        IReceive<SignalRestCurrenScore>
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _scoreTextPopupGameOver;
        private int _score ;
        
        ScoreData _scoreData;
        UserData _userData;
        SaveService _saveService;
        
        [Inject]
        void Construct(ScoreData scoreData, UserData userData, SaveService saveService)
        {
            _scoreData = scoreData;
            _userData = userData;
            _saveService = saveService;
        }

        private void Start()
        {
            _score = _userData.CurrentScoreValue;
            // _scoreText.text = NumberFormat.Format(_score);
            _scoreText.text = _score.ToString();
        }

        public void Receive(in SignalScoreOnGame signal)
        {
            UpdateScore(signal.Score);
        }

        private void UpdateScore(int score)
        {
            var startScore = _score;
            _score += score;
            // _scoreText.text = _score.ToString();
            _userData.CurrentScore.Value = _score;
            AnimText.AnimateNumberChange(_scoreText, startScore, _score, 0.5f, 8, _scoreText.gameObject).Forget();
            // _scoreTextPopupGameOver.text = NumberFormat.Format(_score);
            _scoreTextPopupGameOver.text = _score.ToString();
            _scoreData.CheckHighScore(_score);
        }

        public void ResetScore()
        {
            _score = 0;
            _scoreData.ResetCurrentScore();
            _scoreText.text = "0";
            _scoreTextPopupGameOver.text = "0";
        }

        public void Receive(in SignalRestCurrenScore signal)
        {
            ResetScore();
        }
    }
}