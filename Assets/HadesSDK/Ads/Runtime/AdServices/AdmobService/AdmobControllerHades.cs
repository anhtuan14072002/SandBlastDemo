using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using HadesSDK.Ads.Core;
using HadesSDK.Ads.Runtime.FirebaseServices;
using HadesSDK.Ads.Runtime.Utils;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.AdmobService
{
    public class AdmobControllerHades : AdService, IAOAProvider
    {
        public Action onAOADisplay { get; }
        
        private readonly AdmobConfig _admobConfig;
        private readonly FirebaseService _firebaseService;

        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
        private BannerView _bannerView;
        private BannerView _mrecView;
        private AppOpenAd _appOpenAd;
        
        private bool _isMrecLoaded;

        private Vector2 _mrecCustomPosition = new Vector2(-10000,-10000);
        private Vector2 _invalidPosition = new Vector2(-10000, -10000);
        
        public AdmobControllerHades(
            AdmobConfig admobConfig,
            FirebaseService firebaseService)
        {
            _admobConfig = admobConfig;
            _firebaseService = firebaseService;
        }

        public override void Init()
        {
            if (_admobConfig == null)
            {
                Debug.Log("Admob config is null, unable to start");
                return;
            }
            
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            List<String> deviceIds = new List<String>() { AdRequest.TestDeviceSimulator };

            #if GMA_DEPRECATED
            RequestConfiguration requestConfiguration =
                new RequestConfiguration.Builder()
                    .SetTagForChildDirectedTreatment(TagForChildDirectedTreatment.Unspecified)
                    .SetTestDeviceIds(deviceIds).build();
            #else
            RequestConfiguration requestConfiguration = new RequestConfiguration();
            requestConfiguration.TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified;
            requestConfiguration.TestDeviceIds = deviceIds;
            #endif
            MobileAds.SetRequestConfiguration(requestConfiguration);
            
            MobileAds.Initialize(OnInitComplete);
        }
        
        void OnInitComplete(InitializationStatus initializationStatus)
        {
            IsInit = true;
        
            MobileAdsEventExecutor.ExecuteInUpdate((() =>
            {
                Debug.Log("Initialization Admob Complete");

                Dictionary<string, AdapterStatus> map = initializationStatus.getAdapterStatusMap();
                foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
                {
                    string className = keyValuePair.Key;
                    AdapterStatus status = keyValuePair.Value;
                    switch (status.InitializationState)
                    {
                        case AdapterState.NotReady:
                            // The adapter initialization did not complete.
                            MonoBehaviour.print("Adapter: " + className + " not ready.");
                            break;
                        case AdapterState.Ready:
                            // The adapter was successfully initialized.
                            MonoBehaviour.print("Adapter: " + className + " is initialized.");
                            break;
                    }
                }
                
                OnAdServiceInitializeFinished?.Invoke();
            }));
        }

        #region Interstitial Ad

        public override void LoadInterstitial()
        {
            if(string.IsNullOrEmpty(_admobConfig.interID)) return;
            
            var adRequest = new AdRequest();
            var id = _admobConfig.interID;

            if (_interstitialAd != null)
            {
                DestroyInterstitial();
            }
            
            InterstitialAd.Load(id, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                // If the operation failed with a reason.
                if (error != null)
                {
                    ScheduleReloadInterstitial().Forget();
                    Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                    return;
                }
                
                if (ad == null)
                {
                    ScheduleReloadInterstitial().Forget();
                    Debug.LogError("Unexpected error: Interstitial load event fired with null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                _interRetryAttempt = 0;
                _interstitialAd = ad;

                RegisterEventHandlersInterstitial(ad);
            });
        }

        public override bool IsInterstitialReady()
        {
            return _interstitialAd != null && _interstitialAd.CanShowAd();
        }

        public override void ShowInterstitial()
        {
            if (IsInterstitialReady())
            {
                _interstitialAd.Show();
            }
        }
        
        public void DestroyInterstitial()
        {
            if (_interstitialAd != null)
            {
                Debug.Log("Destroying interstitial ad.");
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
        }

        private void RegisterEventHandlersInterstitial(InterstitialAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (adValue) =>
            {
                Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
                
                OnAdPaid?.Invoke(adValue.ConvertToBaseAdValue(AdType.interstitial, _admobConfig.interID));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () => { Debug.Log("Interstitial ad was clicked."); };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Admob: inters displayed");
                OnInterAdDisplay?.Invoke();
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial ad full screen content closed.");
                ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
                OnInterAdClose?.Invoke();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("Interstitial ad failed to open full screen content with error : "
                               + error);
                ActionUtility.StartActionDelay(LoadInterstitial, 0.5f).Forget();
                OnInterAdDisplayFail?.Invoke();
            };
        }

        #endregion

        #region Reward ad

        public override void LoadReward()
        {
            if (string.IsNullOrEmpty(_admobConfig.rewardID)) return;
            
            // Clean up the old ad before loading a new one.
            if (_rewardedAd != null)
            {
                DestroyRewarded();
            }

            Debug.Log("Loading rewarded ad.");

            // Create our request used to load the ad.
            var adRequest = new AdRequest();
            var id = _admobConfig.rewardID;

            // Send the request to load the ad.
            RewardedAd.Load(id, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                // If the operation failed with a reason.
                if (error != null)
                {
                    ScheduleReloadReward().Forget();
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                    return;
                }

                // If the operation failed for unknown reasons.
                // This is an unexpected error, please report this bug if it happens.
                if (ad == null)
                {
                    ScheduleReloadReward().Forget();
                    Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                _rewardRetryAttempt = 0;
                _rewardedAd = ad;

                // Register to ad events to extend functionality.
                RegisterEventHandlersRewarded(ad);
            });
        }

        public override bool IsRewardReady()
        {
            return _rewardedAd != null && _rewardedAd.CanShowAd();
        }

        public override void ShowReward()
        {
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show(OnRewardReceiveCallback);
            }
        }

        void OnRewardReceiveCallback(Reward reward)
        {
            OnRewardReceive?.Invoke();
        }
        
        public void DestroyRewarded()
        {
            if (_rewardedAd != null)
            {
                Debug.Log("Destroying rewarded ad.");
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
        }
        
        private void RegisterEventHandlersRewarded(RewardedAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (adValue) =>
            {
                Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
                
                OnAdPaid?.Invoke(adValue.ConvertToBaseAdValue(AdType.reward, _admobConfig.rewardID));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () => { Debug.Log("Rewarded ad recorded an impression."); };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () => { Debug.Log("Rewarded ad was clicked."); };
            // Raised when the ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                OnRewardAdDisplay?.Invoke();
                Debug.Log("Rewarded ad full screen content opened.");
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded ad full screen content closed.");
                OnRewardAdClose?.Invoke();

                ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                OnRewardAdDisplayFail?.Invoke();
                Debug.LogError("Rewarded ad failed to open full screen content with error : "
                               + error);

                ActionUtility.StartActionDelay(LoadReward, 0.5f).Forget();
            };
        }

        #endregion

        #region Banner Ad

        public override void LoadBanner()
        {
            if(string.IsNullOrEmpty(_admobConfig.bannerID)) return;

            if (_admobConfig.shouldDestroyBannerWhenLoad)
            {
                CreateBannerView();
            }
            else
            {
                if (_bannerView == null)
                {
                    CreateBannerView();
                }
            }

            // create our request used to load the ad.
            var adRequest = new AdRequest();

            if (_firebaseService.GetRemoteConfig<RemoteConfig>().banner_collap_on)
            {
                adRequest.Extras.Add("collapsible", "bottom");
            }
            
            Debug.Log("Loading banner ad.");
            _bannerView.LoadAd(adRequest);
        }

        public override void ShowBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Show();
            }
        }

        public override void HideBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Hide();
            }
        }

        void CreateBannerView()
        {
            if(string.IsNullOrEmpty(_admobConfig.bannerID)) return;
            
            Debug.Log("Creating banner view");
            DestroyBanner();

            AdSize adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            var id = _admobConfig.bannerID;
            _bannerView = new BannerView(id, adSize, AdPosition.Bottom);

            RegisterEventHandlersBanner(_bannerView);
        }
        
        private void RegisterEventHandlersBanner(BannerView ad)
        {
            // Raised when an ad is loaded into the banner view.
            ad.OnBannerAdLoaded += (() =>
            {
                Debug.Log("Ad Banner Loaded Success");
            });
            // Raised when an ad fails to load into the banner view.
            ad.OnBannerAdLoadFailed += (error =>
            {
                Debug.Log("Ad Banner Loaded Failed with error: "+error);
            });
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (adValue =>
            {
                AdType adType = _admobConfig.bannerIsCollapsible ? AdType.banner_collapsible : AdType.banner;
                OnAdPaid?.Invoke(adValue.ConvertToBaseAdValue(adType, _admobConfig.bannerID));
            });
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += (() =>
            {
                
            });
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                OnBannerClicked?.Invoke();
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                
            };
        }

        public override void DestroyBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("Destroying banner view.");
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        #endregion

        #region Mrec ad

        public override void LoadMrec()
        {
            if(string.IsNullOrEmpty(_admobConfig.mrecID)) return;

            if (_mrecView == null)
            {
                CreateMrecView();
            }

            var adRequest = new AdRequest();
            
            _mrecView.LoadAd(adRequest);
        }

        public override void ShowMrec()
        {
            if (_mrecView!=null)
            {
                _mrecView.Show();
            }
        }

        public override void HideMrec()
        {
            if (_mrecView!=null)
            {
                _mrecView.Hide();
            }
        }

        public override bool IsMrecReady()
        {
            return _isMrecLoaded;
        }

        public override void SetMrecPosition(Vector2 dpPos)
        {
            _mrecCustomPosition = dpPos;
            if (_mrecView != null)
            {
                _mrecView.SetPosition((int)dpPos.x, (int)dpPos.y);
            }
        }

        public void CreateMrecView()
        {
            if(string.IsNullOrEmpty(_admobConfig.mrecID)) return;
            
            DestroyMrecView();
            var id = _admobConfig.mrecID;

            if (_mrecCustomPosition != _invalidPosition)
            {
                _mrecView = new BannerView(id, AdSize.MediumRectangle, (int)_mrecCustomPosition.x, (int)_mrecCustomPosition.y);
            }
            else
            {
                _mrecView = new BannerView(id, AdSize.MediumRectangle, _admobConfig.mrecAdPosition);
            }
            
            RegisterEventHandlersMrec(_mrecView);
        }

        public void DestroyMrecView()
        {
            if (_mrecView != null)
            {
                Debug.Log("Destroy mrec view");
                _isMrecLoaded = false;
                _mrecView.Destroy();
                _mrecView = null;
            }
        }
        
        private void RegisterEventHandlersMrec(BannerView ad)
        {
            // Raised when an ad is loaded into the banner view.
            ad.OnBannerAdLoaded += (() =>
            {
                _isMrecLoaded = true;
                Debug.Log("Ad Mrec Loaded Success");
            });
            // Raised when an ad fails to load into the banner view.
            ad.OnBannerAdLoadFailed += (error =>
            {
                Debug.Log("Ad Mrec Loaded Failed with error: "+error);
            });
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (adValue =>
            {
                var adType = AdType.mrec;
                OnAdPaid?.Invoke(adValue.ConvertToBaseAdValue(adType, _admobConfig.mrecID));
            });
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += (() =>
            {
                
            });
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                
            };
        }

        #endregion

        public void LoadAOA()
        {
            if(string.IsNullOrEmpty(_admobConfig.aoaID)) return;
            var adRequest = new AdRequest();
            var id = _admobConfig.aoaID;
            
            AppOpenAd.Load(id, adRequest, (ad, error) =>
            {
                if (error != null)
                {
                    ScheduleReloadAOA().Forget();
                    Debug.LogError("App open ad failed to load an ad with error : "
                                   + error);
                    return;
                }
                
                if (ad == null)
                {
                    ScheduleReloadAOA().Forget();
                    Debug.LogError("Unexpected error: App open ad load event fired with " +
                                   " null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("App open ad loaded with response : " + ad.GetResponseInfo());
                _appOpenAd = ad;

                RegisterEventHandlersAOA(ad);
            });
        }

        async UniTaskVoid ScheduleReloadAOA()
        {
            await UniTask.WaitForSeconds(15f);
            LoadAOA();
        }

        public void ShowAOA()
        {
            if (_appOpenAd != null && _appOpenAd.CanShowAd())
            {
                Debug.Log("Showing app open ad.");
                _appOpenAd.Show();
            }
            else
            {
                Debug.LogError("App open ad is not ready yet.");
            }
        }

        public bool IsAOAReady()
        {
            return _appOpenAd != null && _appOpenAd.CanShowAd();
        }
        
        private void RegisterEventHandlersAOA(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (adValue) =>
        {
            OnAdPaid?.Invoke(adValue.ConvertToBaseAdValue(AdType.aoa, _admobConfig.aoaID));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App open ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("App open ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App open ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("App open ad full screen content closed.");

            // It may be useful to load a new ad when the current one is complete.
            if (!_admobConfig.onlyShowAoaOnce)
            {
                LoadAOA();
            }
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("App open ad failed to open full screen content with error : "
                            + error);
        };
    }
        
        
    }
}