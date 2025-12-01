#if IRONSOURCE
using System;
using HadesSDK.Ads.Core;
using HadesSDK.Ads.Runtime.AnalyticServices;
using HadesSDK.Ads.Runtime.Utils;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.IronSourceService
{
    public class IronSourceController : AdService
    {
        private readonly IronSourceHadesConfig _config;
        private readonly FirebaseService _firebaseService;
        private readonly MmpService _mmpService;

        public IronSourceController(
            IronSourceHadesConfig config,
            FirebaseService firebaseService,
            MmpService mmpService)
        {
            _config = config;
            _firebaseService = firebaseService;
            _mmpService = mmpService;
        }

        public override void Init()
        {
            IronSourceEvents.onImpressionDataReadyEvent += ImpressionDataReadyEvent;

#if !UNITY_EDITOR
            IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitializationCompletedEvent;
#endif

            IronSourceConfig.Instance.setClientSideCallbacks(true);
            Debug.Log("unity-script: IronSource.Agent.validateIntegration");
            IronSource.Agent.validateIntegration();
            IronSource.Agent.setConsent(true);
            IronSource.Agent.setMetaData("do_not_sell", "false");
            IronSource.Agent.setMetaData("is_child_directed", "false");

            // SDK init
            Debug.Log("unity-script: IronSource.Agent.init");
            //IronSource.Agent.setMetaData("is_test_suite", "enable"); 
            IronSource.Agent.init(_config.appKey);

#if UNITY_EDITOR
            SdkInitializationCompletedEvent();
#endif
        }

        void ImpressionDataReadyEvent(IronSourceImpressionData impressionData)
        {
            double value = 0;

            if (impressionData.revenue is not null)
            {
                try
                {
                    value = impressionData.revenue.Value;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            AdValueNonDetermine adValue = new AdValueNonDetermine()
            {
                value = value,
                adIdentifier = "",
                adNetwork = impressionData.adNetwork,
                adType = impressionData.adUnit,
                adCurrency = "USD",
                adPlatform = MediationNetwork.IronSource
            };
            OnAdNonDeterminePaid?.Invoke(adValue);
        }

        private void SdkInitializationCompletedEvent()
        {
            if (IsInit) return;
            IsInit = true;
            //IronSource.Agent.launchTestSuite();
            Debug.Log("unity-script: Ironsource sdk init complete");

            #region Inter callback

            IronSourceInterstitialEvents.onAdReadyEvent += InterstitialOnAdLoadedEvent;
            IronSourceInterstitialEvents.onAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
            IronSourceInterstitialEvents.onAdShowSucceededEvent += InterstitialOnAdShowSucceededEvent;
            IronSourceInterstitialEvents.onAdShowFailedEvent += InterstitialOnAdDisplayFailedEvent;
            IronSourceInterstitialEvents.onAdClickedEvent += InterstitialOnAdClickedEvent;
            IronSourceInterstitialEvents.onAdOpenedEvent += InterstitialOnAdDisplayedEvent;
            IronSourceInterstitialEvents.onAdClosedEvent += InterstitialOnAdHiddenEvent;

            #endregion

            #region Reward callback

            IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedOnAdDisplayedEvent;
            IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedOnAdHiddenEvent;
            IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedOnAdDisplayFailedEvent;
            IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoUnavailable;
            IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedOnAdReceivedRewardEvent;
            IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedOnAdClickedEvent;
            IronSourceRewardedVideoEvents.onAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
            IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoAvailable;

            #endregion

            #region Banner callback

            IronSourceBannerEvents.onAdLoadedEvent += BannerAdLoadedEvent;
            IronSourceBannerEvents.onAdLoadFailedEvent += BannerAdLoadFailedEvent;
            IronSourceBannerEvents.onAdClickedEvent += BannerAdClickedEvent;
            IronSourceBannerEvents.onAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
            IronSourceBannerEvents.onAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
            IronSourceBannerEvents.onAdLeftApplicationEvent += BannerAdLeftApplicationEvent;

            #endregion


            OnAdServiceInitializeFinished?.Invoke();
        }

        private void InterstitialOnAdLoadedEvent(IronSourceAdInfo info)
        {
            _interRetryAttempt = 0;
            
            Debug.Log("Ironsource Inters ad Loaded");
        }

        private void InterstitialOnAdLoadFailedEvent(IronSourceError error)
        {
            ScheduleReloadInterstitial().Forget();
            Debug.Log("Ironsource Inters ad Loaded Failed: "+error);
        }

        void InterstitialOnAdShowSucceededEvent(IronSourceAdInfo adInfo)
        {
        }

        private void InterstitialOnAdDisplayedEvent(IronSourceAdInfo adInfo)
        {
            OnInterAdDisplay?.Invoke();
        }

        private void InterstitialOnAdDisplayFailedEvent(IronSourceError errorInfo, IronSourceAdInfo adInfo)
        {
            // Interstitial ad failed to display. We recommend loading the next ad
            OnInterAdDisplayFail?.Invoke();
            ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
        }

        private void InterstitialOnAdClickedEvent(IronSourceAdInfo adInfo)
        {
        }

        private void InterstitialOnAdHiddenEvent(IronSourceAdInfo adInfo)
        {
            OnInterAdClose?.Invoke();
            ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
        }

        private void RewardedOnAdLoadedEvent(IronSourceAdInfo adInfo)
        {
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            Debug.Log("Rewarded ad loaded");
            // Reset retry attempt
            _rewardRetryAttempt = 0;
        }

        private void RewardedOnAdLoadFailedEvent(IronSourceError errorInfo)
        {
            ScheduleReloadReward().Forget();
            Debug.Log("Ironsource Rewarded ad loaded Failed: "+errorInfo);
        }

        private void RewardedOnAdDisplayFailedEvent(IronSourceError errorInfo, IronSourceAdInfo adInfo)
        {
            // Rewarded ad failed to display. We recommend loading the next ad
            ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            
            OnRewardAdDisplayFail?.Invoke();
        }

        private void RewardedOnAdDisplayedEvent(IronSourceAdInfo adInfo)
        {
            OnRewardAdDisplay?.Invoke();
        }

        private void RewardedOnAdClickedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
        {
            Debug.Log("Rewarded ad clicked");
        }

        void RewardedVideoUnavailable()
        {
        }

        void RewardedVideoAvailable(IronSourceAdInfo adInfo)
        {
            Debug.Log("Ironsource Rewarded ad loaded");
            // Reset retry attempt
            _rewardRetryAttempt = 0;
        }

        private void RewardedOnAdHiddenEvent(IronSourceAdInfo adInfo)
        {
            ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            OnRewardAdClose?.Invoke();
        }

        private void RewardedOnAdReceivedRewardEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
        {
            OnRewardReceive?.Invoke();
        }

        void BannerAdLoadedEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("Ironsource Banner Ad Loaded");
        }

        void BannerAdLoadFailedEvent(IronSourceError error)
        {
            Debug.Log("Ironsource Banner Ad Loaded Failed: "+error);

            LoadBanner();
        }

        void BannerAdClickedEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerAdClickedEvent");
            OnBannerClicked?.Invoke();
        }

        void BannerAdScreenPresentedEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerAdScreenPresentedEvent");
        }

        void BannerAdScreenDismissedEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerAdScreenDismissedEvent");
        }

        void BannerAdLeftApplicationEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerAdLeftApplicationEvent");
        }

        public override void LoadInterstitial()
        {
            Debug.Log("Ironsource Load Interstitial Ads");
            if (!IronSource.Agent.isInterstitialReady())
            {
                IronSource.Agent.loadInterstitial();
            }
            else
            {
                Debug.Log("Load Interstitial Ads - AdsIsReady - Not Load");
            }
        }

        public override bool IsInterstitialReady()
        {
            return IronSource.Agent.isInterstitialReady();
        }

        public override void ShowInterstitial()
        {
            IronSource.Agent.showInterstitial();
        }

        public override void LoadReward()
        {
            Debug.Log("Ironsource Load Reward Ads");
            if (!IronSource.Agent.isRewardedVideoAvailable())
            {
                IronSource.Agent.loadRewardedVideo();
            }
            else
            {
                Debug.Log("Load Reward Ads - AdsIsReady - Not Load");
            }
        }

        public override bool IsRewardReady()
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        public override void ShowReward()
        {
            IronSource.Agent.showRewardedVideo();
        }

        public override void LoadBanner()
        {
            IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, _config.bannerPosition);
            Debug.Log("Ironsource Load Banner Ads");
        }

        public override void ShowBanner()
        {
            IronSource.Agent.displayBanner();
        }

        public override void HideBanner()
        {
            IronSource.Agent.hideBanner();
        }

        public override void DestroyBanner()
        {
            IronSource.Agent.destroyBanner();
        }

        public override void LoadMrec()
        {
            Debug.Log("Mrec not available");
        }

        public override void ShowMrec()
        {
            Debug.Log("Mrec not available");
        }

        public override bool IsMrecReady()
        {
            Debug.Log("Mrec not available");
            return false;
        }

        public override void HideMrec()
        {
            Debug.Log("Mrec not available");
        }

        public override void SetMrecPosition(Vector2 dpPos)
        {
            Debug.Log("Mrec not available");
        }
    }
}
#endif