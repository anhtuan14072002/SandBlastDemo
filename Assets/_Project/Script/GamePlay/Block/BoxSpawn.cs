using HadesSDK.Ads.Runtime;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class BoxSpawn : MonoBehaviour
    {
        [SerializeField] private bool _isLock;
        [SerializeField] private GameObject _iconAds;
        [SerializeField] private int _boxIndex;
        public bool IsLock => _isLock;
        private Camera _cam;
        private UserData _userData;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Start()
        {
            if (_userData != null && _userData.BoxSpawnIsLockState.ContainsKey(_boxIndex))
            {
                _isLock = _userData.BoxSpawnIsLockState[_boxIndex];
            }

            if (_iconAds != null) _iconAds.SetActive(_isLock);
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
            if (!_isLock) return;
            Vector3 mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == this.gameObject)
            {
                ReviveAds();
            }
        }

        public void ReviveAds()
        {
            AdManager.Instance.ShowReward(OnRewardSuccess, OnRewardFail, "ads_buy_more_box");
        }

        private void OnRewardSuccess()
        {
            _isLock = false;
            if (_iconAds != null) _iconAds.SetActive(false);
            _userData.BoxSpawnIsLockState[_boxIndex] = false;
            Debug.Log("Unlocked reserve slot by watching ads");
        }

        private void OnRewardFail()
        {
            Debug.Log("Reward ads failed");
        }

        public void ResetLockState()
        {
            _isLock = true;
            _userData.BoxSpawnIsLockState[_boxIndex] = true;
            if (_iconAds != null) _iconAds.SetActive(true);
        }
    }
}