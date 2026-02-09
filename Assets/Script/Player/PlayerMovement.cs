using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Player
{
    /// <summary>
    /// Handles player movement and rotation.
    /// Supports both keyboard-based rotation and mouse-based aiming.
    /// </summary>
    [RequireComponent(typeof(InputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private bool rotateTowardMouse = true;
        
        [Header("References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        
        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 100f;
        
        #endregion

        #region Properties
        
        /// <summary>Is the player currently moving?</summary>
        public bool IsMoving => _currentVelocity.magnitude > 0.1f;
        
        /// <summary>Current movement velocity</summary>
        public Vector3 CurrentVelocity => _currentVelocity;
        
        /// <summary>Can the player move?</summary>
        public bool CanMove { get; set; } = true;
        
        /// <summary>Can the player rotate? (Set to false when shooting to lock aim direction)</summary>
        public bool CanRotate { get; set; } = true;
        
        #endregion

        #region Private Fields
        
        private InputHandler _input;
        private Animator _animator;
        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        
        private Vector3 _currentVelocity;
        private Vector3 _targetDirection;
        
        // Animator parameter hashes for performance
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            ValidateSetup();
        }

        private void Update()
        {
            if (!CanMove) return;
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;
            
            // CharacterController movement in Update (not physics-based)
            //if (_characterController != null)
            //{
            //    HandleMovement();
            //}
            
            // Rotation and animation always in Update for smooth visuals
            HandleRotation();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (!CanMove) return;
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;
            
            // Rigidbody movement ONLY in FixedUpdate (physics-based)
            if (_rigidbody != null && _characterController == null)
            {
                HandleMovement();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnPlayerRespawn += HandlePlayerRespawn;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnPlayerRespawn -= HandlePlayerRespawn;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
        }
        
        #endregion

        #region Initialization
        
        private void CacheComponents()
        {
            _input = GetComponent<InputHandler>();
            _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void ValidateSetup()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[PlayerMovement] No camera assigned and no main camera found!");
                }
            }
            
            // Configure Rigidbody for smooth movement
            if (_rigidbody != null)
            {
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
        
        #endregion

        #region Movement
        
        private void HandleMovement()
        {
            // Get input direction
            Vector2 inputVector = _input.InputVector;
            Vector3 inputDirection = new Vector3(inputVector.x, 0, inputVector.y);
            
            if (inputDirection.magnitude < 0.1f)
            {
                _currentVelocity = Vector3.zero;
                
                // Stop Rigidbody immediately
                if (_rigidbody != null)
                {
                    _rigidbody.velocity = new Vector3(0, _rigidbody.velocity.y, 0);
                }
                
                return;
            }
            
            // Transform input relative to camera
            if (mainCamera != null)
            {
                float cameraYRotation = mainCamera.transform.rotation.eulerAngles.y;
                inputDirection = Quaternion.Euler(0, cameraYRotation, 0) * inputDirection;
            }
            
            // Calculate movement
            _targetDirection = inputDirection.normalized;
            _currentVelocity = _targetDirection * movementSpeed;
            
            // Apply movement with correct deltaTime
            float deltaTime = _rigidbody != null ? Time.fixedDeltaTime : Time.deltaTime;
            ApplyMovement(_currentVelocity * deltaTime);
        }

        private void ApplyMovement(Vector3 movement)
        {
            if (_characterController != null)
            {
                _characterController.Move(movement);
            }
            else if (_rigidbody != null)
            {
                // Use MovePosition for smooth Rigidbody movement
                Vector3 newPosition = _rigidbody.position + movement;
                _rigidbody.MovePosition(newPosition);
            }
            else
            {
                transform.position += movement;
            }
        }
        
        #endregion

        #region Rotation
        
        private void HandleRotation()
        {
            // Không xoay nếu đang bị lock (ví dụ khi đang bắn)
            if (!CanRotate) return;
            
            if (rotateTowardMouse)
            {
                RotateTowardMouse();
            }
            else
            {
                RotateTowardMovement();
            }
        }

        private void RotateTowardMouse()
        {
            if (mainCamera == null) return;
            
            Ray ray = mainCamera.ScreenPointToRay(_input.MousePosition);
            Vector3 targetPoint = Vector3.zero;
            bool foundTarget = false;
            
            // Phương pháp 1: Raycast xuống ground layer (nếu có set)
            if (groundLayer.value != 0)
            {
                if (Physics.Raycast(ray, out RaycastHit hitInfo, groundCheckDistance, groundLayer))
                {
                    targetPoint = hitInfo.point;
                    foundTarget = true;
                }
            }
            
            // Phương pháp 2: Fallback - dùng Plane tại độ cao của player
            // Đây là phương pháp chính xác nhất cho mọi góc camera
            if (!foundTarget)
            {
                Plane groundPlane = new Plane(Vector3.up, transform.position);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    targetPoint = ray.GetPoint(distance);
                    foundTarget = true;
                }
            }
            
            if (foundTarget)
            {
                LookAtPoint(targetPoint);
                
                #if UNITY_EDITOR
                Debug.DrawLine(transform.position, targetPoint, Color.green, 0.05f);
                #endif
            }
        }

        private void LookAtPoint(Vector3 targetPoint)
        {
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // Keep rotation on horizontal plane
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        private void RotateTowardMovement()
        {
            if (_targetDirection.magnitude < 0.1f) return;
            
            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
        
        #endregion

        #region Animation
        
        private void UpdateAnimator()
        {
            if (_animator == null) return;
            
            // Update speed (normalized 0-1)
            float speed = _currentVelocity.magnitude / movementSpeed;
            _animator.SetFloat(SpeedHash, speed);
            
            // Update moving state
            _animator.SetBool(IsMovingHash, IsMoving);
        }
        
        #endregion

        #region Event Handlers
        
        private void HandlePlayerDeath()
        {
            CanMove = false;
            _currentVelocity = Vector3.zero;
            UpdateAnimator();
        }

        private void HandlePlayerRespawn()
        {
            CanMove = true;
        }

        private void HandleGamePaused()
        {
            // Animator will be handled by Time.timeScale = 0
        }

        private void HandleGameResumed()
        {
            // Normal operation resumes
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Teleports the player to a new position.
        /// </summary>
        public void SetPosition(Vector3 newPosition)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
                transform.position = newPosition;
                _characterController.enabled = true;
            }
            else
            {
                transform.position = newPosition;
            }
        }

        /// <summary>
        /// Forces the player to look at a specific point.
        /// </summary>
        public void LookAt(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0;
            
            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        #endregion
    }
}
