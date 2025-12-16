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
        [SerializeField] private float _flashDuration = 0.3f;
        [SerializeField] private float _returnDuration = 0.2f;

        private Color32 _originalColor;
        private CancellationTokenSource _cancellationTokenSource;

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
            float elapsed = 0f;

            while (elapsed < _flashDuration)
            {
                if (cancellationToken.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _flashDuration);
                _background.color = Color32.Lerp(startColor, targetColor, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            elapsed = 0f;
            while (elapsed < _returnDuration)
            {
                if (cancellationToken.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _returnDuration);
                _background.color = Color32.Lerp(targetColor, startColor, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            _background.color = startColor;
        }
    }
}