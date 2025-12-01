using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.Utils
{
    public static class ActionUtility
    {
        public static async UniTaskVoid StartActionDelay(Action action, float delaySecs, CancellationToken ct = default)
        {
            await UniTask.WaitForSeconds(delaySecs, cancellationToken: ct);
            action?.Invoke();
        }

        public static async UniTaskVoid StartActionOnMainThread(Action action, CancellationToken ct = default, bool suppressError =  true)
        {
            await UniTask.SwitchToMainThread();
            if (suppressError)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}