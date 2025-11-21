using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class HighScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _highScoreText;
        [Inject] private UserData _userData;
        private int _currentHighScore = 0;
        private float _originalFontSize;
        IDisposable _sub;

        private void Start()
        {
            _originalFontSize = _highScoreText.fontSize;
            _currentHighScore = _userData.HighScore.Value;
            _highScoreText.text = _currentHighScore.ToString();
            AdjustFontSize(_currentHighScore);

            _sub = _userData.HighScore.Subscribe(value =>
            {
                AnimText.AnimateNumberChange(_highScoreText, _currentHighScore, value, 0.5f, 2,
                        _highScoreText.gameObject)
                    .Forget();
                _currentHighScore = value;
                AdjustFontSize(value);
            });
        }

        private void AdjustFontSize(int score)
        {
            if (score > 99999999)
            {
                _highScoreText.fontSize = 30f;
            }
            else
            {
                _highScoreText.fontSize = _originalFontSize;
            }
        }

        private void OnDestroy()
        {
            _sub?.Dispose();
        }
    }
}