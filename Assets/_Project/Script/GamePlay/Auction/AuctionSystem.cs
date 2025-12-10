using UnityEngine;
using Random = UnityEngine.Random;

namespace Sand
{
    public class AuctionSystem : MonoBehaviour
    {
        [Header("People")]
        [SerializeField] private AuctionBox[] _people;

        [Header("Random money")]
        [SerializeField] private int _minMoney = 100;
        [SerializeField] private int _maxMoney = 500;
        
        private int _currentIndex = -1;
        private bool _isFinished = false;

        private void Awake()
        {
            if (_people == null || _people.Length == 0)
                _people = GetComponentsInChildren<AuctionBox>(true);
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void ResetState()
        {
            _isFinished = false;
            _currentIndex = 0;
            foreach (var p in _people)
                p.gameObject.SetActive(true);
            HideAllTalks();
            ShowCurrent();
        }

        private void HideAllTalks()
        {
            foreach (var p in _people)
                p.SetTalkVisible(false);
        }

        private void ShowCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _people.Length) return;

            var box = _people[_currentIndex];
            box.gameObject.SetActive(true);
            box.SetTalkVisible(true);

            int money = Random.Range(_minMoney, _maxMoney + 1);
            box.SetMoney(money);
        }

        public void OnClickNext()
        {
            if (_isFinished) return;
            if (_currentIndex >= 0 && _currentIndex < _people.Length)
                _people[_currentIndex].gameObject.SetActive(false);
            _currentIndex++;
            if (_currentIndex >= _people.Length)
            {
                _isFinished = true;
                LogFinal();
                return;
            }
            ShowCurrent();
        }

        private void LogFinal()
        {
            int lastMoney = _people[_people.Length - 1].Money;
            Debug.Log($"[Auction] Done! Last money shown: {lastMoney}");
        }
    }
}
