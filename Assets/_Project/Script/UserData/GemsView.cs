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
    [SerializeField] private TextMeshProUGUI _gemsTextMenuInGame;
    [SerializeField] private TextMeshProUGUI _gemsTextShop;
    [SerializeField] private TextMeshProUGUI _gemsTextShopInGame;
    [SerializeField] private TextMeshProUGUI _gemsTextArt;
    [SerializeField] private TextMeshProUGUI _gemsTextCollection;
    [SerializeField] private TextMeshProUGUI _gemsTextAuction;
    [Inject] private UserData _userData;
    IDisposable _sub;

    private int _currentGems;

    private void Start()
    {
        _currentGems = _userData.GemsValue;
        // _gemsTextMenu.text = NumberFormat.Format(_currentGems);
        // _gemsTextShop.text = NumberFormat.Format(_currentGems);
        _gemsTextMenu.text = _currentGems.ToString();
        _gemsTextMenuInGame.text = _currentGems.ToString();
        _gemsTextShop.text = _currentGems.ToString();
        _gemsTextShopInGame.text = _currentGems.ToString();
        _gemsTextArt.text = _currentGems.ToString();
        _gemsTextCollection.text = _currentGems.ToString();
        _gemsTextAuction.text = _currentGems.ToString();
        
        _sub = _userData.Gems.Subscribe(value =>
        {
            AnimText.AnimateNumberChange(_gemsTextMenu, _currentGems, value, 0.5f, 2, _gemsTextMenu.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextShop, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextMenuInGame, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextShopInGame, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextArt, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextCollection, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true).Forget();
            AnimText.AnimateNumberChange(_gemsTextAuction, _currentGems, value, 0.5f, 2, _gemsTextShop.gameObject, true, 1.5f).Forget();
            _currentGems = value;
        });
    }
    
    private void OnDestroy()
    {
        _sub?.Dispose();
    }
}