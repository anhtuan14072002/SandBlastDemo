using Core;
using UnityEngine;

namespace Sand
{
    public class EffectTextScore : BaseEffectPool<SignalOpenEffectTextScore>
    {
        public override void Receive(in SignalOpenEffectTextScore signal)
        {
            GetEffect($"+{signal.Score}", signal.Position, signal.Score);
            Global.Send(new SignalScoreOnGame(){Score = signal.Score});
        }

        protected override void ConfigureEffect(GameObject effect, string text = "")
        {
        }
    }
}