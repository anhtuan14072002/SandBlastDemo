using System.Text;
using HadesSDK.Ads.Core;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.FirebaseServices
{
    public class FirebaseServiceDummy : FirebaseService
    {
        private RemoteConfig _remoteConfig;

        public FirebaseServiceDummy()
        {
            // Instance = this;
            _remoteConfig = new RemoteConfig();
        }
        
        public override void Init()
        {
            IsInit = true;
            onRemoteConfigUpdate?.Invoke();
            onRemoteFetchCompleted?.Invoke();
            HasFetchedRemoteConfigs = true;
        }

        public override void LogEvent(string eventKey, params EventParameter[] parameters)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Ay yo this event is event is logged: " + eventKey);
            stringBuilder.AppendLine();

            if (parameters != null && parameters.Length > 0)
            {
                foreach (var parameter in parameters)
                {
                    stringBuilder.Append($"{parameter.eventName}: {parameter.eventValue}");
                    stringBuilder.AppendLine();
                }
            }
            
            Debug.Log(stringBuilder.ToString());
        }

        public override void LogAdPaidEvent(AdService.AdValue adValue)
        {
            Debug.Log($"Ad Paid/Impression event: {adValue.adType.ToString()} {adValue.value.ToString()}");
        }

        public override void LogAdNonDeterminePaidEvent(AdService.AdValueNonDetermine adValue)
        {
            Debug.Log($"Ad Paid/Impression event: {adValue.adType} {adValue.value.ToString()}");
        }

        public override void LogAdEvent(string eventKey, string placement, string error = "")
        {
            Debug.Log($"Ad track event: {eventKey} {placement}");
        }

        public override T GetRemoteConfig<T>()
        {
            if (typeof(T) == _remoteConfig.GetType())
            {
                return _remoteConfig as T;
            }

            Debug.Log("Sorry eh, me do not have this remote class type!");
            return null;
        }
    }
}