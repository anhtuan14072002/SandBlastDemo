using UnityEngine;

namespace Sand
{
    public class PowerUpGame : MonoBehaviour
    {
        private Camera _cam;
        private void Awake()
        {
            _cam = Camera.main;
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                CheckClick();
            }
        }

        private void CheckClick()
        {
            Vector3 mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == this.gameObject)
            {
                Debug.Log("click");
            }
        }
    }
}