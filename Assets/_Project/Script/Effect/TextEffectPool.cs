using UnityEngine;
using TMPro;

namespace Sand
{
    public abstract class TextEffectPool<T> : BaseEffectPool<T>
    {
        protected override void ConfigureEffect(GameObject effect, string text = "")
        {
            effect.transform.localScale = Vector3.one;
            effect.transform.position = transform.position;

            if (!string.IsNullOrEmpty(text))
            {
                var textComp = effect.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp == null)
                    textComp = effect.GetComponentInChildren<TextMeshProUGUI>();

                if (textComp != null)
                {
                    textComp.text = text;
                }
            }
        }
    }
}