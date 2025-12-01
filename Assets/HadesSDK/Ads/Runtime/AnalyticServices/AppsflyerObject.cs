using System.Collections.Generic;
#if APPSFLYER
using AppsFlyerSDK;
#endif
using UnityEngine;

namespace HadesSDK.Ads.Runtime.AnalyticServices
{
#if APPSFLYER
    public class AppsflyerObject : MonoBehaviour, IAppsFlyerConversionData
#else
    public class AppsflyerObject : MonoBehaviour
#endif
    {
        // These fields are set from the editor so do not modify!
        //******************************//
        public string devKey;
        public string appID;
        public string UWPAppID;
        public string macOSAppID;
        public bool isDebug;
        public bool getConversionData;
        //******************************//


        public void Init()
        {
            if(string.IsNullOrEmpty(devKey)) Debug.LogError("Appsflyer dev key is invalid");

#if APPSFLYER
            // These fields are set from the editor so do not modify!
            //******************************//
            AppsFlyer.setIsDebug(isDebug);
#if UNITY_WSA_10_0 && !UNITY_EDITOR
        AppsFlyer.initSDK(devKey, UWPAppID, getConversionData ? this : null);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
    AppsFlyer.initSDK(devKey, macOSAppID, getConversionData ? this : null);
#else
            AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);
#endif
            //******************************/
 
            AppsFlyer.startSDK();
            AppsFlyerAdRevenue.start();
#endif
        }

        // Mark AppsFlyer CallBacks
        public void onConversionDataSuccess(string conversionData)
        {
#if APPSFLYER
            AppsFlyer.AFLog("didReceiveConversionData", conversionData);
            Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
            // add deferred deeplink logic here 
#endif
        }

        public void onConversionDataFail(string error)
        {
#if APPSFLYER
            AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
#endif
        }

        public void onAppOpenAttribution(string attributionData)
        {
#if APPSFLYER
            AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
            Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
            // add direct deeplink logic here 
#endif
        }

        public void onAppOpenAttributionFailure(string error)
        {
#if APPSFLYER
            AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
#endif
        }
    }
}