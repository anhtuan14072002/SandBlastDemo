using System;
using Core;
using TMPro;
using UnityEngine;

namespace Sand
{
    public class Score : Visual,
        IReceive<SignalCountScore>,
        IReceive<SignalUpdateHighScore>
    {
        [SerializeField] private TextMeshProUGUI _textScore;
        [SerializeField] private TextMeshProUGUI _textHighScore;
        [SerializeField] private ScriptableScore _scriptableScore;

        private int _score = 0;

        private void Start()
        {
            _score = 0;
            _textScore.text = "0";
            _textHighScore.text = _scriptableScore._highScore.ToString();
        }

        public void Receive(in SignalCountScore signal)
        {
            CountScore(signal.Amout);
        }
        public void Receive(in SignalUpdateHighScore signal)
        {
            UpdateHighScore();
        }
        public void CountScore(int score)
        {
            _score+=score;
            _textScore.text = _score.ToString();
        }
        private void UpdateHighScore()
        {
            if (_score > _scriptableScore.HighScore)
            {
                _scriptableScore._highScore = _score;
                _scriptableScore.HighScore = _score;
            }
            
            if (_textHighScore != null)
            {
                _textHighScore.text = _scriptableScore._highScore.ToString();
            }
        }
    }
}