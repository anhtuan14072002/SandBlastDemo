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
        private RewardSystem _rewardSystem;
        public int _score = 0;

        [Inject]
        void Construct(RewardSystem rewardSystem)
        {
            _rewardSystem = rewardSystem;
        }
        
        public void Receive(in SignalScoreOnGame signal)
        {
            UpdateScore(signal.Score);
        }

        private void UpdateScore(int score)
        {
            var currentScore = _score;
            _score += score;
            _scoreText.text = score.ToString();
            AnimText.AnimateNumberChange(_scoreText, currentScore, _score, 0.5f, 8,_scoreText.gameObject).Forget();
            _scoreTextPopupGameOver.text = _score.ToString();
            _rewardSystem.AddScore(_score);
        }
        public void ResetScore()
        {
            _score = 0;
            _scoreText.text = "0";
            _scoreTextPopupGameOver.text = "0";
        }

        public void Receive(in SignalRestCurrenScore signal)
        {
            ResetScore();
        }
    }
}