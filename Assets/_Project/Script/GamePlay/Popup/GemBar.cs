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

        public void OpenGemBarInGame()
        {
            _gemBarInGame.gameObject.SetActive(true);
            _gemBarMenu.gameObject.SetActive(false);
        }

        public void CloseGemBarInGame()
        {
            _gemBarInGame.gameObject.SetActive(false);
            _gemBarMenu.gameObject.SetActive(true);
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