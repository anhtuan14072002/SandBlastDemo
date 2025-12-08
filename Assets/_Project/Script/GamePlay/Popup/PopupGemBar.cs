using Core;
using UnityEngine;

namespace Sand
{
    public class PopupGemBar : GameElement,
        IReceive<SignalToggleGemBarMenu>,
        IReceive<SignalToggleGemBarInGame>
    {
        [SerializeField] private GameObject _gemBarMenu;
        [SerializeField] private GameObject _gemBarInGame;

        public void ToggleGemBarMenu(bool isActivate)
        {
            _gemBarMenu.SetActive(isActivate);
        }
        public void ToggleGemBarInGame(bool isActivate)
        {
            _gemBarInGame.SetActive(isActivate);
        }

        public void Receive(in SignalToggleGemBarMenu signal)
        {
            ToggleGemBarMenu(signal.IsActivate);
        }

        public void Receive(in SignalToggleGemBarInGame signal)
        {
            ToggleGemBarInGame(signal.IsActivate);
        }
    }
}