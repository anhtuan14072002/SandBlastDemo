#if FIREBASE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Firebase.Analytics;
using Firebase.Extensions;
#if FIREBASE_MESS
using Firebase.Messaging;
#endif
using Firebase.RemoteConfig;
using HadesSDK.Ads.Core;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.FirebaseServices
{
    public class FirebaseServiceHades : FirebaseService
    {
        private RemoteConfig _remoteConfig;
        private FieldInfo[] _remoteFields;
        private Dictionary<string, object> _remoteConfigMap = new Dictionary<string, object>();

        public FirebaseServiceHades(bool shouldUseCacheRemote)
        {
            CreateDefaultConfig(shouldUseCacheRemote);
        }

        //To do: option to use cached remote value from previous session, but i dont think anyone would need it
        void CreateDefaultConfig(bool shouldUseCacheRemote)
        {
            _remoteConfig = new RemoteConfig();

            _remoteFields = _remoteConfig.GetType().GetFields().Where(info =>
            {
                return info.FieldType == typeof(bool) || info.FieldType == typeof(float) ||
                       info.FieldType == typeof(string);
            }).ToArray();

            for (var i = 0; i < _remoteFields.Length; i++)
            {
                var info = _remoteFields[i];

                _remoteConfigMap.Add(info.Name, info.GetValue(_remoteConfig));
            }
        }

        public override void Init()
        {
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp,
                    // where app is a Firebase.FirebaseApp property of your application class.
                    InitializeFirebase();
                    FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(_remoteConfigMap)
                        .ContinueWithOnMainThread(task => { FetchDataAsync(); });
                    // Set a flag here to indicate whether Firebase is ready to use by your app.
                    Debug.Log("Firebase Initialized");
                }
                else
                {
                    UnityEngine.Debug.LogError(System.String.Format(
                        "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                    // Firebase Unity SDK is not safe to use here.
                }
            });
        }

        public Task FetchDataAsync()
        {
            Debug.Log("Fetching data...");
            System.Threading.Tasks.Task fetchTask =
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
                    TimeSpan.Zero);
            return fetchTask.ContinueWithOnMainThread(FetchComplete);
        }

        private void FetchComplete(Task fetchTask)
        {
            if (fetchTask.IsCanceled)
            {
                Debug.Log("Fetch canceled.");
            }
            else if (fetchTask.IsFaulted)
            {
                Debug.Log("Fetch encountered an error.");
            }
            else if (fetchTask.IsCompleted)
            {
                Debug.Log("Fetch completed successfully!");
            }

            var info = FirebaseRemoteConfig.DefaultInstance.Info;

            switch (info.LastFetchStatus)
            {
                case LastFetchStatus.Success:
                    FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                        .ContinueWithOnMainThread(task =>
                        {
                            for (var i = 0; i < _remoteFields.Length; i++)
                            {
                                var fieldInfo = _remoteFields[i];

                                string key = fieldInfo.Name;
                                var remoteConfig = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                                if (remoteConfig.Source == ValueSource.RemoteValue)
                                {
                                    try
                                    {
                                        if (fieldInfo.FieldType == typeof(float))
                                        {
                                            var value = (float)remoteConfig.DoubleValue;
                                            fieldInfo.SetValue(_remoteConfig, value);
                                            Debug.Log($"Fetch Remote: {fieldInfo.Name} {value.ToString()}");
                                        }
                                        else if (fieldInfo.FieldType == typeof(bool))
                                        {
                                            var value = remoteConfig.BooleanValue;
                                            fieldInfo.SetValue(_remoteConfig, value);
                                            Debug.Log($"Fetch Remote: {fieldInfo.Name} {value.ToString()}");
                                        }
                                        else if (fieldInfo.FieldType == typeof(string))
                                        {
                                            var value = remoteConfig.StringValue;
                                            fieldInfo.SetValue(_remoteConfig, value);
                                            Debug.Log($"Fetch Remote: {fieldInfo.Name} {value.ToString()}");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError(e);
                                        continue;
                                    }
                                }
                            }

                            HasFetchedRemoteConfigs = true;
                            onRemoteConfigUpdate?.Invoke();
                        });
                    break;
                case LastFetchStatus.Failure:
                    switch (info.LastFetchFailureReason)
                    {
                        case FetchFailureReason.Error:
                            Debug.Log("Error");
                            break;
                        case FetchFailureReason.Throttled:
                            Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
                            break;
                    }

                    break;
                case LastFetchStatus.Pending:
                    Debug.Log("Latest Fetch call still pending.");
                    break;
            }
            
            onRemoteFetchCompleted?.Invoke();
        }

        protected void InitializeFirebase()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
#if FIREBASE_MESS
            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
            FirebaseMessaging.MessageReceived -= OnMessageReceived;
            FirebaseMessaging.TokenReceived -= OnTokenReceived;
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;
#endif
            // Set a flag here to indicate whether Firebase is ready to use by your app.
            IsInit = true;
        }

