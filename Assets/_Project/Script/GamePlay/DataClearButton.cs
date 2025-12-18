using Zenject;
using UnityEngine;

namespace Sand
{
    public class DataClearButton : MonoBehaviour
    {
        [Inject] private UserData _userData;
        [Inject] private SaveService _saveService;

        public void OnClearDataButtonClicked()
        {
            if (ConfirmDelete())
            {
                _userData.ClearAllData();
                _saveService.DeleteAllSavedData();
                Debug.Log("Delete all data");
            }
        }

        private bool ConfirmDelete()
        {
            return true;
        }
    }
}