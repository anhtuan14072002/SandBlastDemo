/*using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Runtime;
using Random = UnityEngine.Random;

namespace Sand
{
    public class LoadingGame : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textPercent;
        [SerializeField] private Image _fillImage;

        private float currentValue = 0f;
        private float targetValue = 0f;

        private readonly int[] steps = { 3, 8, 15, 32, 55, 80, 92, 98, 100 };

        private void Start()
        {
            FakeLoadingRoutine().Forget();
        }

        private async UniTask FakeLoadingRoutine()
        {
            foreach (int step in steps)
            {
                targetValue = step / 100f;

                while (currentValue < targetValue)
                {
                    currentValue = Mathf.MoveTowards(currentValue, targetValue, Time.deltaTime * 0.6f);
                    UpdateUI(currentValue);
                    await UniTask.Yield();
                }

                if (step == 55)
                {
                    AdManager.Instance.Init();
                }
                await UniTask.WaitForSeconds(Random.Range(0.1f, 0.25f));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            SceneManager.LoadScene("GamePlay");
        }

        private void UpdateUI(float value)
        {
            _fillImage.fillAmount = value;
            _textPercent.text = $"{(int)(value * 100)}%";
        }
    }
}*/
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using HadesSDK.Ads.Runtime;
using Random = UnityEngine.Random;
using System;
using HadesSDK.Ads.Core;

namespace Sand
{
    public class LoadingGame : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textPercent;
        [SerializeField] private Image _fillImage;

        private float currentValue = 0f;
        private float targetValue = 0f;

        private readonly int[] steps = { 8, 15, 55, 80, 92, 98, 100 };
        private const float AD_LOAD_TIMEOUT = 10f;

        private void Start()
        {
            FakeLoadingRoutine().Forget();
        }

        private async UniTask FakeLoadingRoutine()
        {
            var a = SceneManager.LoadSceneAsync("GamePlay");
            a.allowSceneActivation = false;
            foreach (int step in steps)
            {
                targetValue = step / 100f;

                while (currentValue < targetValue)
                {
                    currentValue = Mathf.MoveTowards(currentValue, targetValue, Time.deltaTime * 0.6f);
                    UpdateUI(currentValue);
                    await UniTask.Yield();
                }

                await UniTask.WaitForSeconds(Random.Range(0.1f, 0.25f));
            }

            await ShowAdWithTimeout();
            await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
            a.allowSceneActivation = true;
            // SceneManager.LoadScene("GamePlay");
        }

        private async UniTask ShowAdWithTimeout()
        {
            try
            {
                AdManager.Instance.Init();
                var adLoadTask = WaitForAdLoad();
                var timeoutTask = UniTask.WaitForSeconds(AD_LOAD_TIMEOUT);
                var completed = await UniTask.WhenAny(adLoadTask, timeoutTask);
                
                if (completed == 0)
                {
                    if (FirebaseService.Instance != null)
                    {
                        Debug.Log("ok");
                    }
                    else
                    {
                        Debug.Log("FirebaseService is null");
                    }
                    
                    AdManager.Instance.ShowAoa();
                }
                else
                {
                    Debug.Log("Quá Timeout ");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"lỗi ad : {ex.Message}");
            }
        }

        private async UniTask WaitForAdLoad()
        {
            while (!IsAdReady())
            {
                await UniTask.Yield();
            }
        }

        private bool IsAdReady()
        {
            try
            {
                return AdManager.Instance.IsAoaReady();
            }
            catch
            {
                return false;
            }
        }
        private void UpdateUI(float value)
        {
            _fillImage.fillAmount = value;
            _textPercent.text = $"{(int)(value * 100)}%";
        }
    }
}