using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using Sand;
using TMPro;
using UnityEngine;
using Zenject;

public class GemsView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _gemsTextMenu;
    [SerializeField] private TextMeshProUGUI _gemsTextShop;
    [Inject] private UserData _userData;
    IDisposable _sub;

    private int _currentGems;

    private void Start()
    {
        _currentGems = _userData.GemsValue;
        _gemsTextMenu.text = NumberFormat.Format(_currentGems);
        _gemsTextShop.text = NumberFormat.Format(_currentGems);

        _sub = _userData.Gems.Subscribe(value =>
        {
            AnimText.AnimateNumberChange(_gemsTextMenu, _currentGems, value, 0.5f, 2, _gemsTextMenu.gameObject).Forget();
            AnimText.AnimateNumberChange(_gemsTextShop, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject).Forget();
            _currentGems = value;
        });
    }

    private void OnDestroy()
    {
        _sub?.Dispose();
    }
}