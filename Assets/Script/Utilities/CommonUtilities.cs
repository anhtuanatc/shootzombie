using UnityEngine;

namespace ShootZombie.Utilities
{
    /// <summary>
    /// Simple component to automatically destroy a GameObject after a delay.
    /// Useful for particle effects, temporary objects, etc.
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private bool useScaledTime = true;
        
        private float _timer;

        private void OnEnable()
        {
            _timer = 0f;
        }

        private void Update()
        {
            _timer += useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
            
            if (_timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Sets the lifetime and resets the timer.
        /// </summary>
        public void SetLifetime(float newLifetime)
        {
            lifetime = newLifetime;
            _timer = 0f;
        }
    }

    /// <summary>
    /// Destroys a GameObject when a particle system is complete.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class DestroyOnParticleComplete : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (_particleSystem != null && !_particleSystem.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Rotates an object continuously.
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 90, 0);
        [SerializeField] private bool useScaledTime = true;

        private void Update()
        {
            float deltaTime = useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
            transform.Rotate(rotationSpeed * deltaTime);
        }
    }

    /// <summary>
    /// Makes an object bob up and down.
    /// </summary>
    public class FloatUpDown : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.5f;
        [SerializeField] private float frequency = 1f;
        [SerializeField] private bool randomOffset = true;

        private Vector3 _startPosition;
        private float _offset;

        private void Start()
        {
            _startPosition = transform.position;
            _offset = randomOffset ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        }

        private void Update()
        {
            float y = Mathf.Sin((Time.time + _offset) * frequency) * amplitude;
            transform.position = _startPosition + Vector3.up * y;
        }
    }

    /// <summary>
    /// Billboard effect - always face camera.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private bool lockYAxis = true;

        private UnityEngine.Camera _mainCamera;

        private void Start()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) return;

            if (lockYAxis)
            {
                Vector3 lookDirection = _mainCamera.transform.position - transform.position;
                lookDirection.y = 0;
                
                if (lookDirection.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(-lookDirection);
                }
            }
            else
            {
                transform.LookAt(_mainCamera.transform);
            }
        }
    }
}
