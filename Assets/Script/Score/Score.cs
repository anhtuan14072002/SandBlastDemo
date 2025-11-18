using Core;
using TMPro;
using UnityEngine;

namespace Sand
{
    public class Score : Visual,IReceive<SignalCountScore>
    {
        [SerializeField] private TextMeshProUGUI _textScore;

        public void Receive(in SignalCountScore signal)
        {
            CountScore(signal.Amout);
        }
        
        public void CountScore(int score)
        {
            var currentCount = int.Parse(_textScore.text);
            var totalCount = currentCount + score;
            _textScore.text = totalCount.ToString();
        }
    }
}