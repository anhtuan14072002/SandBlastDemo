using Core;
using UnityEngine;

namespace Sand
{
    public class GemBar : GameElement,
        IReceive<SignalOpenGemBarIngame>,
        IReceive<SignalCloseGemBarIngame>
    {
         [SerializeField] private GameObject _gemBarInGame;
         [SerializeField] private GameObject _gemBarMenu;
         [SerializeField] private GameObject _highScoreBar;

        public void OpenGemBarInGame()
        {
            _gemBarInGame.gameObject.SetActive(true);
            _gemBarMenu.gameObject.SetActive(false);
            _highScoreBar.gameObject.SetActive(false);
        }

        public void CloseGemBarInGame()
        {
            _gemBarInGame.gameObject.SetActive(false);
            _gemBarMenu.gameObject.SetActive(true);
            _highScoreBar.gameObject.SetActive(true);
        }

        public void Receive(in SignalOpenGemBarIngame signal)
        {
            OpenGemBarInGame();
        }

        public void Receive(in SignalCloseGemBarIngame signal)
        {
            CloseGemBarInGame();
        }
    }
}