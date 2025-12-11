using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("Button Use Skill")]
        [SerializeField] private Button _btnUseMagicBrush;
        [FormerlySerializedAs("_buttonUseBoom")] 
        [SerializeField] private Button _btnUseBoom;

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
            
            //--------Boom---------//
            _btnBuyBoom.onClick.AddListener(OpenPopupConfirmBuyBoom);
            _btnConfirmBoom.onClick.AddListener(BuyBoom);
            _btnUseBoom.onClick.AddListener(UsePowerUpBoom);
            _btnCloseSkillBoom.onClick.AddListener(ClosePowerUpBoom);
            
            //-----MagicBrush----//
            _btnBuyMagicBrush.onClick.AddListener(OpenPopupConfirmBuyMagicBrush);
            _btnConfirmMagicBrush.onClick.AddListener(BuyMagicBrush);
            _btnOpenSkillMagicBrush.onClick.AddListener(OpenPopupMagicBrush);
            _btnUseMagicBrush.onClick.AddListener(UsePowerUpMagicBrush);
            _btnCloseSkillMagicBrush.onClick.AddListener(ClosePopupMagicBrush);
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
            if (!_popupSkillMagicBrush.activeSelf) return;
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

        //================ OTHERS =======================//
        
        private void OnDestroy()
        {
            _mouseClickSubWave?.Dispose();
        }
    }
}