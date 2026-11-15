using UnityEngine;

namespace Fightyub
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = static with world, 0.05 = slow distant sky, 1.0 = moves 1:1 with foreground/ground")]
        [Range(0f, 1f)]
        public float parallaxFactor = 1f;

        private Transform _cameraTransform;
        private Vector3 _initialPosition;
        private Vector3 _initialCameraPosition = new Vector3(0f, 0f, -10f);
        private bool _initialized;

        private void Awake()
        {
            InitializeOrigin();
        }

        private void OnEnable()
        {
            InitializeOrigin();
            UpdateParallax();
        }

        private void Start()
        {
            InitializeOrigin();
            UpdateParallax();
        }

        public void InitializeOrigin()
        {
            if (_initialized) return;
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            _initialPosition = transform.position;
            _initialCameraPosition = new Vector3(0f, 0f, -10f);
            _initialized = true;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            _initialPosition = newOrigin;
            transform.position = newOrigin;
            _initialCameraPosition = (Camera.main != null) ? Camera.main.transform.position : new Vector3(0f, 0f, -10f);
            _initialized = true;
            UpdateParallax();
        }

        public void ReinitializeOrigin()
        {
            _initialPosition = transform.position;
            _initialCameraPosition = (Camera.main != null) ? Camera.main.transform.position : new Vector3(0f, 0f, -10f);
            _initialized = true;
            UpdateParallax();
        }

        private void LateUpdate()
        {
            UpdateParallax();
        }

        public void UpdateParallax()
        {
            if (!_initialized) InitializeOrigin();
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            if (_cameraTransform == null) return;

            Vector3 camDelta = _cameraTransform.position - _initialCameraPosition;
            // A layer with parallaxFactor = 0.05 moves almost with the camera (stays fixed in background),
            // while parallaxFactor = 1.0 stays anchored in world space.
            float moveX = camDelta.x * (1f - parallaxFactor);
            float moveY = camDelta.y * (1f - parallaxFactor) * 0.5f;
            transform.position = new Vector3(_initialPosition.x + moveX, _initialPosition.y + moveY, _initialPosition.z);
        }
    }
}
