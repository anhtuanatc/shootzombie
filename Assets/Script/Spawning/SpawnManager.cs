using System.Collections.Generic;
using UnityEngine;
using ShootZombie.Core;
using ShootZombie.Enemy;

namespace ShootZombie.Spawning
{
    /// <summary>
    /// Manages enemy spawning with wave progression.
    /// Spawns enemies at defined spawn points with configurable timing.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject[] enemyPrefabs;
        
        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool useThisTransformAsSpawn = true;
        
        [Header("Spawn Timing")]
        [SerializeField] private float initialDelay = 2f;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float minSpawnInterval = 0.5f;
        [SerializeField] private float intervalDecreasePerWave = 0.2f;
        
        [Header("Wave Settings")]
        [SerializeField] private bool useWaves = false;
        [SerializeField] private int enemiesPerWave = 10;
        [SerializeField] private int additionalEnemiesPerWave = 3;
        [SerializeField] private float waveDelay = 5f;
        
        [Header("Spawn Limits")]
        [SerializeField] private int maxActiveEnemies = 20;
        [SerializeField] private int maxTotalSpawns = -1; // -1 = unlimited
        
        [Header("Player Reference")]
        [SerializeField] private Transform player;
        [SerializeField] private bool autoFindPlayer = true;
        
        [Header("Object Pooling")]
        [SerializeField] private bool useObjectPooling = false;
        [SerializeField] private string enemyPoolTag = "Zombie";
        
        #endregion

        #region Properties
        
        /// <summary>Current wave number</summary>
        public int CurrentWave { get; private set; } = 0;
        
        /// <summary>Total enemies spawned</summary>
        public int TotalSpawned { get; private set; } = 0;
        
        /// <summary>Currently active enemy count</summary>
        public int ActiveEnemyCount => _activeEnemies.Count;
        
        /// <summary>Is spawning currently active?</summary>
        public bool IsSpawning { get; private set; } = false;
        
        #endregion

        #region Private Fields
        
