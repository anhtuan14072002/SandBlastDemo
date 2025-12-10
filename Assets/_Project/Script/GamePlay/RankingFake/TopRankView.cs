using TMPro;
using UnityEngine;

namespace Sand
{
    public class TopRankView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textName; 
        [SerializeField] private TextMeshProUGUI _textScore; 
        
        public void SetFromInfo(string playerName, int score)
        {
            if (_textName != null) _textName.text = playerName;
            if (_textScore != null) _textScore.text = score.ToString();
        }
    }
}