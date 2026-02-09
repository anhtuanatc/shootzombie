using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Enemy
{
    /// <summary>
    /// Interface for all damageable entities.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage);
        bool IsAlive { get; }
    }

    /// <summary>
    /// Base class for all enemies. Handles health, damage, and death.
    /// Extend this class for specific enemy types.
    /// </summary>
    public class EnemyBase : MonoBehaviour, IDamageable, IPoolable
    {
        #region Inspector Fields
        
        [Header("Stats")]
        [SerializeField] protected int maxHealth = 100;
        [SerializeField] protected int pointValue = 100;
        [SerializeField] protected float speed = 3f;
        [SerializeField] protected int damageToPlayer = 20;
        
        [Header("Effects")]
        [SerializeField] protected GameObject deathEffectPrefab;
        [SerializeField] protected GameObject hitEffectPrefab;
        
        [Header("Audio")]
        [SerializeField] protected AudioClip hitSound;
        [SerializeField] protected AudioClip deathSound;
        [SerializeField] protected AudioClip attackSound;
        
        [Header("Object Pooling")]
        [SerializeField] protected bool useObjectPooling = false;
        [SerializeField] protected string poolTag = "Enemy";
        
        #endregion

        #region Properties
        
        /// <summary>Current health</summary>
        public int CurrentHealth { get; protected set; }
        
        /// <summary>Is the enemy alive?</summary>
        public bool IsAlive => CurrentHealth > 0;
        
        /// <summary>Reference to the player transform</summary>
        public Transform Player { get; protected set; }
        
        /// <summary>Point value when killed</summary>
        public int PointValue => pointValue;
        
        #endregion

        #region Private Fields
        
        protected AudioSource _audioSource;
        protected Animator _animator;
        protected bool _isDying = false;
        
        protected static readonly int SpeedHash = Animator.StringToHash("Speed");
        protected static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        protected static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        protected static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        
        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            CacheComponents();
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Initialization
        
        protected virtual void CacheComponents()
        {
            _audioSource = GetComponent<AudioSource>();
            _animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Initializes the enemy. Called on Start and when spawned from pool.
        /// </summary>
        public virtual void Initialize()
        {
            CurrentHealth = maxHealth;
            _isDying = false;
            
            // Find player if not set
            if (Player == null)
            {
                FindPlayer();
            }
        }

        protected virtual void SubscribeToEvents()
        {
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        protected virtual void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnGameEnd -= HandleGameEnd;
        }
        
        #endregion

        #region Player Reference
        
        /// <summary>
        /// Sets the player reference.
        /// </summary>
        public virtual void SetPlayer(Transform playerTransform)
        {
            Player = playerTransform;
        }

        protected virtual void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Player = playerObj.transform;
            }
        }
        
        #endregion

        #region Damage & Death
        
        /// <summary>
        /// Applies damage to this enemy.
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (_isDying || !IsAlive) return;
            
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Max(0, CurrentHealth);
            
            // Effects
            PlayHitEffects();
            
            // Animation
            if (_animator != null)
            {
                _animator.SetTrigger(HitTriggerHash);
            }
            
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Handles enemy death.
        /// </summary>
        protected virtual void Die()
        {
            if (_isDying) return;
            _isDying = true;
            
            // Broadcast death event
            GameEvents.TriggerEnemyKilled(gameObject, pointValue);
            
            // Play death effects
            PlayDeathEffects();
            
            // Animation
            if (_animator != null)
            {
                _animator.SetTrigger(DeathTriggerHash);
            }
            
            // Cleanup
            CleanupOnDeath();
        }

        protected virtual void CleanupOnDeath()
        {
            if (useObjectPooling && ObjectPool.HasInstance)
            {
                // Return to pool after death animation
                Invoke(nameof(ReturnToPool), 0.5f);
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }

        protected virtual void ReturnToPool()
        {
            if (ObjectPool.HasInstance)
            {
                ObjectPool.Instance.Despawn(poolTag, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        #endregion

        #region Effects
        
        protected virtual void PlayHitEffects()
        {
            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                var effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // Play hit sound
            if (hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hitSound);
            }
        }

        protected virtual void PlayDeathEffects()
        {
            // Spawn death effect
            if (deathEffectPrefab != null)
            {
                var effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Play death sound
            if (deathSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(deathSound);
            }
        }
        
        #endregion

        #region Event Handlers
        
        protected virtual void HandlePlayerDeath()
        {
            // Stop chasing - optional behavior
        }

        protected virtual void HandleGameEnd(bool isVictory)
        {
            // Stop all activity
        }
        
        #endregion

        #region IPoolable Implementation
        
        public virtual void OnSpawnFromPool()
        {
            Initialize();
        }

        public virtual void OnReturnToPool()
        {
            CancelInvoke();
            _isDying = false;
            CurrentHealth = maxHealth;
        }
        
        #endregion
    }
}
