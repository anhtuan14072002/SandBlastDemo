using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameResources : MonoBehaviour
    {
        [SerializeField] private Button[] _btnBuyGem;
        [SerializeField] private int[] _priceBuyGem;
        [SerializeField] private int[] _amountGem;
        [SerializeField] private TextMeshProUGUI[] _textPriceBuyGem;
        [SerializeField] private TextMeshProUGUI[] _textAmoutGems;

        [Inject] UserData _userData;
        [Inject] RewardSystem _rewardSystem;

        private void Start()
        {
            _btnBuyGem[0].onClick.AddListener(BuyNoAds);
            _btnBuyGem[1].onClick.AddListener(() => BuyGems(_amountGem[1]));
            _btnBuyGem[2].onClick.AddListener(() => BuyGems(_amountGem[2]));
            _btnBuyGem[3].onClick.AddListener(() => BuyGems(_amountGem[3]));
            _btnBuyGem[4].onClick.AddListener(() => BuyGems(_amountGem[4]));
            _btnBuyGem[5].onClick.AddListener(() => BuyGems(_amountGem[5]));
            
            _textPriceBuyGem[0].text = _priceBuyGem[0].ToString();
            _textPriceBuyGem[1].text = _priceBuyGem[1].ToString();
            _textPriceBuyGem[2].text = _priceBuyGem[2].ToString();
            _textPriceBuyGem[3].text = _priceBuyGem[3].ToString();
            _textPriceBuyGem[4].text = _priceBuyGem[4].ToString();
            _textPriceBuyGem[5].text = _priceBuyGem[5].ToString();
            
            _textAmoutGems[1].text = _amountGem[1].ToString();
            _textAmoutGems[2].text = _amountGem[2].ToString();
            _textAmoutGems[3].text = _amountGem[3].ToString();
            _textAmoutGems[4].text = _amountGem[4].ToString();
            _textAmoutGems[5].text = _amountGem[5].ToString();
        }

        public void BuyNoAds()
        {
            Debug.Log("BuyNoAds");
        }

        private void BuyGems(int gems)
        {
            _rewardSystem.AddGems(gems);
        }
    }
}