using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Sand
{
    public class PowerUp : MonoBehaviour
    {
        [SerializeField] private GameObject _popupSkillMagicBrush;
        [SerializeField] private GameObject _popupSkillBoom;
        [SerializeField] private GameObject _popupConfirmBuyMagicBrush;
        [SerializeField] private GameObject _popupConfirmBuyBoom;
        [SerializeField] private TextMeshProUGUI _textPriceMagicBrush;
        [SerializeField] private TextMeshProUGUI _textPriceBoom;
        [SerializeField] private Button _btnOpenSkillMagicBrush;
        [SerializeField] private Button _btnCloseSkillMagicBrush;
        [SerializeField] private Button _btnCloseSkillBoom;
        [SerializeField] private Button _btnBuyMagicBrush;
        [SerializeField] private Button _btnConfirmMagicBrush;
        [SerializeField] private Button _btnConfirmBoom;
        [SerializeField] private Button _btnBuyBoom;
        [SerializeField] private int _priceSkillMagicBrush;
        [SerializeField] private int _priceSkillBoom;

        [Header("Button Use Skill")] [SerializeField]
        private Button _btnUseMagicBrush;

        [SerializeField] private Button _buttonUseBoom;

        private bool _isUseBoom;

        PowerUpSystem _powerUpSystem;
        RewardSystem _rewardSystem;
        UserData _userData;
        IDisposable _mouseClickSubWave;

        [Inject]
        void Construct(UserData userData, RewardSystem rewardSystem, PowerUpSystem powerUpSystem)
        {
            _userData = userData;
            _rewardSystem = rewardSystem;
            _powerUpSystem = powerUpSystem;
        }

        private void Start()
        {
            _textPriceMagicBrush.text = _priceSkillMagicBrush.ToString();
            _textPriceBoom.text = _priceSkillBoom.ToString();

            _btnBuyMagicBrush.onClick.AddListener(OpenPopupConfirmBuyMagicBrush);
            _btnBuyBoom.onClick.AddListener(OpenPopupConfirmBuyBoom);

            _btnConfirmMagicBrush.onClick.AddListener(BuyMagicBrush);
            _btnConfirmBoom.onClick.AddListener(BuyBoom);

            _btnOpenSkillMagicBrush.onClick.AddListener(OpenPopupMagicBrush);
            _btnCloseSkillMagicBrush.onClick.AddListener(UsePowerUpMagicBrush);
            _btnCloseSkillBoom.onClick.AddListener(ClosePowerUpBoom);

            _btnUseMagicBrush.onClick.AddListener(UseMagicBrush);
            _buttonUseBoom.onClick.AddListener(UseBoom);
            //---Boom
        }

        //------------Boom-----------------//
        private void BuyBoom()
        {
            if (_userData.Gems.Value >= _priceSkillBoom)
            {
                _userData.Gems.Value -= _priceSkillBoom;
                _rewardSystem.AddBoom(1);
                ClosePopupConfirmBuyBoom();
            }
            else
            {
                Global.Send(new SignalOpenEffectNotEnough());
            }
        }

        private void UseBoom()
        {
            if (_userData.Boom.Value > 0)
            {
                OpenSkillBoom();
                _isUseBoom = true;
                if (_mouseClickSubWave != null)
                    _mouseClickSubWave.Dispose();

                _mouseClickSubWave = Observable.EveryUpdate()
                    .Where(_ => Input.GetMouseButtonDown(0) && _isUseBoom)
                    .Subscribe(_ => { MouseClickBoom(); });
            }
            else
            {
                Debug.Log("No boom");
            }
        }

        private void OpenSkillBoom()
        {
            _popupSkillBoom.SetActive(true);
        }

        private void ClosePowerUpBoom()
        {
            _isUseBoom = false;
            _popupSkillBoom.SetActive(false);
            _mouseClickSubWave?.Dispose();
        }

        private void MouseClickBoom()
        {
            if (_userData.Boom.Value > 0 && _isUseBoom)
            {
                bool hasEffect = _powerUpSystem.PowerUpBoom();
                if (hasEffect)
                {
                    _rewardSystem.DeductBoom(1);
                    if (_userData.Boom.Value <= 0) ClosePowerUpBoom();
                }
            }
        }
        //------------------------------------//

        //---------MagicBrush-------------//
        private void BuyMagicBrush()
        {
            if (_userData.Gems.Value >= _priceSkillMagicBrush)
            {
                _userData.Gems.Value -= _priceSkillMagicBrush;
                _rewardSystem.AddMagicBrush(1);
                ClosePopupConfirmBuyMagicBrush();
            }
            else
            {
                Global.Send(new SignalOpenEffectNotEnough());
            }
        }

        private void UseMagicBrush()
        {
            if (_userData.MagicBrush.Value > 0)
            {
                OpenPopupMagicBrush();
                _rewardSystem.DeductMagicBrush(1);
                RemoveSameColorCompleteBands();
            }
            else
            {
                Debug.Log("Not enough magic brush");
            }
        }

        private void OpenPopupMagicBrush()
        {
            if (_userData.MagicBrush.Value > 0)
            {
                _popupSkillMagicBrush.SetActive(true);
            }
            else
            {
                Debug.Log("Not enough magic brush");
            }
        }

        private void UsePowerUpMagicBrush()
        {
            _popupSkillMagicBrush.SetActive(false);
        }

        //-------------------------------------//

        //-------------PopupBuyPowerUp-----------//
        private void OpenPopupConfirmBuyMagicBrush()
        {
            _popupConfirmBuyMagicBrush.SetActive(true);
        }

        public void ClosePopupConfirmBuyMagicBrush()
        {
            _popupConfirmBuyMagicBrush.SetActive(false);
        }

        private void OpenPopupConfirmBuyBoom()
        {
            _popupConfirmBuyBoom.SetActive(true);
        }

        public void ClosePopupConfirmBuyBoom()
        {
            _popupConfirmBuyBoom.SetActive(false);
        }

        //-------------------------------------//
        private void RemoveSameColorCompleteBands()
        {
            UsePowerUpMagicBrush();
            _powerUpSystem.PowerUpMagicBrush(_powerUpSystem._colorMagic.color).Forget();
        }

        public void ResetGem()
        {
            _rewardSystem.AddGems(0);
        }
        private void OnDestroy()
        {
            _btnBuyMagicBrush.onClick.RemoveListener(BuyMagicBrush);
            _btnBuyBoom.onClick.RemoveListener(BuyBoom);
            _mouseClickSubWave?.Dispose();
        }
    }
}