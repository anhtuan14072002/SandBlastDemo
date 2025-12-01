using System;
using HadesSDK.Ads.Core;
using HadesSDK.Ads.Runtime.AdServices.AdmobService;
using HadesSDK.Ads.Runtime.AnalyticServices;
using HadesSDK.Ads.Runtime.FirebaseServices;
using HadesSDK.Ads.Runtime.Utils;
using UnityEngine;
using UnityEngine.Serialization;

#if APPLOVIN
using HadesSDK.Ads.Runtime.AdServices.ApplovinService;
#endif

#if IRONSOURCE
using HadesSDK.Ads.Runtime.AdServices.IronSourceService;
#endif

namespace HadesSDK.Ads.Runtime
{
    public class AdManager : MonoBehaviour
    {
        [Header("Extra configs")] 
        [SerializeField] private UtilityCanvas _utilityCanvas;
        [SerializeField] private bool _showLoadingAdPopup = true;
        [SerializeField] private bool _notifyIfNoRewardReady = true;
        
        [Header("Analytic")]
        [SerializeField] private AnalyticController _analyticController;
#if APPLOVIN
        [Header("APPLOVIN")]
        [SerializeField] private ApplovinConfig _applovinConfig;
#elif IRONSOURCE
        [Header("IRONSOURCE")]
        [SerializeField] private IronSourceHadesConfig _ironSourceHadesConfig;
#endif
        [Header("ADMOB")]
        [SerializeField] private AdmobConfig _admobConfig;
        
        private AdService _mainAdMediation;
        private AdmobControllerHades _backfillMediation;

        private IAOAProvider _aoaProvider;

        private FirebaseService _firebaseService;
        private RemoteConfig _remoteConfig;
        private MmpService _mmpService;
        
        #region Singleton

        public static AdManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                OnAwake();
            }
            else
            {
                Destroy(gameObject);
            }
            
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        private float _timer;
        private float _lastTimeShowFullScreenAd = -100;
        private float _lastTimeShowInterAd = -100;
        private float _lastTimeShowAoa = -100;

        private string _interPlacement;
        private string _rewardPlacement;

        private Action _interSuccessCallback;
        private Action _rewardSuccessCallback;

        private Action _interFailCallback;
        private Action _rewardFailCallback;
        private bool _canCallInit = true;
        public bool IsInit { get; private set; }

        void OnAwake()
        {
            _analyticController.InitFirebaseBody();

            _firebaseService = _analyticController.GetFirebaseService();
            _firebaseService.onRemoteConfigUpdate += OnFirebaseRemoteConfigUpdate;
            _firebaseService.Init();

            _remoteConfig = _firebaseService.GetRemoteConfig<RemoteConfig>();
        }

        void OnFirebaseRemoteConfigUpdate()
        {
            _remoteConfig = _firebaseService.GetRemoteConfig<RemoteConfig>();
        }

        public void Init()
        {
            if(!_canCallInit) return;
            _canCallInit = false;
            
            _analyticController.InitMmpServiceBody();
            _mmpService = _analyticController.GetMmpService();
            
#if APPLOVIN
            _mainAdMediation =
                new ApplovinControllerHades(_applovinConfig, _firebaseService, _analyticController.GetMmpService());
            _backfillMediation = new AdmobControllerHades(_admobConfig, _firebaseService);

            _aoaProvider = (ApplovinControllerHades)_mainAdMediation;
            
            RegisterAdEvent(_backfillMediation);
            _backfillMediation.OnAdServiceInitializeFinished += OnBackfillMediationInitializeFinished;
            _backfillMediation.Init();
            
#elif IRONSOURCE
            _mainAdMediation = new IronSourceController(_ironSourceHadesConfig, _firebaseService,
                _analyticController.GetMmpService());
            _backfillMediation = new AdmobControllerHades(_admobConfig, _firebaseService);
            _aoaProvider = _backfillMediation;
            
            RegisterAdEvent(_backfillMediation);
            _backfillMediation.OnAdServiceInitializeFinished += OnBackfillMediationInitializeFinished;
            _backfillMediation.Init();
#else
            _mainAdMediation = new AdmobControllerHades(_admobConfig, _firebaseService);
            _backfillMediation = (AdmobControllerHades)_mainAdMediation;
            _backfillMediation.OnAdServiceInitializeFinished += OnBackfillMediationInitializeFinished;
            _aoaProvider = _backfillMediation;
#endif
            
            
            _mainAdMediation.OnAdServiceInitializeFinished += OnMainAdMediationInitializeFinished;
            RegisterAdEvent(_mainAdMediation);
            _mainAdMediation.Init();
        }

