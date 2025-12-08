using UnityEngine;

namespace Sand
{
    public class PopupShopInGame : MonoBehaviour
    {
        [SerializeField] private GameObject _popupShopInGame;
        
        public void OpenShopInGame()
        {
            _popupShopInGame.SetActive(true);
        }
        public void CloseShopInGame()
        {
            _popupShopInGame.SetActive(false);
        }
    }
}