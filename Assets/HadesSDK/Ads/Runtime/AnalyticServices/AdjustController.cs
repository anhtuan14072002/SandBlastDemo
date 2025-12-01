#if ADJUST
using System.Collections.Generic;
using com.adjust.sdk;
using HadesSDK.Ads.Core;

namespace HadesSDK.Ads.Runtime.AnalyticServices
{
    public class AdjustController : MmpService
    {
        private readonly string _appKey;
        private readonly AdjustEnvironment _adjustEnvironment;

        public AdjustController(string appKey, AdjustEnvironment adjustEnvironment)
        {
            _appKey = appKey;
            _adjustEnvironment = adjustEnvironment;
        }
        
        public override void Init()
        {
            Adjust.start(new AdjustConfig(_appKey, _adjustEnvironment));
        }

        public override string GetUserID()
        {
            return null;
        }

        public override void TrackAdEvent(AdService.AdValue adValue)
        {
            var adRevenue = new AdjustAdRevenue(GetMediationType(adValue.adPlatform));
            adRevenue.setRevenue(adValue.value, "USD");
            adRevenue.setAdRevenueNetwork(adValue.adNetwork);
            adRevenue.setAdRevenueUnit(adValue.adType.ToString());
            adRevenue.setAdRevenuePlacement(adValue.placement);

            Adjust.trackAdRevenue(adRevenue);
        }

        public override void TrackAdNonDetermineEvent(AdService.AdValueNonDetermine adValue)
        {
            var adRevenue = new AdjustAdRevenue(GetMediationType(adValue.adPlatform));
            adRevenue.setRevenue(adValue.value, "USD");
            adRevenue.setAdRevenueNetwork(adValue.adNetwork);
            adRevenue.setAdRevenueUnit(adValue.adType);
            adRevenue.setAdRevenuePlacement("");

            Adjust.trackAdRevenue(adRevenue);
        }

        string GetMediationType(MediationNetwork mediationNetwork)
        {
            string mediationNetworkType = "source";
            
            switch (mediationNetwork)
            {
                case MediationNetwork.Admob:
                    mediationNetworkType = AdjustConfig.AdjustAdRevenueSourceAdMob;
                    break;
                case MediationNetwork.Applovin:
                    mediationNetworkType = AdjustConfig.AdjustAdRevenueSourceAppLovinMAX;
                    break;
                case MediationNetwork.IronSource:
                    mediationNetworkType = AdjustConfig.AdjustAdRevenueSourceIronSource;
                    break;
                default:
                    mediationNetworkType = "source";
                    break;
            }

            return mediationNetworkType;
        }

        public override void TrackCustomEvent(string eventKey, Dictionary<string, string> eventValues)
        {
            AdjustEvent adjustEvent = new AdjustEvent(eventKey);
            Adjust.trackEvent(adjustEvent);
        }
    }
}
#endif