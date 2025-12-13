/*
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
        [Header("Popups")]
        [SerializeField] private GameObject _popupSkillMagicBrush;
        [SerializeField] private GameObject _popupSkillBoom;
        [SerializeField] private GameObject _popupConfirmBuyMagicBrush;
        [SerializeField] private GameObject _popupConfirmBuyBoom;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _textPriceMagicBrush;
        [SerializeField] private TextMeshProUGUI _textPriceBoom;

        [Header("Buttons")]
        [SerializeField] private Button _btnCloseSkillMagicBrush;
        [SerializeField] private Button _btnCloseSkillBoom;
        [SerializeField] private Button _btnBuyMagicBrush;
        [SerializeField] private Button _btnConfirmMagicBrush;
        [SerializeField] private Button _btnConfirmBoom;
        [SerializeField] private Button _btnBuyBoom;
        [SerializeField] private Button _btnUseMagicBrush; // trong popup MagicBrush (nếu còn dùng)

        [Header("Prices")]
        [SerializeField] private int _priceSkillMagicBrush;
        [SerializeField] private int _priceSkillBoom;

        private bool _isUseBoom;

        private PowerUpSystem _powerUpSystem;
        private RewardSystem _rewardSystem;
        private UserData _userData;

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
            if (_textPriceMagicBrush != null)
                _textPriceMagicBrush.text = _priceSkillMagicBrush.ToString();

            if (_textPriceBoom != null)
                _textPriceBoom.text = _priceSkillBoom.ToString();

            //--------Boom---------//
            if (_btnBuyBoom != null)
                _btnBuyBoom.onClick.AddListener(OpenPopupConfirmBuyBoom);

            if (_btnConfirmBoom != null)
                _btnConfirmBoom.onClick.AddListener(BuyBoom);

            if (_btnCloseSkillBoom != null)
                _btnCloseSkillBoom.onClick.AddListener(ClosePowerUpBoom);

            //-----MagicBrush----//
            if (_btnBuyMagicBrush != null)
                _btnBuyMagicBrush.onClick.AddListener(OpenPopupConfirmBuyMagicBrush);

            if (_btnConfirmMagicBrush != null)
                _btnConfirmMagicBrush.onClick.AddListener(BuyMagicBrush);

            if (_btnUseMagicBrush != null)
                _btnUseMagicBrush.onClick.AddListener(UsePowerUpMagicBrush);

            if (_btnCloseSkillMagicBrush != null)
                _btnCloseSkillMagicBrush.onClick.AddListener(ClosePopupMagicBrush);
        }

        //================ ENTRY FROM WORLD ICON ===================//

        public void OnClickPowerUpIcon(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Boom:
                    UsePowerUpBoom();
                    break;

                case PowerUpType.MagicBrush:
                    OpenPopupMagicBrush();
                    break;
            }
        }

        //===================== BOOM =======================//

        private void BuyBoom()
        {
            if (_userData.GemsValue >= _priceSkillBoom)
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

        private void UsePowerUpBoom()
        {
            if (_userData.BoomValue > 0)
            {
                OpenSkillBoom();
                _isUseBoom = true;

                _mouseClickSubWave?.Dispose();
                _mouseClickSubWave = Observable.EveryUpdate()
                    .Where(_ => Input.GetMouseButtonDown(0) && _isUseBoom)
                    .Subscribe(_ => { MouseClickBoom(); });
            }
            else
            {
                Global.Send(new SignalOpenEffectNotEnough());
            }
        }

        private void OpenSkillBoom()
        {
            if (_popupSkillBoom != null)
                _popupSkillBoom.SetActive(true);
        }

        private void ClosePowerUpBoom()
        {
            _isUseBoom = false;

            if (_popupSkillBoom != null)
                _popupSkillBoom.SetActive(false);

            _mouseClickSubWave?.Dispose();
        }

        private void MouseClickBoom()
        {
            if (_userData.BoomValue > 0 && _isUseBoom)
            {
                bool hasEffect = _powerUpSystem.PowerUpBoom();
                if (hasEffect)
                {
                    _rewardSystem.DeductBoom(1);
                    if (_userData.BoomValue <= 0)
                        ClosePowerUpBoom();
                }
            }
        }

        //===================== MAGIC BRUSH =======================//

        private void BuyMagicBrush()
        {
            if (_userData.GemsValue >= _priceSkillMagicBrush)
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

        private void OpenPopupMagicBrush()
        {
            if (_userData.MagicBrushValue > 0)
            {
                if (_popupSkillMagicBrush != null)
                    _popupSkillMagicBrush.SetActive(true);

                _powerUpSystem.EnableMagicBrushIcon();
            }
            else
            {
                Global.Send(new SignalOpenEffectNotEnough());
            }
        }

        private void UsePowerUpMagicBrush()
        {
            if (_popupSkillMagicBrush != null && !_popupSkillMagicBrush.activeSelf) return;

            var selectedColor = _powerUpSystem._colorMagic.color;
            if (selectedColor.a <= 0f)
            {
                ClosePopupMagicBrush();
                return;
            }

            _rewardSystem.DeductMagicBrush(1);
            RemoveSameColorCompleteBands(selectedColor);
            ClosePopupMagicBrush();
        }

        private void ClosePopupMagicBrush()
        {
            if (_popupSkillMagicBrush != null)
                _popupSkillMagicBrush.SetActive(false);

            _powerUpSystem.DisableMagicBrushIcon();
        }

        private void RemoveSameColorCompleteBands(Color32 selectedColor)
        {
            _powerUpSystem.PowerUpMagicBrush(selectedColor).Forget();
        }

        //================ POPUP BUY POWER UP ================//

        private void OpenPopupConfirmBuyMagicBrush()
        {
            if (_popupConfirmBuyMagicBrush != null)
                _popupConfirmBuyMagicBrush.SetActive(true);
        }

        public void ClosePopupConfirmBuyMagicBrush()
        {
            if (_popupConfirmBuyMagicBrush != null)
                _popupConfirmBuyMagicBrush.SetActive(false);
        }

        private void OpenPopupConfirmBuyBoom()
        {
            if (_popupConfirmBuyBoom != null)
                _popupConfirmBuyBoom.SetActive(true);
        }

        public void ClosePopupConfirmBuyBoom()
        {
            if (_popupConfirmBuyBoom != null)
                _popupConfirmBuyBoom.SetActive(false);
        }

        //================ OTHERS =======================//

        private void OnDestroy()
        {
            _mouseClickSubWave?.Dispose();
        }
    }
}
*/
