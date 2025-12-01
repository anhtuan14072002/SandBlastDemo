using UnityEngine;

namespace HadesSDK.Ads.Runtime.AdServices.IronSourceService
{
    [CreateAssetMenu(menuName = "HadesSDK/IronSourceConfig", fileName = "IronSourceConfig")]
    public class IronSourceHadesConfig : ScriptableObject
    {
        public string appKey;

#if IRONSOURCE
        public IronSourceBannerPosition bannerPosition = IronSourceBannerPosition.BOTTOM;
#endif
    }
}