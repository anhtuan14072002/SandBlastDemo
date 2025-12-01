#if APPLOVIN
using System;
using HadesSDK.Ads.Core;
using HadesSDK.Ads.Runtime.AnalyticServices;
using HadesSDK.Ads.Runtime.Utils;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.ApplovinService
{
    public class ApplovinControllerHades : AdService, IAOAProvider
    {
        private readonly ApplovinConfig _config;
        private readonly FirebaseService _firebaseService;
        private readonly MmpService _mmpService;

        private Vector2 _mrecCustomPosition = new Vector2(-10000, -10000);
        private Vector2 _invalidPosition = new Vector2(-10000, -10000);

        private bool _isMrecLoaded;

        public ApplovinControllerHades(
            ApplovinConfig config,
            FirebaseService firebaseService,
            MmpService mmpService)
        {
            _config = config;
            _firebaseService = firebaseService;
            _mmpService = mmpService;
        }

        public override void Init()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;

            var userID = _mmpService.GetUserID();
            if(string.IsNullOrEmpty(userID)) MaxSdk.SetUserId(userID);
            MaxSdk.SetSdkKey(_config.appKey);
            MaxSdk.InitializeSdk();
        }

        public override void Dispose()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
        }

        void OnSdkInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            #region inter callback

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += InterstitialOnAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += InterstitialOnAdPaidEvent;

            #endregion

            #region reward callback

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedOnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedOnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedOnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedOnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedOnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedOnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += RewardedOnAdPaidEvent;

            #endregion

            #region banner callback

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += BannerOnAdPaidEvent;

            #endregion

            #region mrec callback

            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMRecAdCollapsedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += MrecOnAdPaidEvent;

            #endregion

            #region aoa callback

            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAoaAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAoaAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAoaAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAoaDisplayEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAoaDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAoaHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += AoaOnAdPaidEvent;

            #endregion
            
            _mmpService.Init();

            IsInit = true;
            OnAdServiceInitializeFinished?.Invoke();
        }

        private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
            _interRetryAttempt = 0;
            
            Debug.Log("Applovin Inter loaded");
        }

        private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            ScheduleReloadInterstitial().Forget();
            
            Debug.Log("Applovin Inter loaded Failed"+errorInfo);
        }

        private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            OnInterAdDisplay?.Invoke();
        }

        private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo,
            MaxSdkBase.AdInfo adInfo)
        {
            OnInterAdDisplayFail?.Invoke();
            ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
        }

        private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
        }

        private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnInterAdClose?.Invoke();
            ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
        }
        
        void InterstitialOnAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdPaid?.Invoke(new AdValue()
            {
                adPlatform = MediationNetwork.Applovin,
                adNetwork = adInfo.NetworkName,
                value = adInfo.Revenue,
                adIdentifier = adUnitId,
                adCurrency = "USD",
                adType = AdType.interstitial
            });
        }

        private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            Debug.Log("Applovin Rewarded ad loaded");
            // Reset retry attempt
            _rewardRetryAttempt = 0;
        }

        private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            ScheduleReloadReward().Forget();
            
            Debug.Log("Applovin Rewarded ad loaded Failed "+errorInfo);
        }

        private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo,
            MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad failed to display. We recommend loading the next ad
            ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            
            OnRewardAdDisplayFail?.Invoke();
        }

        private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnRewardAdDisplay?.Invoke();
        }

        private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad clicked");
        }

        private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            OnRewardAdClose?.Invoke();
        }

        private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward,
            MaxSdkBase.AdInfo adInfo)
        {
            OnRewardReceive?.Invoke();
        }
        
        void RewardedOnAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdPaid?.Invoke(new AdValue()
            {
                adPlatform = MediationNetwork.Applovin,
                adNetwork = adInfo.NetworkName,
                value = adInfo.Revenue,
                adIdentifier = adUnitId,
                adCurrency = "USD",
                adType = AdType.reward
            });
        }
        
        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { Debug.Log("Applovin Banner ad loaded"); }

        private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) {Debug.Log("Applovin Banner ad loaded Failed: "+errorInfo); }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { OnBannerClicked?.Invoke(); }

        private void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
        
        void BannerOnAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdPaid?.Invoke(new AdValue()
            {
                adPlatform = MediationNetwork.Applovin,
                adNetwork = adInfo.NetworkName,
                value = adInfo.Revenue,
                adIdentifier = adUnitId,
                adCurrency = "USD",
                adType = AdType.banner
            });
        }
        
        public void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { _isMrecLoaded = true; Debug.Log("Applovin Mrec ad loaded"); }

        public void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error) { Debug.Log("Applovin Mrec ad loaded Failed: "+error); }

        public void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
        

        public void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        public void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
        
        void MrecOnAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdPaid?.Invoke(new AdValue()
            {
                adPlatform = MediationNetwork.Applovin,
                adNetwork = adInfo.NetworkName,
                value = adInfo.Revenue,
                adIdentifier = adUnitId,
                adCurrency = "USD",
                adType = AdType.mrec
            });
        }
        
        public void OnAoaAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { Debug.Log("Applovin Aoa ad loaded"); }

        public void OnAoaAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            Debug.Log("Applovin Rewarded ad loaded Failed: " + error);
            ActionUtility.StartActionDelay(LoadAOA, 7f).Forget();
        }

        public void OnAoaAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnAoaHiddenEvent(string adUnitId, MaxSdkBase.AdInfo arg2)
        {
            ActionUtility.StartActionOnMainThread(LoadAOA).Forget();
        }

        private void OnAoaDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo arg2, MaxSdkBase.AdInfo arg3)
        {
            ActionUtility.StartActionOnMainThread(LoadAOA).Forget();
        }

        private void OnAoaDisplayEvent(string adUnitId, MaxSdkBase.AdInfo arg2) { }
        
        void AoaOnAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdPaid?.Invoke(new AdValue()
            {
                adPlatform = MediationNetwork.Applovin,
                adNetwork = adInfo.NetworkName,
                value = adInfo.Revenue,
                adIdentifier = adUnitId,
                adCurrency = "USD",
                adType = AdType.aoa
            });
        }
        

        public override void LoadInterstitial()
        {
            if (string.IsNullOrEmpty(_config.interID)) return;
            Debug.Log("Applovin Load Interstitial");
            if (MaxSdk.IsInitialized())
            {
                if (!MaxSdk.IsInterstitialReady(_config.interID))
                {
                    MaxSdk.LoadInterstitial(_config.interID);
                }
                else
                {
                    Debug.Log("Applovin Load Interstitial - AdsIsReady - Not Load");
                }
            }
        }

        public override bool IsInterstitialReady()
        {
            return MaxSdk.IsInterstitialReady(_config.interID);
        }

        public override void ShowInterstitial()
        {
            MaxSdk.ShowInterstitial(_config.interID);
        }

        public override void LoadReward()
        {
            if (string.IsNullOrEmpty(_config.rewardID)) return;
            Debug.Log("Applovin Load Reward");
            if (MaxSdk.IsInitialized())
            {
                if (!MaxSdk.IsRewardedAdReady(_config.rewardID))
                {
                    MaxSdk.LoadRewardedAd(_config.rewardID);
                }
                else
                {
                    Debug.Log("Applovin Load Reward - AdsIsReady - Not Load");
                }
            }
        }

        public override bool IsRewardReady()
        {
            return MaxSdk.IsRewardedAdReady(_config.rewardID);
        }

        public override void ShowReward()
        {
            MaxSdk.ShowRewardedAd(_config.rewardID);
        }

        public override void LoadBanner()
        {
            if (string.IsNullOrEmpty(_config.bannerID)) return;
            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
            // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
            MaxSdk.CreateBanner(_config.bannerID, _config.bannerPosition);
            MaxSdk.SetBannerExtraParameter(_config.bannerID, "adaptive_banner", "false");
            // Set background or background color for banners to be fully functional.
            MaxSdk.SetBannerBackgroundColor(_config.bannerID, Color.black);
            MaxSdk.SetBannerWidth(_config.bannerID, (float)Screen.width);
            
            Debug.Log("Applovin Start Load Banner");
        }

        public override void ShowBanner()
        {
            MaxSdk.ShowBanner(_config.bannerID);
        }

        public override void HideBanner()
        {
            MaxSdk.HideBanner(_config.bannerID);
        }

        public override void DestroyBanner()
        {
            MaxSdk.DestroyBanner(_config.bannerID);
        }

        public override void LoadMrec()
        {
            if (string.IsNullOrEmpty(_config.mrecID)) return;
            if (_mrecCustomPosition != _invalidPosition)
            {
                MaxSdk.CreateMRec(_config.mrecID, _mrecCustomPosition.x, _mrecCustomPosition.y);
            }
            else
            {
                MaxSdk.CreateMRec(_config.mrecID, MaxSdkBase.AdViewPosition.BottomCenter);
            }
            
            Debug.Log("Applovin Start Load Mrec");
        }

        public override void ShowMrec()
        {
            MaxSdk.ShowMRec(_config.mrecID);
        }

        public override void HideMrec()
        {
            MaxSdk.HideMRec(_config.mrecID);
        }

        public override bool IsMrecReady()
        {
            return _isMrecLoaded;
        }

        public override void SetMrecPosition(Vector2 dpPos)
        {
            _mrecCustomPosition = dpPos;
            MaxSdk.UpdateMRecPosition(_config.mrecID, dpPos.x, dpPos.y);
        }

        public void LoadAOA()
        {
            if (string.IsNullOrEmpty(_config.aoaID)) return;

            MaxSdk.LoadAppOpenAd(_config.aoaID);
            
            Debug.Log("Applovin Start Load Aoa");
        }

        public void ShowAOA()
        {
            MaxSdk.ShowAppOpenAd(_config.aoaID);
        }

        public bool IsAOAReady()
        {
            return MaxSdk.IsAppOpenAdReady(_config.aoaID);
        }

        public Action onAOADisplay { get; }
    }
}
#endif