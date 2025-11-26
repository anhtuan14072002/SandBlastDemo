using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class BoomView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textAmountBoom;
        UserData _userData;
        IDisposable _sub;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }

        private void Start()
        {
            _sub = _userData.Boom.Subscribe(value =>
            {
                _textAmountBoom.text = _userData.Boom.Value.ToString();
                /*AnimText.AnimateNumberChange(_textAmountBoom, currentAmountBoom, (int)value, 0.5f, 2,
                    _textAmountBoom.gameObject).Forget();*/
            });
        }

        private void OnDestroy()
        {
            _sub?.Dispose();
        }
    }
}