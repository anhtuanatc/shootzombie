using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Player
{
    /// <summary>
    /// Centralized input handling for the player.
    /// Supports both legacy Input Manager and can be extended for new Input System.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        #region Properties
        
        /// <summary>Movement input as a 2D vector (horizontal, vertical)</summary>
        public Vector2 InputVector { get; private set; }
        
        /// <summary>Raw movement input (not smoothed)</summary>
        public Vector2 RawInputVector { get; private set; }
        
        /// <summary>Current mouse position in screen coordinates</summary>
        public Vector3 MousePosition { get; private set; }
        
        /// <summary>Is any movement input being received?</summary>
        public bool HasMovementInput => InputVector.magnitude > 0.1f;
        
        /// <summary>Is the fire button pressed?</summary>
        public bool IsFirePressed { get; private set; }
        
        /// <summary>Is the fire button held down?</summary>
        public bool IsFireHeld { get; private set; }
        
        /// <summary>Is the reload button pressed?</summary>
        public bool IsReloadPressed { get; private set; }
        
        /// <summary>Is the pause button pressed?</summary>
        public bool IsPausePressed { get; private set; }
        
        /// <summary>Is the interaction button pressed?</summary>
        public bool IsInteractPressed { get; private set; }
        
        /// <summary>Mouse scroll delta</summary>
        public float ScrollDelta { get; private set; }
        
        /// <summary>Is input currently enabled?</summary>
        public bool InputEnabled { get; set; } = true;
        
        #endregion

        #region Inspector Fields
        
        [Header("Input Settings")]
        [SerializeField] private bool useRawInput = false;
        
        [Header("Input Axes Names")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string fireButton = "Fire1";
        
        [Header("Key Bindings")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        
        #endregion

        #region Unity Lifecycle
        
        private void Update()
        {
            if (!InputEnabled)
            {
                ClearInput();
                return;
            }

            // Don't process input when game is paused (except pause key)
            if (GameManager.HasInstance && GameManager.Instance.IsPaused)
            {
                IsPausePressed = Input.GetKeyDown(pauseKey);
                ClearMovementInput();
                return;
            }

            UpdateMovementInput();
            UpdateMouseInput();
            UpdateButtonInput();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnPlayerRespawn += HandlePlayerRespawn;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnPlayerRespawn -= HandlePlayerRespawn;
        }
        
        #endregion

        #region Input Processing
        
        private void UpdateMovementInput()
        {
            float horizontal, vertical;
            
            if (useRawInput)
            {
                horizontal = Input.GetAxisRaw(horizontalAxis);
                vertical = Input.GetAxisRaw(verticalAxis);
            }
            else
            {
                horizontal = Input.GetAxis(horizontalAxis);
                vertical = Input.GetAxis(verticalAxis);
            }
            
            InputVector = new Vector2(horizontal, vertical);
            
            // Clamp magnitude to prevent faster diagonal movement
            if (InputVector.magnitude > 1f)
            {
                InputVector = InputVector.normalized;
            }
            
            RawInputVector = new Vector2(
                Input.GetAxisRaw(horizontalAxis),
                Input.GetAxisRaw(verticalAxis)
            );
        }

        private void UpdateMouseInput()
        {
            MousePosition = Input.mousePosition;
            ScrollDelta = Input.mouseScrollDelta.y;
        }

        private void UpdateButtonInput()
        {
            IsFirePressed = Input.GetButtonDown(fireButton);
            IsFireHeld = Input.GetButton(fireButton);
            IsReloadPressed = Input.GetKeyDown(reloadKey);
            IsPausePressed = Input.GetKeyDown(pauseKey);
            IsInteractPressed = Input.GetKeyDown(interactKey);
        }

        private void ClearInput()
        {
            ClearMovementInput();
            IsFirePressed = false;
            IsFireHeld = false;
            IsReloadPressed = false;
            IsPausePressed = false;
            IsInteractPressed = false;
            ScrollDelta = 0f;
        }

        private void ClearMovementInput()
        {
            InputVector = Vector2.zero;
            RawInputVector = Vector2.zero;
        }
        
        #endregion

        #region Event Handlers
        
        private void HandlePlayerDeath()
        {
            InputEnabled = false;
        }

        private void HandlePlayerRespawn()
        {
            InputEnabled = true;
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Gets the movement direction in world space relative to a camera.
        /// </summary>
        public Vector3 GetWorldSpaceMovement(UnityEngine.Camera cam)
        {
            if (cam == null || !HasMovementInput) return Vector3.zero;
            
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            return (forward * InputVector.y + right * InputVector.x).normalized;
        }

        /// <summary>
        /// Gets the mouse position in world space on a plane at the given height.
        /// </summary>
        public Vector3 GetMouseWorldPosition(UnityEngine.Camera cam, float yHeight = 0f)
        {
            if (cam == null) return Vector3.zero;
            
            Ray ray = cam.ScreenPointToRay(MousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0, yHeight, 0));
            
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
        
        #endregion
    }
}
