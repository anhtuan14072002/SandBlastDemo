using UnityEngine;

namespace Sand
{
    public class EffectTextScoreCombo : BaseEffectPool<SignalOpenEffectTextScoreCombo>
    {
        protected override void ConfigureEffect(GameObject effect, string text = "")
        {
            throw new System.NotImplementedException();
        }

        public override void Receive(in SignalOpenEffectTextScoreCombo signal)
        {
            throw new System.NotImplementedException();
        }
    }
}