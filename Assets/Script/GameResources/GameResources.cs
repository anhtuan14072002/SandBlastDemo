using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class GameResources : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _textPriceBuyGem;
        [SerializeField] private TextMeshProUGUI[] _textAmoutGems;
        [SerializeField] private Button[] _btnBuyGem;
        [SerializeField] private int[] _priceBuyGem;
        [SerializeField] private int[] _amountGem;

        [Inject] UserData _userData;
        [Inject] RewardSystem _rewardSystem;

        private void Start()
        {
            _btnBuyGem[0].onClick.AddListener(BuyNoAds);
            
            for (int i = 1; i < _btnBuyGem.Length; i++)
            {
                int index = i; 
                _btnBuyGem[i].onClick.AddListener(() => BuyGems(_amountGem[index]));
            }
            
            for (int i = 0; i < _textPriceBuyGem.Length; i++)
            {
                _textPriceBuyGem[i].text = _priceBuyGem[i].ToString();
            }
            
            for (int i = 1; i < _textAmoutGems.Length; i++)
            {
                _textAmoutGems[i].text = _amountGem[i].ToString();
            }
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