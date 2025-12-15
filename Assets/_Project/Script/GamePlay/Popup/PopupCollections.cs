using Core;
using UnityEngine;

namespace Sand
{
    public struct SignalOpenPopupCollections { }
    public class PopupCollections : GameElement,
        IReceive<SignalClosePopupCollections>,
        IReceive<SignalOpenPopupCollections>,
        IReceive<SignalTogglePopupDraw>
    {
        [SerializeField] private GameObject _popupCollections;
        [SerializeField] private GameObject _popupDraw;
        [SerializeField] private GameObject _popupDrawInGame;
        [SerializeField] private SpriteRenderer _spriteDraw;
        
        public void OpenPopupCollection()
        {
            _popupCollections.SetActive(true);
            SetOrderCloseDraw();
        }

        public void ClosePopupCollection()
        {
            _popupCollections.SetActive(false);
        }
        public void OpenPopupDraw()
        {
            _popupDrawInGame.SetActive(true);
            _popupDraw.SetActive(true);
        }
        public void ClosePopupDraw()
        {
            _popupDrawInGame.SetActive(false);
            _popupDraw.SetActive(false);
        }
        public void SetOrderOpenDraw()
        {
            _spriteDraw.sortingOrder = 99;
        }
        public void SetOrderCloseDraw()
        {
            _spriteDraw.sortingOrder = 0;
        }
        public void Receive(in SignalClosePopupCollections signal)
        {
            ClosePopupCollection();
        }
        
        public void Receive(in SignalOpenPopupCollections signal)
        {
            OpenPopupCollection();
        }
        
        public void Receive(in SignalTogglePopupDraw signal)
        {
            if (signal.IsActive)
            {
                ClosePopupCollection();
                OpenPopupDraw();
                SetOrderOpenDraw();
            }
            else
            {
                SetOrderCloseDraw();
                ClosePopupDraw();
            }
        }
    }
}