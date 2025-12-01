#if APPSFLYER
using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using HadesSDK.Ads.Core;

namespace HadesSDK.Ads.Runtime.AnalyticServices
{
    public class AppsflyerController : MmpService
    {
        private readonly AppsflyerObject _appsFlyerObjectScript;
        
        public AppsflyerController(AppsflyerObject appsFlyerObjectScript)
        {
            _appsFlyerObjectScript = appsFlyerObjectScript;
        }

        public override void Init()
        {
            _appsFlyerObjectScript.Init();
        }

        public override string GetUserID()
        {
            return AppsFlyer.getAppsFlyerId();
        }

        public override void TrackAdEvent(AdService.AdValue adValue)
        {
            Dictionary<string, string> additions = new Dictionary<string, string>();
            additions.Add("ad_platform", adValue.adPlatform.ToString());
            additions.Add("ad_source", adValue.adNetwork);
            additions.Add("ad_unit_name", adValue.adIdentifier);
            additions.Add("ad_format", adValue.adType.ToString());
            additions.Add("ad_placement", adValue.placement);
            additions.Add("value", adValue.value.ToString());
            additions.Add("currency", adValue.adCurrency);
            AppsFlyerAdRevenue.logAdRevenue(adValue.adNetwork, GetMediationType(adValue.adPlatform), adValue.value, adValue.adCurrency,
                additions);
        }

        public override void TrackAdNonDetermineEvent(AdService.AdValueNonDetermine adValue)
        {
            Dictionary<string, string> additions = new Dictionary<string, string>();
            additions.Add("ad_platform", adValue.adPlatform.ToString());
            additions.Add("ad_source", adValue.adNetwork);
            additions.Add("ad_unit_name", adValue.adIdentifier);
            additions.Add("ad_format", adValue.adType);
            additions.Add("ad_placement", "");
            additions.Add("value", adValue.value.ToString());
            additions.Add("currency", adValue.adCurrency);
            AppsFlyerAdRevenue.logAdRevenue(adValue.adNetwork, GetMediationType(adValue.adPlatform), adValue.value, adValue.adCurrency,
                additions);
        }

        AppsFlyerAdRevenueMediationNetworkType GetMediationType(MediationNetwork mediationNetwork)
        {
            AppsFlyerAdRevenueMediationNetworkType mediationNetworkType = default;
            
            switch (mediationNetwork)
            {
                case MediationNetwork.Admob:
                    mediationNetworkType = AppsFlyerAdRevenueMediationNetworkType
                        .AppsFlyerAdRevenueMediationNetworkTypeGoogleAdMob;
                    break;
                case MediationNetwork.Applovin:
                    mediationNetworkType = AppsFlyerAdRevenueMediationNetworkType
                        .AppsFlyerAdRevenueMediationNetworkTypeApplovinMax;
                    break;
                case MediationNetwork.IronSource:
                    mediationNetworkType = AppsFlyerAdRevenueMediationNetworkType
                        .AppsFlyerAdRevenueMediationNetworkTypeIronSource;
                    break;
                default:
                    mediationNetworkType = AppsFlyerAdRevenueMediationNetworkType
                        .AppsFlyerAdRevenueMediationNetworkTypeUnity;
                    break;
            }

            return mediationNetworkType;
        }

        public override void TrackCustomEvent(string eventKey, Dictionary<string, string> eventValues)
        {
            AppsFlyer.sendEvent("af_"+eventKey, eventValues);
        }
    }
}
#endif