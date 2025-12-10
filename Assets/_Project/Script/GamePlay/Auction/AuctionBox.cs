using TMPro;
using UnityEngine;

namespace Sand
{
    public class AuctionBox : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject _talkObj;
        [SerializeField] private TextMeshProUGUI _textMoney; 
        private int _money;
        public int Money => _money;
        public void SetMoney(int value)
        {
            _money = value;
            if (_textMoney != null) _textMoney.text = _money.ToString();
        }

        public void SetTalkVisible(bool visible)
        {
            if (_talkObj != null) _talkObj.SetActive(visible);
        }
    }
}