using UnityEngine;
using Zenject;

namespace Sand
{
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpIcon : MonoBehaviour
    {
        [SerializeField] private PowerUpType _type;
        [Header("Icon Mode")]
        [SerializeField] private bool _isPlusIcon; 
        private Camera _cam;
        PowerUp _powerUp;

        [Inject]
        void Construct(PowerUp powerUp)
        {
            _powerUp = powerUp;
        }

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) OnClick();
        }

        private void OnClick()
        {
            if (_powerUp == null) return;
            if (_powerUp.IsAnyPopupActive()) return;
            
            var mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
                _powerUp.OnClickPowerUp(_type, _isPlusIcon);
        }
    }
}