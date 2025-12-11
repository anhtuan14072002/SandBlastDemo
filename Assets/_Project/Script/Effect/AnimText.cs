using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Core
{
    public static class AnimText
    {
        public static async UniTask AnimateNumberChange(TextMeshProUGUI textComponent, int startValue, int endValue,
            float duration, float frequency, GameObject icon, bool isAcronym, float delay = 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            float elapsedTime = 0;
            var startColor = Color.white;
            var targetColor = Color.red;
            float tweenInterval = 0.2f;
            float lastTweenTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / duration;
                var currentValue = (int)Mathf.Lerp(startValue, endValue, progress);
                
                if (isAcronym)
                   textComponent.text = NumberFormat.Format(currentValue);
                else
                    textComponent.text = currentValue.ToString();

                if (endValue < startValue)
                {
                    var colorLerp = (Mathf.Sin(Time.time * 10f) + 1f) * 0.5f;
                    textComponent.color = Color.Lerp(startColor, targetColor, colorLerp);
                }
                else if (endValue > startValue)
                {
                    if (Time.time - lastTweenTime >= tweenInterval)
                    {
                        if (icon == null) return;
                        _ = Tween.PunchScale(icon.transform, new Vector3(0.35f, 0.35f), 0.1f, frequency);
                        lastTweenTime = Time.time;
                    }
                }

                await UniTask.Yield();
            }

            // textComponent.text = NumberFormat.Format(endValue);
            if (isAcronym)
                textComponent.text = NumberFormat.Format(endValue);
            else
                textComponent.text = endValue.ToString();
            
            if (endValue < startValue)
            {
                textComponent.color = Color.white;
            }
        }
    }
    public static class NumberFormat
    {
        public static string Format(int value)
        {
            if (value >= 1_000_000)
            {
                float m = value / 1_000_000f; 
                return m.ToString("0.##") + "M"; 
            }

            if (value >= 1_000)
            {
                float k = value / 1_000f;
                return k.ToString("0.##") + "k"; 
            }

            return value.ToString("N0");
        }
    }

}