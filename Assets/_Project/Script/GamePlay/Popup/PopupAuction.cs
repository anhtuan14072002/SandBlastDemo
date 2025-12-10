using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class PopupAuction : MonoBehaviour
    {
        [SerializeField] private GameObject _popupAuction;
        [SerializeField] private GameObject _objLock;
        [SerializeField] private Button _btnAuction;
        private bool _isAuction;

        public void OpenAuction()
        {
            _popupAuction.SetActive(true);
        }

        public void CloseAuction()
        {
            _isAuction = false;
            _popupAuction.SetActive(false);
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