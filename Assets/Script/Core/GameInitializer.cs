using UnityEngine;

namespace ShootZombie.Core
{
    /// <summary>
    /// Initializes the game scene and ensures all required managers exist.
    /// Place this in every game scene to ensure proper setup.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Auto-Start")]
        [SerializeField] private bool autoStartGame = true;
        [SerializeField] private float startDelay = 0.5f;
        
        [Header("Required Managers")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject objectPoolPrefab;
        
        [Header("Scene References")]
        [SerializeField] private Transform playerSpawnPoint;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureManagersExist();
        }

        private void Start()
        {
            if (autoStartGame)
            {
                Invoke(nameof(StartGameSession), startDelay);
            }
        }

        private void OnDestroy()
        {
            // Clean up events when scene unloads
            GameEvents.ClearAllEvents();
        }
        
        #endregion

        #region Initialization
        
        private void EnsureManagersExist()
        {
            // Ensure GameManager exists
            if (!GameManager.HasInstance)
            {
                if (gameManagerPrefab != null)
                {
                    Instantiate(gameManagerPrefab);
                }
                else
                {
                    // Create one dynamically
                    var go = new GameObject("[GameManager]");
                    go.AddComponent<GameManager>();
                }
                Debug.Log("[GameInitializer] GameManager created.");
            }
            
            // Ensure ObjectPool exists (optional)
            if (!ObjectPool.HasInstance && objectPoolPrefab != null)
            {
                Instantiate(objectPoolPrefab);
                Debug.Log("[GameInitializer] ObjectPool created.");
            }
        }

        private void StartGameSession()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.StartGame();
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Manually starts the game.
        /// </summary>
        public void ManualStart()
        {
            StartGameSession();
        }

        /// <summary>
        /// Gets the player spawn point position.
        /// </summary>
        public Vector3 GetPlayerSpawnPosition()
        {
            return playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        }
        
        #endregion
    }
}
