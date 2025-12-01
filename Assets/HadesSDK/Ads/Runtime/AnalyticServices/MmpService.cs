using System.Collections.Generic;
using HadesSDK.Ads.Core;

namespace HadesSDK.Ads.Runtime.AnalyticServices
{
    public abstract class MmpService
    {
        public abstract void Init();
        public abstract string GetUserID();

        public abstract void TrackAdEvent(AdService.AdValue adValue);
        public abstract void TrackAdNonDetermineEvent(AdService.AdValueNonDetermine adValue);

        public abstract void TrackCustomEvent(string eventKey, Dictionary<string, string> eventValues);
    }
}