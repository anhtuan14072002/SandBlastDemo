using UnityEngine;

namespace HadesSDK.Ads.Runtime.Utils
{
    public class UtilityCanvas : MonoBehaviour
    {
        [SerializeField] private FloatingAndFadeOutText _adNotReadyTextPrefab;
        
        [SerializeField] private RectTransform _loadingAdPopup;

        public void DisplayAdLoadingPopup(bool isDisplay)
        {
            _loadingAdPopup.gameObject.SetActive(isDisplay);
        }

        public void NotifyAdNotReady()
        {
            FloatingAndFadeOutText noAdsText = Instantiate(_adNotReadyTextPrefab, transform);
            noAdsText.transform.position = _adNotReadyTextPrefab.transform.position;
            noAdsText.Play("Ad Is Loading!");
        }
    }
}