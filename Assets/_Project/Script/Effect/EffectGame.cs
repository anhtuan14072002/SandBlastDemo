using System;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sand
{
    public class EffectGame : GameElement,
        IReceive<SignaOpenEffecFireWork>
    {
        [FormerlySerializedAs("_effectLevelUp")] [SerializeField]
        private GameObject _effectFirework;
        [SerializeField] private GameObject _effectClaimGem;

        public void OpenEffectFirework()
        {
            _effectFirework.SetActive(true);
        }

        public void CloseEffectFirework()
        {
            _effectFirework.SetActive(false);
        }

        public void OpenEffectClaimGem()
        {
            _effectClaimGem.SetActive(true);
        }

        public void CloseEffectClaimGem()
        {
            _effectClaimGem.SetActive(false);
        }

        public async UniTask OpenEffectFireworkTime()
        {
            OpenEffectFirework();
            await UniTask.Delay(TimeSpan.FromSeconds(3.5f));
            CloseEffectFirework();
        }

        public async UniTask OpenEffectClaimTime()
        {
            OpenEffectClaimGem();
            await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
            CloseEffectClaimGem();
        }

        public void Receive(in SignaOpenEffecFireWork signal)
        {
            OpenEffectFireworkTime().Forget();
        }
    }
}