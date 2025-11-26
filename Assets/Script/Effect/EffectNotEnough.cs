using Core;

namespace Sand
{
    public class EffectNotEnough : SimpleEffectPool<SignalOpenEffectNotEnough>
    {
        public override void Receive(in SignalOpenEffectNotEnough signal)
        {
            GetEffect();
        }
    }
}