using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class MagicBrushView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textAmountMagicBrush;
        UserData _userData;
        IDisposable _sub;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }
        
        private void Start()
        {
            _sub = _userData.MagicBrush.Subscribe(value =>
            {
                _textAmountMagicBrush.text = _userData.MagicBrush.Value.ToString();
                /*AnimText.AnimateNumberChange(_textAmountMagicBrush, currentAmountMagicBrush, (int)value, 0.5f, 2,
                    _textAmountMagicBrush.gameObject).Forget();*/
            });
        }
        
        private void OnDestroy()
        {
            _sub?.Dispose();
        }
    }
}