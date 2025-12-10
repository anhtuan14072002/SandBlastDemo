namespace Sand
{
    public class EffectDrawCompleted : SimpleEffectPool<SignalOpenEffectDrawCompleted>
    {
        public override void Receive(in SignalOpenEffectDrawCompleted signal)
        {
            GetEffect();
        }
    }
}