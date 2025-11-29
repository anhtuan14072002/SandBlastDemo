using UnityEngine;

namespace Sand
{
    public abstract class SimpleEffectPool<T> : BaseEffectPool<T>
    {
        protected override void ConfigureEffect(GameObject effect, string text = "")
        {
            effect.transform.localScale = Vector3.one;
            effect.transform.position = transform.position;
        }
    }
}