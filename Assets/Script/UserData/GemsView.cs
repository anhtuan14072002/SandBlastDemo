using System;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class GemsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gemsText;
        [Inject] private UserData _userData;
        private IDisposable _sub;
        
        private void Start()
        {
            _sub = _userData.Gems.Subscribe(value =>
            {
                _gemsText.text = value.ToString("0"); 
            });
        }

        private void OnDestroy()
        {
            _sub?.Dispose();
        }
    }
}