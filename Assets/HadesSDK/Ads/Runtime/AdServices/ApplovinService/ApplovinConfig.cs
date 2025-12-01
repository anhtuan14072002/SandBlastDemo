using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.ApplovinService
{
    [CreateAssetMenu(menuName = "HadesSDK/ApplovinConfig", fileName = "ApplovinConfig")]
    public class ApplovinConfig : ScriptableObject
    {
        public string appKey;
        
        public string interID;
        public string rewardID;
        public string bannerID;
        public string aoaID;
        public string mrecID;

#if APPLOVIN
        public MaxSdkBase.BannerPosition bannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
#endif
    }
}