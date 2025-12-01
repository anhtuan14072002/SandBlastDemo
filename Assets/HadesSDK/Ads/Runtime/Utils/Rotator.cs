using UnityEngine;

namespace HadesSDK.Ads.Runtime.Utils
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private bool _shouldReset;
        public Vector3 direction = new Vector3(0, 0, 1f);
        public float speed = 1f;

        void Update () {
            transform.Rotate(direction * (speed * Time.deltaTime * 100f));
        }
        private void OnEnable()
        {
            if (_shouldReset)
            {
                transform.eulerAngles = Vector3.zero;
            }
        }
    }
}