        private List<GameObject> _activeEnemies = new List<GameObject>();
        private float _currentSpawnInterval;
        private int _enemiesSpawnedThisWave;
        private int _enemiesRequiredThisWave;
        private bool _isWaitingForWave;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopSpawning();
        }
        
        #endregion

        #region Initialization
        
        private void Initialize()
        {
            // Setup spawn points
            if (useThisTransformAsSpawn && (spawnPoints == null || spawnPoints.Length == 0))
            {
                spawnPoints = new Transform[] { transform };
            }
            
            // Find player
            if (autoFindPlayer && player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            
            // Initialize spawn interval
            _currentSpawnInterval = spawnInterval;
            
            // Start spawning if game is already playing
            if (GameManager.HasInstance && GameManager.Instance.IsPlaying)
            {
                StartSpawning();
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameEnd += HandleGameEnd;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameEnd -= HandleGameEnd;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }
        
        #endregion

        #region Spawning Control
        
        /// <summary>
        /// Starts the spawning process.
        /// </summary>
        public void StartSpawning()
        {
            if (IsSpawning) return;
            
            IsSpawning = true;
            
            if (useWaves)
            {
                StartNextWave();
            }
            else
            {
                InvokeRepeating(nameof(SpawnEnemy), initialDelay, _currentSpawnInterval);
            }
            
            Debug.Log("[SpawnManager] Spawning started!");
        }

        /// <summary>
        /// Stops the spawning process.
        /// </summary>
        public void StopSpawning()
        {
            IsSpawning = false;
            CancelInvoke(nameof(SpawnEnemy));
            CancelInvoke(nameof(StartNextWave));
            
            Debug.Log("[SpawnManager] Spawning stopped!");
        }

        /// <summary>
        /// Clears all active enemies.
        /// </summary>
        public void ClearAllEnemies()
        {
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            _activeEnemies.Clear();
            
            GameEvents.TriggerEnemyCountChanged(0);
        }
        
        #endregion

        #region Wave Management
        
        private void StartNextWave()
        {
            CurrentWave++;
            _enemiesSpawnedThisWave = 0;
            _enemiesRequiredThisWave = enemiesPerWave + (additionalEnemiesPerWave * (CurrentWave - 1));
            _isWaitingForWave = false;
            
            // Decrease spawn interval
            _currentSpawnInterval = Mathf.Max(minSpawnInterval, spawnInterval - (intervalDecreasePerWave * (CurrentWave - 1)));
            
            // Broadcast wave start
            GameEvents.TriggerWaveStart(CurrentWave);
            
            // Start spawning for this wave
            InvokeRepeating(nameof(SpawnEnemy), initialDelay, _currentSpawnInterval);
            
            Debug.Log($"[SpawnManager] Wave {CurrentWave} started! Enemies: {_enemiesRequiredThisWave}");
        }

        private void CheckWaveComplete()
        {
            if (!useWaves) return;
            if (_isWaitingForWave) return;
            
            // Check if all enemies for this wave have been spawned and killed
            if (_enemiesSpawnedThisWave >= _enemiesRequiredThisWave && _activeEnemies.Count == 0)
            {
                OnWaveComplete();
            }
        }

        private void OnWaveComplete()
        {
            _isWaitingForWave = true;
            CancelInvoke(nameof(SpawnEnemy));
            
            GameEvents.TriggerWaveComplete(CurrentWave);
            
            Debug.Log($"[SpawnManager] Wave {CurrentWave} complete!");
            
            // Start next wave after delay
            Invoke(nameof(StartNextWave), waveDelay);
        }
        
        #endregion

        #region Spawn Logic
        
        private void SpawnEnemy()
        {
            // Check if player exists
            if (player == null)
            {
                Debug.LogWarning("[SpawnManager] No player found, skipping spawn.");
                return;
            }
            
            // Check spawn limits
            if (_activeEnemies.Count >= maxActiveEnemies)
            {
                return;
            }
            
            if (maxTotalSpawns > 0 && TotalSpawned >= maxTotalSpawns)
            {
                StopSpawning();
                return;
            }
            
            // Wave check
            if (useWaves && _enemiesSpawnedThisWave >= _enemiesRequiredThisWave)
            {
                CancelInvoke(nameof(SpawnEnemy));
                return;
            }
            
            // Validate prefabs
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[SpawnManager] No enemy prefabs assigned!");
                return;
            }
            
            // Select random prefab and spawn point
            int prefabIndex = Random.Range(0, enemyPrefabs.Length);
            Transform spawnPoint = GetRandomSpawnPoint();
            
            // Spawn enemy
            GameObject enemy = SpawnEnemyPrefab(enemyPrefabs[prefabIndex], spawnPoint);
            
            if (enemy != null)
            {
                RegisterEnemy(enemy);
            }
        }

        private GameObject SpawnEnemyPrefab(GameObject prefab, Transform spawnPoint)
        {
            Vector3 position = spawnPoint.position;
            Quaternion rotation = spawnPoint.rotation;
            
            // Add some random offset
            position += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            
            GameObject enemy;
            
            if (useObjectPooling && ObjectPool.HasInstance)
            {
                enemy = ObjectPool.Instance.Spawn(enemyPoolTag, position, rotation);
            }
            else
            {
                enemy = Instantiate(prefab, position, rotation);
            }
            
            // Set player reference
            if (enemy != null)
            {
                var enemyBase = enemy.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    enemyBase.SetPlayer(player);
                }
                else
                {
                    // Legacy support for old EnemyAI
                    var legacyAI = enemy.GetComponent<ShootZombie.Enemy.ZombieAI>();
                    if (legacyAI != null)
                    {
                        legacyAI.SetPlayer(player);
                    }
                }
            }
            
            return enemy;
        }

        private void RegisterEnemy(GameObject enemy)
        {
            _activeEnemies.Add(enemy);
            TotalSpawned++;
            _enemiesSpawnedThisWave++;
            
            GameEvents.TriggerEnemySpawned(enemy);
            GameEvents.TriggerEnemyCountChanged(_activeEnemies.Count);
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return transform;
            }
            
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        
        #endregion

        #region Event Handlers
        
        private void HandleGameStart()
        {
            ResetSpawnManager();
            StartSpawning();
        }

        private void HandleGameEnd(bool isVictory)
        {
            StopSpawning();
        }

        private void HandleEnemyKilled(GameObject enemy, int points)
        {
            _activeEnemies.Remove(enemy);
            GameEvents.TriggerEnemyCountChanged(_activeEnemies.Count);
            
            CheckWaveComplete();
        }
        
        #endregion

        #region Reset
        
        private void ResetSpawnManager()
        {
            CurrentWave = 0;
            TotalSpawned = 0;
            _enemiesSpawnedThisWave = 0;
            _currentSpawnInterval = spawnInterval;
            _isWaitingForWave = false;
            
            ClearAllEnemies();
        }
        
        #endregion

        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn points
            Gizmos.color = Color.green;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(point.position, point.position + point.forward * 1f);
                }
            }
        }
        
        #endregion
    }
}
