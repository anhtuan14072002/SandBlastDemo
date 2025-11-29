using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class GemsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gemsTextMenu;
        [SerializeField] private TextMeshProUGUI _gemsTextShop;
        [Inject] private UserData _userData;
        IDisposable _sub;

        private void Start()
        {
            var currentGems = _userData.Gems.Value;
            _sub = _userData.Gems.Subscribe(value =>
            {
                AnimText.AnimateNumberChange(_gemsTextMenu, (int)currentGems, (int)value, 0.5f, 2,
                    _gemsTextMenu.gameObject).Forget();
                AnimText.AnimateNumberChange(_gemsTextShop, (int)currentGems, (int)value, 0.5f, 2,
                    _gemsTextShop.gameObject).Forget();
            });
        }
        
        private void OnDestroy()
        {
            _sub?.Dispose();
        }
    }
}