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
        [Inject] RewardSystem _rewardSystem;
        private int _score = 0;
        
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
            _rewardSystem.AddScore(_score);
        }

        public void Receive(in SignalRestCurrenScore signal)
        {
            _scoreText.text = "0";
        }
    }
}