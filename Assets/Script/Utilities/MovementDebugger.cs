using UnityEngine;
using ShootZombie.Player;

namespace ShootZombie.Utilities
{
    /// <summary>
    /// Debug script to visualize player movement and detect jitter issues.
    /// Attach to Player to see movement data in real-time.
    /// </summary>
    public class MovementDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool logMovement = false;
        [SerializeField] private bool drawGizmos = true;
        
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        
        private Vector3 _lastPosition;
        private Vector3 _currentVelocity;
        private float _updateDeltaTime;
        private float _fixedDeltaTime;
        private int _updateCount;
        private int _fixedUpdateCount;
        
        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void Update()
        {
            _updateCount++;
            _updateDeltaTime = Time.deltaTime;
            
            if (logMovement && Vector3.Distance(transform.position, _lastPosition) > 0.001f)
            {
                Debug.Log($"[Update] Frame: {Time.frameCount}, Pos: {transform.position}, Delta: {transform.position - _lastPosition}");
            }
        }

        private void FixedUpdate()
        {
            _fixedUpdateCount++;
            _fixedDeltaTime = Time.fixedDeltaTime;
            
            // Calculate actual velocity
            _currentVelocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
            _lastPosition = transform.position;
            
            if (logMovement && _currentVelocity.magnitude > 0.1f)
            {
                Debug.Log($"[FixedUpdate] Frame: {Time.frameCount}, Velocity: {_currentVelocity}, Magnitude: {_currentVelocity.magnitude}");
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Box("Movement Debugger", GUILayout.Width(390));
            
            GUILayout.Label($"Position: {transform.position}");
            GUILayout.Label($"Velocity: {_currentVelocity}");
            GUILayout.Label($"Speed: {_currentVelocity.magnitude:F2} m/s");
            
            GUILayout.Space(10);
            GUILayout.Label($"Update Count: {_updateCount}");
            GUILayout.Label($"FixedUpdate Count: {_fixedUpdateCount}");
            GUILayout.Label($"Update DeltaTime: {_updateDeltaTime * 1000:F2} ms");
            GUILayout.Label($"FixedUpdate DeltaTime: {_fixedDeltaTime * 1000:F2} ms");
            
            GUILayout.Space(10);
            if (playerMovement != null)
            {
                GUILayout.Label($"Player Velocity: {playerMovement.CurrentVelocity}");
                GUILayout.Label($"Is Moving: {playerMovement.IsMoving}");
            }
            
            // Check for jitter indicators
            GUILayout.Space(10);
            float velocityVariance = Mathf.Abs(_currentVelocity.magnitude - (playerMovement?.CurrentVelocity.magnitude ?? 0));
            if (velocityVariance > 0.5f)
            {
                GUI.color = Color.red;
                GUILayout.Label($"⚠ JITTER DETECTED! Variance: {velocityVariance:F2}");
                GUI.color = Color.white;
            }
            
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, _currentVelocity.normalized * 2f);
            
            // Draw position trail
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
