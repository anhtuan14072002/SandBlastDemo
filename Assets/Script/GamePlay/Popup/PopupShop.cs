using UnityEngine;

namespace Sand
{
    public class PopupShop : MonoBehaviour
    {
        [SerializeField] private GameObject _shopPanel;
        
        public void OpenShop()
        {
            _shopPanel.SetActive(true);
        }

        public void CloseShop()
        {
            _shopPanel.SetActive(false);
        }
    }
}