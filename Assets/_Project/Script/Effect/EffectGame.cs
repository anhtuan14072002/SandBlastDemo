using System;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sand
{
    public class EffectGame : GameElement
    {
        [FormerlySerializedAs("_effectLevelUp")]
        [SerializeField] private GameObject _effectFirework;
        [SerializeField] private GameObject _effectClaimGem;
        
        public void OpenEffectLevelUp()
        {
            _effectFirework.SetActive(true);
        }

        public void CloseEffectLevelUp()
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

        public async UniTask OpenEffectTime()
        {
            OpenEffectClaimGem();
            await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
            CloseEffectClaimGem();
        }
    }
}