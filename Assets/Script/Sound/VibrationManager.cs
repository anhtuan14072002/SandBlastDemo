using Lofelt.NiceVibrations;
using UnityEngine;

namespace Sand
{
    public class VibrationManager : MonoBehaviour
    {
        private bool _isVibration = false;

        public virtual void SelectionButton()
        {
            if (!_isVibration) HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
        }

        public void StopVibration()
        {
            _isVibration = true;
        }

        public void StartVibration()
        {
            _isVibration = false;
        }
    }
}