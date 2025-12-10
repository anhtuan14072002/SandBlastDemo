using TMPro;
using UnityEngine;

namespace Sand
{
    public class InfoRank : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _textIndex;
        [SerializeField] private TextMeshProUGUI _textScore;
        [SerializeField] private TextMeshProUGUI _textName;

        [Header("Data")]
        public string PlayerName; 
        public int Index;
        public int Score;

        public void SetInfo()
        {
            if (_textIndex != null)
                _textIndex.text = Index.ToString();
            if (_textScore != null)
                _textScore.text = Score.ToString();
            if (_textName != null)
                _textName.text = PlayerName;
        }
    }
}