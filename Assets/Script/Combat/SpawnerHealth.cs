using UnityEngine;
using ShootZombie.Core;
using ShootZombie.Enemy;
using ShootZombie.Spawning;

namespace ShootZombie.Combat
{
    /// <summary>
    /// Health component for spawners/nests that enemies come from.
    /// Destroying spawners can be a game objective.
    /// </summary>
    public class SpawnerHealth : MonoBehaviour, IDamageable
    {
        #region Inspector Fields
        
        [Header("Health")]
        [SerializeField] private int maxHealth = 500;
        [SerializeField] private bool isDestructible = true;
        
        [Header("Effects")]
        [SerializeField] private GameObject destroyEffectPrefab;
        [SerializeField] private GameObject damageEffectPrefab;
        [SerializeField] private float destroyEffectDuration = 2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip destroySound;
        
        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private float damageFlashDuration = 0.1f;
        
        #endregion

        #region Properties
        
        /// <summary>Current health</summary>
        public int CurrentHealth { get; private set; }
        
        /// <summary>Maximum health</summary>
        public int MaxHealth => maxHealth;
        
        /// <summary>Health as normalized value (0-1)</summary>
        public float HealthNormalized => (float)CurrentHealth / maxHealth;
        
        /// <summary>Is the spawner still alive?</summary>
        public bool IsAlive => CurrentHealth > 0;
        
        #endregion

        #region Private Fields
        
        private AudioSource _audioSource;
        private Color _originalColor;
        private bool _isDestroyed = false;
        
        private static int _activeSpawnerCount = 0;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
            
            if (targetRenderer != null)
            {
                _originalColor = targetRenderer.material.color;
            }
        }

        private void Start()
        {
            CurrentHealth = maxHealth;
        }

        private void OnEnable()
        {
            _activeSpawnerCount++;
        }

        private void OnDisable()
        {
            _activeSpawnerCount--;
        }
        
        #endregion

        #region IDamageable Implementation
        
        /// <summary>
        /// Applies damage to the spawner.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!isDestructible || _isDestroyed) return;
            
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Max(0, CurrentHealth);
            
            // Effects
            PlayDamageEffects();
            
            if (CurrentHealth <= 0)
            {
                OnDestroyed();
            }
        }
        
        #endregion

        #region Destruction
        
        private void OnDestroyed()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            
            // Broadcast event
            GameEvents.TriggerSpawnerDestroyed(gameObject);
            
            // Check if this was the last spawner
            if (_activeSpawnerCount <= 1)
            {
                GameEvents.TriggerAllSpawnersDestroyed();
            }
            
            // Effects
            PlayDestroyEffects();
            
            // Cleanup
            DisableSpawning();
            Destroy(gameObject, 0.1f);
        }

        private void DisableSpawning()
        {
            // Disable any spawn manager on this object
            var spawnManager = GetComponent<SpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.enabled = false;
            }
        }
        
        #endregion

        #region Effects
        
        private void PlayDamageEffects()
        {
            // Spawn damage effect
            if (damageEffectPrefab != null)
            {
                var effect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // Play sound
            if (damageSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(damageSound);
            }
            
            // Flash color
            if (targetRenderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private void PlayDestroyEffects()
        {
            // Spawn destroy effect
            if (destroyEffectPrefab != null)
            {
                var effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, destroyEffectDuration);
            }
            
            // Play sound
            if (destroySound != null)
            {
                AudioSource.PlayClipAtPoint(destroySound, transform.position);
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            targetRenderer.material.color = damageColor;
            yield return new WaitForSeconds(damageFlashDuration);
            
            if (targetRenderer != null)
            {
                targetRenderer.material.color = _originalColor;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Heals the spawner.
        /// </summary>
        public void Heal(int amount)
        {
            if (_isDestroyed) return;
            
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        }

        /// <summary>
        /// Gets the number of active spawners.
        /// </summary>
        public static int GetActiveSpawnerCount()
        {
            return _activeSpawnerCount;
        }
        
        #endregion
    }
}
