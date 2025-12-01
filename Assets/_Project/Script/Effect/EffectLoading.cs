using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace Sand
{
    public class EffectLoading : MonoBehaviour
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
                await  UniTask.WaitForSeconds(Random.Range(0.1f, 0.25f));
            }
            SceneManager.LoadScene("GamePlay");
        }

        private void UpdateUI(float value)
        {
            _fillImage.fillAmount = value;
            _textPercent.text = $"{(int)(value * 100)}%";
        }
    }
}

/*using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sand
{
    public class EffectLoading : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textPercent;
        [SerializeField] private Image _fillImage;
        [SerializeField] private float _duration = 1f;

        private float _timer = 0f;
        private bool _isDone = false;

        private void Update()
        {
            if (_isDone) return;
            _timer += Time.deltaTime;

            float t = Mathf.Clamp01(_timer / _duration);
            float value = Mathf.Lerp(0f, 1f, t);

            _fillImage.fillAmount = value;
            _textPercent.text = $"{(int)(value * 100)}%";

            if (t >= 1f)
            {
                _isDone = true;
                LoadScene();
            }
        }

        private void LoadScene()
        {
            SceneManager.LoadScene("GamePlay");
        }
    }
}*/