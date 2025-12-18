using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sand
{
    public class BackgroundSync : GameElement,
        IReceive<SignalSyncBackgroundGamePlay>
    {
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private float _flashDuration = 1f;
        [SerializeField] private float _returnDuration = 1.1f;
        [Header("Flash Intensity")]
        [SerializeField, Range(0f, 2f)] private float _flashIntensity = 1f;
        
        private CancellationTokenSource _cancellationTokenSource;
        private Color32 _originalColor;

        private void Start()
        {
            _originalColor = _background.color;
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        public void Receive(in SignalSyncBackgroundGamePlay signal)
        {
            SetBackgroundWithFlash(signal.Color);
        }

        public void SetBackground(Color32 color)
        {
            _background.color = color;
        }

        public void SetBackgroundWithFlash(Color32 targetColor)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            FlashBackgroundAsync(targetColor, _cancellationTokenSource.Token).Forget();
        }

        private async UniTask FlashBackgroundAsync(Color32 targetColor, CancellationToken cancellationToken)
        {
            Color32 startColor = _originalColor;
            Color32 adjustedTarget = ApplyIntensity(startColor, targetColor, _flashIntensity);

            float elapsed = 0f;

            while (elapsed < _flashDuration)
            {
                if (cancellationToken.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _flashDuration);
                _background.color = Color32.Lerp(startColor, adjustedTarget, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            elapsed = 0f;
            while (elapsed < _returnDuration)
            {
                if (cancellationToken.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _returnDuration);
                _background.color = Color32.Lerp(adjustedTarget, startColor, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _background.color = startColor;
        }

        private Color32 ApplyIntensity(Color32 original, Color32 target, float intensity)
        {
            if (intensity <= 0f) return original;
            if (Mathf.Approximately(intensity, 1f)) return target;
            return Color32.Lerp(original, target, Mathf.Clamp01(intensity));
        }
    }
}