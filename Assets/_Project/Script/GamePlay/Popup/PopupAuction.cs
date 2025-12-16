using Core;
using UnityEngine;

namespace Sand
{
    public class PopupAuction : GameElement,
        IReceive<SignalActivePopupAuction>
    {
        [SerializeField] private GameObject _popupAuction;
        [SerializeField] private GameObject _objLock;
        [SerializeField] private AuctionSate _auctionState;

        private bool _isAuction;
        public void OpenAuction()
        {
            _popupAuction.SetActive(true);
            Global.Send(new SignalOpenPopupCollections());
        }
        public void CloseAuction()
        {
            _popupAuction.SetActive(false);
        }
        
        public void Receive(in SignalActivePopupAuction signal)
        {
            if (signal.IsActive) OpenAuction();
            else CloseAuction();
        }
    }
}