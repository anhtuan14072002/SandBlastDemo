using Core;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class PopupAuction : GameElement,
        IReceive<SignalActiveLockAuction>,
        IReceive<SignalActivePopupAuction>
    {
        [SerializeField] private GameObject _popupAuction;
        [SerializeField] private GameObject _objLock;
        [SerializeField] private AuctionSate _auctionState;

        private bool _isAuction;
        public void OpenAuction()
        {
            _popupAuction.SetActive(true);
        }

        public void CloseAuction()
        {
            _popupAuction.SetActive(false);
            // _renderPicture.OpenMapArt();
        }

        public void Receive(in SignalActiveLockAuction signal)
        {
            if (_objLock != null)
                _objLock.SetActive(signal.IsActive);
        }

        public void Receive(in SignalActivePopupAuction signal)
        {
            if (signal.IsActive) OpenAuction();
            else CloseAuction();
        }
    }
}