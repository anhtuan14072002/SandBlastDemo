using Core;
using UnityEngine;

namespace Sand
{
    public class WarningSand : GameElement,
        IReceive<SignalWarningSand>
    {
        [SerializeField] private GameObject _warningSand;
        public void Receive(in SignalWarningSand signal)
        {
            ToggleWarningSand(signal.IsWarning);
        }
        public void ToggleWarningSand(bool isWarningSand)
        {
            _warningSand.SetActive(isWarningSand);
        }
    }
}