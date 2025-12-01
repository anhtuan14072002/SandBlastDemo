using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class GDPRScript : MonoBehaviour
    {
        public bool ShowGDPRPopupDone = false;
        [SerializeField] bool test;
        public void CallGDPR()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetiOSAppPauseOnBackground(true);
#if UNITY_EDITOR
            Invoke(nameof(InitADS), Time.deltaTime);
            return;
#endif
            var debugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA,
                TestDeviceHashedIds = new List<string> { "B481B40EA3991523BEF337A7ABB1E229", "042343634A935EBC371BC1696E2D7826", "0FC6649D954AF2F6297E3991920E151D" }
            };
            ConsentRequestParameters request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false, };
            if (test)
            {
                request = new ConsentRequestParameters
                {
                    TagForUnderAgeOfConsent = false,
                    ConsentDebugSettings = debugSettings,
                };
            }
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }

        void OnConsentInfoUpdated(FormError consentError)
        {
            if (consentError != null)
            {
                // Handle the error.
                UnityEngine.Debug.LogError(consentError);
                Invoke(nameof(InitADS), Time.deltaTime);
                return;
            }


            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                Invoke(nameof(InitADS), Time.deltaTime);
                if (formError != null)
                {
                    // Consent gathering failed.
                    UnityEngine.Debug.LogError(consentError);
                    return;
                }

                //if (ConsentInformation.CanRequestAds())
                //{
                //    AdsManager.Instance.Init();
                //}
                // Consent has been gathered.
            });
        }

        void InitADS()
        {
            ShowGDPRPopupDone = true;
            HadesSDK.Ads.Runtime.AdManager.Instance.Init();
        }
    }
}
