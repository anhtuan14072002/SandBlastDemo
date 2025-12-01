using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace HadesSDK.Ads.Core
{
    public abstract class FirebaseService
    {
        public static FirebaseService Instance;
        public bool IsInit { get; protected set; } = false;
        public bool HasFetchedRemoteConfigs { get; protected set; }
        public abstract void Init();
        public abstract void LogEvent(string eventName, params EventParameter[] parameters);
        public abstract void LogAdEvent(string eventName, string placement, string error = "");
        public abstract void LogAdPaidEvent(AdService.AdValue adValue);
        public abstract void LogAdNonDeterminePaidEvent(AdService.AdValueNonDetermine adValue);

        public abstract T GetRemoteConfig<T>() where T : class;

        public Action onRemoteFetchCompleted;
        public Action onRemoteConfigUpdate;
    }

    public struct EventParameter
    {
        public string eventName;
        public string eventValue;

        public EventParameter(string eventName, string eventValue)
        {
            this.eventName = eventName;
            this.eventValue = eventValue;
        }

        public static EventParameter Default
        {
            get
            {
                Debug.Log("Bitch why you use this default event!");
                return new EventParameter("default_event","defaultEvent");
            }
        }
    }
}