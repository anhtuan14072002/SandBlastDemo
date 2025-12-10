using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class PopupAuction : MonoBehaviour
    {
        [SerializeField] private GameObject _popupAuction;
        [SerializeField] private GameObject _objLock;
        [SerializeField] private Button _btnAuction;
        private bool _isAuction;
        
        RenderPicture _renderPicture;

        [Inject]
        void Construct(RenderPicture renderPicture)
        {
            _renderPicture = renderPicture;
        }

        public void OpenAuction()
        {
            _popupAuction.SetActive(true);
            _renderPicture.CloseMapArt();
        }

        public void CloseAuction()
        {
            _popupAuction.SetActive(false);
            _renderPicture.OpenMapArt();
        }

        public void OpenLock()
        {
            _objLock.SetActive(false);
        }

        public void CloseLock()
        {
            _objLock.SetActive(true);
        }
    }
}