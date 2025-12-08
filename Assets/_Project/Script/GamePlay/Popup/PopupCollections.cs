using Core;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class PopupCollections : GameElement,
        IReceive<SignalClosePopupCollections>,
        IReceive<SignalTogglePopupArt>
    {
        [SerializeField] private GameObject _popupCollections;
        [SerializeField] private GameObject _popupArt;
        [Inject] RenderPicture _renderPicture;
        public void OpenPopupCollection()
        {
            _popupCollections.SetActive(true);
        }
        public void ClosePopupCollection()
        {
            _popupCollections.SetActive(false);
        }

        public void Receive(in SignalClosePopupCollections signal)
        {
            ClosePopupCollection();
        }

        public void Receive(in SignalTogglePopupArt signal)
        {
            _popupArt.SetActive(signal.IsActive);
        }
    }
}