        void OnMainAdMediationInitializeFinished()
        {
            IsInit = true;
            
            _mainAdMediation.LoadInterstitial();
            _mainAdMediation.LoadReward();
            _mainAdMediation.LoadBanner();
        }

        void OnBackfillMediationInitializeFinished()
        {
            _aoaProvider.LoadAOA();
            _backfillMediation.LoadMrec();
        }

        #region Ad Paid Event Handler

        void OnAdPaidEvent(AdService.AdValue adValue)
        {
            if (adValue.adType == AdService.AdType.interstitial)
            {
                adValue.placement = _interPlacement;
            }
            else if (adValue.adType == AdService.AdType.reward)
            {
                adValue.placement = _rewardPlacement;
            }
            
            _firebaseService.LogAdPaidEvent(adValue);
            _mmpService.TrackAdEvent(adValue);
        }

        void OnAdNonDeterminePaidEvent(AdService.AdValueNonDetermine adValue)
        {
            _firebaseService.LogAdNonDeterminePaidEvent(adValue);
            _mmpService.TrackAdNonDetermineEvent(adValue);
        }

        #endregion

        void RegisterAdEvent(AdService adService)
        {
            adService.OnInterAdDisplay += OnInterDisplayed;
            adService.OnInterAdDisplayFail += OnInterDisplayedFailed;
            adService.OnInterAdClose += OnInterHidden;

            adService.OnRewardAdDisplay += OnRewardDisplayed;
            adService.OnRewardAdDisplayFail += OnRewardDisplayedFailed;
            adService.OnRewardReceive += OnRewardReceiveEvent;
            adService.OnRewardAdClose += OnRewardHidden;

            adService.OnBannerClicked += OnBannerClicked;

            adService.OnAdPaid += OnAdPaidEvent;
            adService.OnAdNonDeterminePaid += OnAdNonDeterminePaidEvent;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
        }

        bool CanShowFullScreenAd()
        {
            return _timer - _lastTimeShowFullScreenAd >= 1.2f;
        }

        #region Interstitial

        /// Fail callback doesn't really do anything atm, so just fill in null, and don't ask why.
        public void ShowInterstitial(Action successCallback, Action failCallback, string placement)
        {
            _interSuccessCallback = successCallback;
            _interFailCallback = failCallback;
            _interPlacement = placement;

            if (!CanShowFullScreenAd())
            {
                _interSuccessCallback?.Invoke();
                return;
            }
            
            _mmpService.TrackCustomEvent("inters_call_show", null);
            _firebaseService.LogAdEvent("inters_call_show", _interPlacement);

            if (!IsInterstitialPassCapping())
            {
                _interSuccessCallback?.Invoke();
                return;
            }
            
            _mmpService.TrackCustomEvent("inters_passed_capping_time", null);
            _firebaseService.LogAdEvent("inters_passed_capping_time", _interPlacement);

            if (_mainAdMediation.IsInterstitialReady())
            {
                _mmpService.TrackCustomEvent("inters_available", null);
                _firebaseService.LogAdEvent("inters_available", _interPlacement);
                
                _lastTimeShowFullScreenAd = _timer;
                _lastTimeShowInterAd = _timer;
                if(_showLoadingAdPopup) _utilityCanvas.DisplayAdLoadingPopup(true);
                _mainAdMediation.ShowInterstitial();
            }
            else
            {
                _mainAdMediation.ScheduleReloadInterstitial().Forget();
                _interSuccessCallback?.Invoke();
            }
        }

        public bool IsInterstitialReady()
        {
            return _mainAdMediation.IsInterstitialReady();
        }

        public bool IsInterstitialPassCapping()
        {
            float cappingTime = _remoteConfig.inter_ad_capping_time;

            return (_timer - _lastTimeShowInterAd) >= cappingTime;
        }

        void OnInterDisplayed()
        {
            ActionUtility.StartActionOnMainThread(() => _utilityCanvas.DisplayAdLoadingPopup(false)).Forget();
            
            _mmpService.TrackCustomEvent("inters_displayed", null);
            _firebaseService.LogAdEvent("inters_displayed", _interPlacement);
        }

        void OnInterDisplayedFailed()
        {
            ActionUtility.StartActionOnMainThread((() =>
            {
                _utilityCanvas.DisplayAdLoadingPopup(false);
                _interSuccessCallback?.Invoke();
                _interSuccessCallback = null;
            })).Forget();
        }

