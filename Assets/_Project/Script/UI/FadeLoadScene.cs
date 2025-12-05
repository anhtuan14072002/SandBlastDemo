using UnityEngine;
using UnityEngine.UI;

namespace Sand
{
    public class FadeLoadScene : MonoBehaviour
    {
        [SerializeField] private Image _fadeImage;

        private void Awake()
        {
            _fadeImage.gameObject.SetActive(true);
        }
    }
}