using Core;
using UnityEngine;

namespace Sand
{
    public class EffectGame : GameElement
    {
        [SerializeField] private GameObject _effectLevelUp;
        
        public void OpenEffectLevelUp()
        {
            _effectLevelUp.SetActive(true);
        }

        public void CloseEffectLevelUp()
        {
            _effectLevelUp.SetActive(false);
        }
    }
}