        void OnInterHidden()
        {
            ActionUtility.StartActionOnMainThread((() =>
            {
                _utilityCanvas.DisplayAdLoadingPopup(false);
                _interSuccessCallback?.Invoke();
                _interSuccessCallback = null;
            })).Forget();
        }

        #endregion

        #region Reward

        public void ShowReward(Action successCallback, Action failCallback, string placement)
        {
            if (!CanShowFullScreenAd())
            {
                return;
            }

            _mmpService.TrackCustomEvent("rewarded_call_show", null);
            _firebaseService.LogAdEvent("rewarded_call_show", _rewardPlacement);
            
            if (_mainAdMediation.IsRewardReady())
            {
                _mmpService.TrackCustomEvent("rewarded_available", null);
                _firebaseService.LogAdEvent("rewarded_available", _rewardPlacement);
                
                _rewardSuccessCallback = successCallback;
                _rewardFailCallback = failCallback;
                _rewardPlacement = placement;

                _lastTimeShowFullScreenAd = _timer;
                if(_showLoadingAdPopup) _utilityCanvas.DisplayAdLoadingPopup(true);
                _mainAdMediation.ShowReward();
            }
            else
            {
                if(_notifyIfNoRewardReady) _utilityCanvas.NotifyAdNotReady();
            }
        }

        public bool IsRewardReady()
        {
            return _mainAdMediation.IsRewardReady();
        }

        void OnRewardDisplayed()
        {
            ActionUtility.StartActionOnMainThread(() => _utilityCanvas.DisplayAdLoadingPopup(false)).Forget();
            _mmpService.TrackCustomEvent("rewarded_ad_displayed", null);
            _firebaseService.LogAdEvent("rewarded_ad_displayed", _rewardPlacement);
        }
        
        void OnRewardDisplayedFailed()
        {
            ActionUtility.StartActionOnMainThread((() =>
            {
                _utilityCanvas.DisplayAdLoadingPopup(false);
                _rewardFailCallback?.Invoke();
                _rewardFailCallback = null;
            })).Forget();
        }

        void OnRewardHidden()
        {
            ActionUtility.StartActionOnMainThread(() => _utilityCanvas.DisplayAdLoadingPopup(false)).Forget();
        }

        void OnRewardReceiveEvent()
        {
            _mmpService.TrackCustomEvent("rewarded_ad_completed", null);
            _firebaseService.LogAdEvent("rewarded_ad_completed", _rewardPlacement);
            
            ActionUtility.StartActionOnMainThread((() =>
            {
                _utilityCanvas.DisplayAdLoadingPopup(false);
                _rewardSuccessCallback?.Invoke();
                _rewardSuccessCallback = null;
            })).Forget();
        }

        #endregion

        #region Banner

        public void LoadBanner()
        {
            _mainAdMediation.LoadBanner();
        }
        
        public void ShowBanner()
        {
            _mainAdMediation.ShowBanner();
        }

        public void HideBanner()
        {
            _mainAdMediation.HideBanner();
        }

        public void DestroyBanner()
        {
            _mainAdMediation.DestroyBanner();
        }

        void OnBannerClicked()
        {
            _lastTimeShowFullScreenAd = _timer;
        }

        #endregion

        #region Mrec

        public void ShowMrec()
        {
            _backfillMediation.ShowMrec();
        }

        public void HideMrec()
        {
            _backfillMediation.HideMrec();
        }

        public bool IsMrecReady()
        {
            return _backfillMediation.IsMrecReady();
        }

        public void UpdateMrecPosition(Vector2 dpPos)
        {
            _backfillMediation.SetMrecPosition(dpPos);
        }

        #endregion

        #region Aoa

        public void ShowAoa()
        {
            if(_aoaProvider == null) return;
            
            if(_remoteConfig.open_ad_on == false) return;
            if(!CanShowFullScreenAd()) return;
            if(!IsAoaPassedCappingTime()) return;
            
            if (IsAoaReady())
            {
                _lastTimeShowAoa = _timer;
                _lastTimeShowFullScreenAd = _timer;
                _aoaProvider.ShowAOA();
            }
        }

        bool IsAoaPassedCappingTime()
        {
            float cappingTime = _remoteConfig.open_ad_capping_time;
            return _timer - _lastTimeShowAoa >= cappingTime;
        }

        public bool IsAoaReady()
        {
            if (_aoaProvider == null) return false;

            return _aoaProvider.IsAOAReady();
        }
        #endregion
    }
}