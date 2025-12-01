// using DG.Tweening;

using PrimeTween;
using Sand;
using TMPro;
using UnityEngine;

namespace HadesSDK.Ads.Runtime.Utils
{
    public class FloatingAndFadeOutText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        public void Play(string text)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 targetAnchorPos = rectTransform.anchoredPosition + Vector2.up * 200;
            
            _text.text = text;
            gameObject.SetActive(true);
            Tween.Alpha(_text, 0f, 1f, 1f, Ease.InQuart );
            rectTransform.TweenAnchoredY(targetAnchorPos.y, 1f, Ease.InQuart).OnComplete(() => Destroy(gameObject));
            
            /*_text.DOFade(0f, 1f).SetEase(Ease.InQuart);
            rectTransform.DOAnchorPos(targetAnchorPos, 1f).OnComplete((() =>
            {
                Destroy(gameObject);
            }));*/
        }
    }
}