#if FIREBASE_MESS
        public void OnTokenReceived(object sender, TokenReceivedEventArgs token)
        {
            Debug.Log("Received Registration Token: " + token.Token);
        }

        public void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.Log("Received a new message from: " + e.Message.From);
        }
#endif

        public void OnDestroy()
        {
#if FIREBASE_MESS
            if (IsInit)
            {
                FirebaseMessaging.MessageReceived -= OnMessageReceived;
                FirebaseMessaging.TokenReceived -= OnTokenReceived;
            }
#endif
        }

        public override void LogEvent(string eventKey, params EventParameter[] parameters)
        {
            try
            {
                if (parameters != null && parameters.Length > 0)
                {
                    Parameter[] firebaseParams = new Parameter[parameters.Length];

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        firebaseParams[i] = ConvertBaseEventParameterToParameter(parameters[i]);
                    }

                    FirebaseAnalytics.LogEvent(eventKey, firebaseParams);
                }
                else
                {
                    FirebaseAnalytics.LogEvent(eventKey, new Parameter("",""));
                } 
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public Parameter ConvertBaseEventParameterToParameter(EventParameter eventParameter)
        {
            return new Parameter(eventParameter.eventName, eventParameter.eventValue);
        }

        public override void LogAdPaidEvent(AdService.AdValue adValue)
        {
            double revenue = adValue.value;
            var impressionParameters = new[] {
                new Firebase.Analytics.Parameter("ad_platform", adValue.adPlatform.ToString()),
                new Firebase.Analytics.Parameter("ad_source", adValue.adNetwork),
                new Firebase.Analytics.Parameter("ad_unit_name", adValue.adIdentifier),
                new Firebase.Analytics.Parameter("ad_format", adValue.adType.ToString()), // Please check this - as wecouldn't find format refereced in your unity docshttps://dash.applovin.com/documentation/mediation/unity/getting-started/advanced-settings#impression-level-user-revenue - api
                new Firebase.Analytics.Parameter("placement", adValue.placement),
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", "USD"), // All Applovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
        }

        public override void LogAdNonDeterminePaidEvent(AdService.AdValueNonDetermine adValue)
        {
            double revenue = adValue.value;
            var impressionParameters = new[] {
                new Firebase.Analytics.Parameter("ad_platform", adValue.adPlatform.ToString()),
                new Firebase.Analytics.Parameter("ad_source", adValue.adNetwork),
                new Firebase.Analytics.Parameter("ad_unit_name", adValue.adIdentifier),
                new Firebase.Analytics.Parameter("ad_format", adValue.adType), // Please check this - as wecouldn't find format refereced in your unity docshttps://dash.applovin.com/documentation/mediation/unity/getting-started/advanced-settings#impression-level-user-revenue - api
                new Firebase.Analytics.Parameter("placement", ""),
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", "USD"), // All Applovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
        }

        public override void LogAdEvent(string eventName, string placement, string error = "")
        {
            try
            {
                if (string.IsNullOrEmpty(placement))
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName,
                            new Firebase.Analytics.Parameter("placement", placement));
                    }
                    else
                    {
                        Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
                    }
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName,
                        new Firebase.Analytics.Parameter("placement", placement),
                        new Firebase.Analytics.Parameter("error", error));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public override T GetRemoteConfig<T>()
        {
            if (typeof(T) == _remoteConfig.GetType())
            {
                return _remoteConfig as T;
            }

            Debug.LogError("Incorrect remote config format");
            return null;
        }
    }
}
#endif