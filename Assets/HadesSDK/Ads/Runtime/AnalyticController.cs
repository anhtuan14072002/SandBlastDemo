using System;
using HadesSDK.Ads.Core;
using HadesSDK.Ads.Runtime.AnalyticServices;
using HadesSDK.Ads.Runtime.FirebaseServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HadesSDK.Ads.Runtime
{
    public class AnalyticController : MonoBehaviour
    {
        #region Singleton

        public static AnalyticController Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        #endregion
        
#if APPSFLYER
        [Header("APPSFLYER")]
        [SerializeField] private GameObject _appsflyerObjectScripObj;
#endif
#if ADJUST
        [Header("ADJUST")]
        [SerializeField] private string _adjustAppKey;
        [SerializeField] private com.adjust.sdk.AdjustEnvironment _adjustEnvironment;
#endif
        
        private MmpService _mmpService;
        private FirebaseService _firebaseService;

        public RemoteConfig RemoteConfig { get; private set; }

        public void InitFirebaseBody()
        {
#if FIREBASE
            _firebaseService = new FirebaseServiceHades(false);
#else
            _firebaseService = new FirebaseServiceDummy();
#endif
            _firebaseService.onRemoteConfigUpdate += OnUpdateRemote;
            FirebaseService.Instance = _firebaseService;
            RemoteConfig = _firebaseService.GetRemoteConfig<RemoteConfig>();
        }

        void OnUpdateRemote()
        {
            RemoteConfig = _firebaseService.GetRemoteConfig<RemoteConfig>();
        }

        public void InitMmpServiceBody()
        {
#if APPSFLYER
            _mmpService = new AppsflyerController(_appsflyerObjectScripObj.GetComponent<AppsflyerObject>());
#elif ADJUST
            _mmpService = new AdjustController(_adjustAppKey, _adjustEnvironment);
#else
            _mmpService = new MmpServiceDummy();
#endif
        }

        public MmpService GetMmpService()
        {
            return _mmpService;
        }

        public FirebaseService GetFirebaseService()
        {
            return _firebaseService;
        }


        private void OnDestroy()
        {
            FirebaseService.Instance = null;
        }
        
    }

    public enum MmpType
    {
        Dummy = 0,
        Appsflyer = 1,
        Adjust = 2,
    }
    
}