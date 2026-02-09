using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Camera
{
    /// <summary>
    /// Camera controller that follows the player with smooth movement.
    /// Supports camera shake for impact feedback.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;
        
        [Header("Follow Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 12, -11);
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private bool useSmoothing = true;
        
        [Header("Bounds")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);
        
        [Header("Camera Shake")]
        [SerializeField] private float defaultShakeDuration = 0.2f;
        [SerializeField] private float defaultShakeMagnitude = 0.3f;
        
        [Header("Look At")]
        [SerializeField] private bool lookAtTarget = false;
        [SerializeField] private float lookAtSmoothSpeed = 5f;
        
        #endregion

        #region Properties
        
        /// <summary>Current camera target</summary>
        public Transform Target => target;
        
        /// <summary>Is camera shake currently active?</summary>
        public bool IsShaking => _isShaking;
        
        #endregion

        #region Private Fields
        
        private Vector3 _desiredPosition;
        private Vector3 _smoothedPosition;
        private Vector3 _shakeOffset;
        
        private bool _isShaking;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeMagnitude;
        
        // Optimization: Cache FindPlayer cooldown
        private float _findPlayerCooldown = 0f;
        private const float FIND_PLAYER_INTERVAL = 0.5f;
        
        // Cache target Rigidbody for smooth physics tracking
        private Rigidbody _targetRigidbody;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (target == null && autoFindPlayer)
            {
                FindPlayer();
            }
        }

        private void Start()
        {
            // Initialize position immediately to prevent lerp from starting position
            if (target != null)
            {
                // Cache target Rigidbody for smooth physics tracking
                _targetRigidbody = target.GetComponent<Rigidbody>();
                
                transform.position = target.position + offset;
                _smoothedPosition = transform.position;
            }
        }

        private void LateUpdate()
        {
            // Optimized: Only find player every 0.5s instead of every frame
            if (target == null)
            {
                if (autoFindPlayer && Time.time >= _findPlayerCooldown)
                {
                    FindPlayer();
                    _findPlayerCooldown = Time.time + FIND_PLAYER_INTERVAL;
                }
                return;
            }
            
            UpdateCameraPosition();
            UpdateCameraRotation();
            UpdateShake();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Initialization
        
        private void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnPlayerShoot += HandlePlayerShoot;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnPlayerShoot -= HandlePlayerShoot;
        }
        
        #endregion

        #region Camera Movement
        
        private void UpdateCameraPosition()
        {
            // Get target position - use Rigidbody position if available for smooth physics tracking
            Vector3 targetPosition;
            bool isPhysicsTarget = false;
            
            if (_targetRigidbody != null)
            {
                // Use Rigidbody.position for physics objects (syncs with interpolation)
                targetPosition = _targetRigidbody.position;
                isPhysicsTarget = true;
            }
            else
            {
                // Use Transform.position for non-physics objects
                targetPosition = target.position;
            }
            
            // Calculate desired position
            _desiredPosition = targetPosition + offset;
            
            // Apply bounds
            if (useBounds)
            {
                _desiredPosition.x = Mathf.Clamp(_desiredPosition.x, minBounds.x, maxBounds.x);
                _desiredPosition.z = Mathf.Clamp(_desiredPosition.z, minBounds.y, maxBounds.y);
            }
            
            // Apply smoothing
            // IMPORTANT: Don't smooth physics targets - they already have Rigidbody interpolation!
            // Double interpolation causes jitter
            if (useSmoothing && !isPhysicsTarget)
            {
                // Only smooth non-physics targets
                Vector3 currentPos = transform.position - _shakeOffset;
                float smoothFactor = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _smoothedPosition = Vector3.Lerp(currentPos, _desiredPosition, smoothFactor);
            }
            else
            {
                // No smoothing for physics targets - follow directly
                _smoothedPosition = _desiredPosition;
            }
            
            // Apply final position (with shake offset)
            transform.position = _smoothedPosition + _shakeOffset;
        }

        private void UpdateCameraRotation()
        {
            if (!lookAtTarget) return;
            
            Vector3 lookDirection = target.position - transform.position;
            
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    lookAtSmoothSpeed * Time.deltaTime
                );
            }
        }
        
        #endregion

        #region Camera Shake
        
        /// <summary>
        /// Triggers camera shake with default parameters.
        /// </summary>
        public void Shake()
        {
            Shake(defaultShakeDuration, defaultShakeMagnitude);
        }

        /// <summary>
        /// Triggers camera shake with custom parameters.
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            _isShaking = true;
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
            _shakeTimer = duration;
        }

        private void UpdateShake()
        {
            if (!_isShaking) return;
            
            if (_shakeTimer > 0)
            {
                // Calculate shake offset
                float progress = _shakeTimer / _shakeDuration;
                float currentMagnitude = _shakeMagnitude * progress; // Fade out
                
                _shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentMagnitude,
                    Random.Range(-1f, 1f) * currentMagnitude,
                    Random.Range(-1f, 1f) * currentMagnitude
                );
                
                _shakeTimer -= Time.deltaTime;
            }
            else
            {
                _isShaking = false;
                _shakeOffset = Vector3.zero;
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void HandlePlayerDamaged(int damage)
        {
            // Shake based on damage amount
            float magnitude = Mathf.Min(defaultShakeMagnitude * (damage / 20f), 0.8f);
            Shake(defaultShakeDuration, magnitude);
        }

        private void HandlePlayerShoot()
        {
            // Subtle shake on shoot
            Shake(0.05f, 0.05f);
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets a new camera target.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            // Cache new target's Rigidbody
            if (target != null)
            {
                _targetRigidbody = target.GetComponent<Rigidbody>();
            }
            else
            {
                _targetRigidbody = null;
            }
        }

        /// <summary>
        /// Sets the camera offset.
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// Immediately moves camera to target position.
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }
        
        #endregion

        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3(
                    (minBounds.x + maxBounds.x) / 2f,
                    transform.position.y,
                    (minBounds.y + maxBounds.y) / 2f
                );
                Vector3 size = new Vector3(
                    maxBounds.x - minBounds.x,
                    1f,
                    maxBounds.y - minBounds.y
                );
                Gizmos.DrawWireCube(center, size);
            }
        }
        
        #endregion
    }
}
