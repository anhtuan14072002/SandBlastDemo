using GoogleMobileAds.Api;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.AdmobService
{
    [CreateAssetMenu(menuName = "HadesSDK/AdmobConfig", fileName = "AdmobConfig")]
    public class AdmobConfig :  ScriptableObject
    {
        public string interID;
        public string rewardID;
        public string mrecID;
        public string aoaID;
        public string bannerID;

        public AdPosition mrecAdPosition = AdPosition.Bottom;

        public bool bannerIsCollapsible;
        [Tooltip("If set to True, when LoadBanner is called, immediately create a new banner and discard the old one")]
        public bool shouldDestroyBannerWhenLoad = false;
        [Tooltip("This is not working yet")]
        public bool shouldReloadBannerIfLoadedFail;
        [Tooltip("If set to True, aoa won't be load again after being showed")] 
        public bool onlyShowAoaOnce;


    }
}