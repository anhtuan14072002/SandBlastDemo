#region Demo

using UnityEngine;

namespace Sand
{
    public class FitCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Map width (world units)")]
        [SerializeField] private float _mapWidth = 16f;

        [Header("Padding (world units)")]
        [SerializeField] private float _extraWidth = 0f;

        [Header("Clamp")]
        [SerializeField] private float _minOrthoSize = 7f;

        int _lastW, _lastH;

        private void Reset() => _camera = Camera.main;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
        }

        private void Start() => Apply();

        private void Update()
        {
            if (Screen.width != _lastW || Screen.height != _lastH)
                Apply();
        }

        private void Apply()
        {
            if (_camera == null) return;

            _lastW = Screen.width;
            _lastH = Screen.height;

            float aspect = (float)Screen.width / Screen.height;
            float targetWidth = Mathf.Max(0.01f, _mapWidth + _extraWidth);

            _camera.orthographic = true;
            float sizeByWidth = (targetWidth / 2f) / aspect;
            _camera.orthographicSize = Mathf.Max(sizeByWidth, _minOrthoSize);
        }
    }
}

#endregion
