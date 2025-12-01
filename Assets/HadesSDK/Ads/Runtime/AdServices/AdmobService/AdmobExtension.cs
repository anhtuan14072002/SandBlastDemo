using HadesSDK.Ads.Core;

namespace HadesSDK.Ads.Runtime.AdServices.AdmobService
{
    public static class AdmobExtension
    {
        public static AdService.AdValue ConvertToBaseAdValue(this GoogleMobileAds.Api.AdValue adValue, AdService.AdType adType, string identifier)
        {
            AdService.AdValue baseAdValue = new AdService.AdValue
            {
                value = (adValue.Value) / 1000000.0,
                adType = adType,
                adCurrency = "USD",
                adNetwork = "admob",
                adIdentifier = identifier,
                adPlatform = MediationNetwork.Admob,
            };

            return baseAdValue;
        }
    }
}