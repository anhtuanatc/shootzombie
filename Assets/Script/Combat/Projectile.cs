using UnityEngine;
using ShootZombie.Core;
using ShootZombie.Enemy;

namespace ShootZombie.Combat
{
    /// <summary>
    /// Projectile component for bullets and other projectiles.
    /// Handles collision detection, damage dealing, and effects.
    /// Supports object pooling for performance.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        #region Inspector Fields
        
        [Header("Damage")]
        [SerializeField] private int damage = 20;
        [SerializeField] private bool damageEnemies = true;
        [SerializeField] private bool damageSpawners = true;
        
        [Header("Lifetime")]
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private bool destroyOnHit = true;
        
        [Header("Effects")]
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private float impactEffectDuration = 0.5f;
        [SerializeField] private AudioClip impactSound;
        
        [Header("Object Pooling")]
        [SerializeField] private bool useObjectPooling = false;
        [SerializeField] private string poolTag = "Bullet";
        
        [Header("Physics")]
        [SerializeField] private bool useGravity = false;
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
        
        #endregion

        #region Properties
        
        /// <summary>The damage this projectile deals</summary>
        public int Damage => damage;
        
        /// <summary>The Rigidbody component</summary>
        public Rigidbody Rigidbody => _rigidbody;
        
        #endregion

        #region Private Fields
        
        private Rigidbody _rigidbody;
        private Collider _collider;
        private TrailRenderer _trailRenderer;
        private float _spawnTime;
        private bool _hasHit;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
            ConfigureRigidbody();
        }

        private void OnEnable()
        {
            ResetProjectile();
        }

        private void Update()
        {
            CheckLifetime();
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTrigger(other);
        }
        
        #endregion

        #region Initialization
        
        private void CacheComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _trailRenderer = GetComponent<TrailRenderer>();
        }

        private void ConfigureRigidbody()
        {
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = useGravity;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        private void ResetProjectile()
        {
            _spawnTime = Time.time;
            _hasHit = false;
            
            // Reset rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            // Clear trail
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
            
            // Enable collider
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }
        
        #endregion

        #region Collision Handling
        
        private void HandleCollision(Collision collision)
        {
            if (_hasHit) return;
            
            ProcessHit(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
        }

        private void HandleTrigger(Collider other)
        {
            if (_hasHit) return;
            
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            
            ProcessHit(other.gameObject, hitPoint, hitNormal);
        }

        private void ProcessHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
        {
            bool didDamage = false;
            
            // Try to damage enemy
            if (damageEnemies)
            {
                // Tìm IDamageable trên object hoặc parent (quan trọng nếu collider là child)
                var damageable = hitObject.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    damageable = hitObject.GetComponentInParent<IDamageable>();
                }
                
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    didDamage = true;
                    Debug.Log($"[Projectile] Hit {hitObject.name}, dealt {damage} damage!");
                }
                else
                {
                    Debug.Log($"[Projectile] Hit {hitObject.name} but no IDamageable found.");
                }
            }
            
            // Try to damage spawner (for backwards compatibility)
            if (damageSpawners)
            {
                var spawnerHealth = hitObject.GetComponent<SpawnerHealth>();
                if (spawnerHealth != null)
                {
                    spawnerHealth.TakeDamage(damage);
                    didDamage = true;
                }
            }
            
            // Spawn impact effect
            SpawnImpactEffect(hitPoint, hitNormal);
            
            // Play sound
            PlayImpactSound(hitPoint);
            
            // Handle destruction
            if (destroyOnHit || didDamage)
            {
                OnHit();
            }
        }
        
        #endregion

        #region Effects
        
        private void SpawnImpactEffect(Vector3 position, Vector3 normal)
        {
            if (impactEffectPrefab == null) return;
            
            // Đảm bảo normal không phải zero vector
            Quaternion rotation = Quaternion.identity;
            if (normal.sqrMagnitude > 0.001f)
            {
                rotation = Quaternion.LookRotation(normal);
            }
            
            GameObject effect = Instantiate(impactEffectPrefab, position, rotation);
            Destroy(effect, impactEffectDuration);
        }

        private void PlayImpactSound(Vector3 position)
        {
            if (impactSound == null) return;
            
            AudioSource.PlayClipAtPoint(impactSound, position);
        }
        
        #endregion

        #region Lifecycle
        
        private void CheckLifetime()
        {
            if (Time.time >= _spawnTime + lifetime)
            {
                Despawn();
            }
        }

        private void OnHit()
        {
            _hasHit = true;
            
            // Disable collider to prevent multiple hits
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            
            // Stop movement
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
            }
            
            Despawn();
        }

        private void Despawn()
        {
            if (useObjectPooling && ObjectPool.HasInstance)
            {
                ObjectPool.Instance.Despawn(poolTag, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Launches the projectile in a direction with a given force.
        /// </summary>
        public void Launch(Vector3 direction, float force)
        {
            if (_rigidbody != null)
            {
                _rigidbody.AddForce(direction.normalized * force, forceMode);
            }
        }

        /// <summary>
        /// Sets the projectile's velocity directly.
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            if (_rigidbody != null)
            {
                _rigidbody.velocity = velocity;
            }
        }
        
        #endregion

        #region IPoolable Implementation
        
        public void OnSpawnFromPool()
        {
            ResetProjectile();
        }

        public void OnReturnToPool()
        {
            _hasHit = false;
            
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }
        
        #endregion
    }
}
