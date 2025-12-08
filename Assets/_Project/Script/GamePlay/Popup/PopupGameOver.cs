using Core;
using HadesSDK.Ads.Runtime;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class PopupGameOver : GameElement,
        IReceive<SignalClosePopupGameOver>,
        IReceive<SignalOpenPopupGameOver>
    {
        [SerializeField] private GameObject _popupGameOver;
        [Inject] RenderMap _renderMap;
        [Inject] CountDrawData _countDrawData;
        [Inject] UserData _userData;


        public void OpenPopupGameOver()
        {
            _popupGameOver.SetActive(true);
            _countDrawData.CountDrawPicture();
            _userData.CountDrawInGame.Value = 0;
            AdManager.Instance.HideBanner();
            AdManager.Instance.ShowMrec();
        }

        public void ClosePopupGameOver()
        {
            if (_renderMap != null) _renderMap.Reset();
            if (_popupGameOver.activeSelf)
            {
                _popupGameOver.SetActive(false);
                AdManager.Instance.HideMrec();
                AdManager.Instance.ShowBanner();
            }
        }

        public void Receive(in SignalOpenPopupGameOver signal)
        {
            OpenPopupGameOver();
        }

        public void Receive(in SignalClosePopupGameOver signal)
        {
            ClosePopupGameOver();
        }
    }
}