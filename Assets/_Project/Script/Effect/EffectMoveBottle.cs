using System;
using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace Sand
{
    public class EffectMoveBottle : GameElement,
        IReceive<SignalMoveBottleDraw>
    {
        [SerializeField] private GameObject _bottle;
        [SerializeField] private Transform _originalPos;
        [SerializeField] private Transform _targetPos;

        private void Move()
        {
            Tween.Position(_bottle.transform, _targetPos.position, 0.25f, Ease.Linear);
            Tween.LocalRotation(_bottle.transform, Quaternion.Euler(0, 0, 90), 0.2f);
            EndMove().Forget();
        }
        
        private async UniTask EndMove()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            Tween.LocalRotation(_bottle.transform, Quaternion.identity, 0.2f);
            Tween.Position(_bottle.transform, _originalPos.position, 0.25f);
        }
        public void Receive(in SignalMoveBottleDraw signal)
        {
            Move();
        }
    }
}