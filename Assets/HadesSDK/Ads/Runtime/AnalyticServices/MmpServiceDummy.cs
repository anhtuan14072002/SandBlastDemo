using System.Collections.Generic;
using HadesSDK.Ads.Core;

namespace HadesSDK.Ads.Runtime.AnalyticServices
{
    public class MmpServiceDummy : MmpService
    {
        public override void Init()
        {
            
        }

        public override string GetUserID()
        {
            return null;
        }

        public override void TrackAdEvent(AdService.AdValue adValue)
        {
            
        }

        public override void TrackAdNonDetermineEvent(AdService.AdValueNonDetermine adValue)
        {
            
        }

        public override void TrackCustomEvent(string eventKey, Dictionary<string, string> eventValues)
        {
            
        }
    }
}