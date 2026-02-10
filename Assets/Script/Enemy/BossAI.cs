using UnityEngine;
using UnityEngine.AI;
using ShootZombie.Core;
using ShootZombie.Player;

namespace ShootZombie.Enemy
{
    /// <summary>
    /// Boss AI that behaves like a player (shoots bullets) and can summon minions.
    /// </summary>
    public class BossAI : EnemyBase
    {
        #region Inspector Fields
        
        [Header("Boss Settings")]
        [SerializeField] private float detectionRange = 30f;
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Movement")]
        [SerializeField] private NavMeshAgent navAgent;
        [SerializeField] private float stoppingDistance = 5f; // Keep distance to shoot
        
        [Header("Shooting")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletForce = 20f;
        [SerializeField] private float minFireRate = 3f;
        [SerializeField] private float maxFireRate = 4f;
        [SerializeField] private float shootStopDuration = 1f;
        [SerializeField] private float shootRange = 15f;
        
        [Header("Summoning")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int minionCount = 2;
        [SerializeField] private float summonCooldown = 10f;
        [SerializeField] private float summonRange = 2f;
        [SerializeField] private Transform[] summonPoints;
        
        [Header("Effects")]
        [SerializeField] private GameObject summonEffectPrefab;
        [SerializeField] private AudioClip summonSound;
        
        #endregion

        #region Private Fields
        
        private float _nextFireTime;
        private float _nextSummonTime;
        private bool _isStunned;
        private bool _isShooting;
        
        // Animation Hashes
        private static readonly int ShootTriggerHash = Animator.StringToHash("Shoot");
        private static readonly int SummonTriggerHash = Animator.StringToHash("Summon"); // Ensure you have this parameter or use Reload
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();
            
            if (navAgent != null)
            {
                navAgent.speed = speed;
                navAgent.stoppingDistance = stoppingDistance;
            }
            
            _nextSummonTime = Time.time + summonCooldown / 2; // Initial delay
        }

        private void Update()
        {
            if (_isDying) return;
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;
            
            if (Player == null)
            {
                FindPlayer();
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                UpdateBehavior(distanceToPlayer);
            }
            else
            {
                StopMoving();
            }
            
            UpdateAnimation();
        }
        
        #endregion

        #region AI Behavior
        
        private void UpdateBehavior(float distance)
        {
            // Do not move or rotate if shooting (stun handling is done in Update)
            if (_isShooting) 
            {
                StopMoving();
                // Optionally rotate towards player even while shooting? 
                // Usually "standing still to shoot" implies locking rotation or slow rotation.
                // Let's allow rotation for accuracy, but no movement.
                RotateTowardPlayer(); 
                return;
            }

            // Always face player when in range
            RotateTowardPlayer();
            
            // Priority 1: Summon
            if (Time.time >= _nextSummonTime && minionPrefab != null)
            {
                SummonMinions();
                return;
            }
            
            // Priority 2: Shoot
            if (distance <= shootRange && Time.time >= _nextFireTime)
            {
                Shoot();
            }
            
            // Movement Logic
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(Player.position);
            }
        }
        
        private void RotateTowardPlayer()
        {
            Vector3 direction = Player.position - transform.position;
            direction.y = 0;
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        
        private void StopMoving()
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.ResetPath();
            }
        }
        
        #endregion

        #region Combat
        
        private void Shoot()
        {
            float fireRate = Random.Range(minFireRate, maxFireRate);
            _nextFireTime = Time.time + fireRate;
            
            // Start shooting sequence
            StartCoroutine(ShootRoutine());
        }

        private System.Collections.IEnumerator ShootRoutine()
        {
            _isShooting = true;
            
            // Animation
            if (_animator != null)
            {
                _animator.SetTrigger(ShootTriggerHash);
            }
            
            // Delay actual shot for animation sync (optional, can be 0)
            yield return new WaitForSeconds(0.2f);
            
            // Shoot logic
            if (bulletPrefab != null && firePoint != null && Player != null)
            {
                // Predict player position slightly? For now, shoot directly at player
                Vector3 targetPos = Player.position;
                // Aim a bit higher (at body center)
                targetPos.y += 1f;
                
                Vector3 direction = (targetPos - firePoint.position).normalized;
                
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
                // Add tag or layer check to bullet so it damages player? 
                // Note: Player bullets likely damage Enemies. Enemy bullets need to damage Player.
                // You might need a separate EnemyBullet prefab or modify the bullet script to handle layers.
                
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.AddForce(direction * bulletForce, ForceMode.Impulse);
                }
            }
            
            // Wait for the rest of the stop duration
            yield return new WaitForSeconds(shootStopDuration - 0.2f);
            
            _isShooting = false;
        }
        
        private void SummonMinions()
        {
            _nextSummonTime = Time.time + summonCooldown;
            
            // Animation
            if (_animator != null)
            {
                // If "Summon" trigger exists, use it. Otherwise maybe "Reload"
                _animator.SetTrigger(SummonTriggerHash); 
            }
            
            // Sound
            if (summonSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(summonSound);
            }
            
            // Spawn logic
            for (int i = 0; i < minionCount; i++)
            {
                Vector3 spawnPos = GetSummonPosition();
                
                // Effect
                if (summonEffectPrefab != null)
                {
                    Instantiate(summonEffectPrefab, spawnPos, Quaternion.identity);
                }
                
                GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                
                // Initialize minion
                EnemyBase enemyScript = minion.GetComponent<EnemyBase>();
                if (enemyScript != null)
                {
                    enemyScript.SetPlayer(Player);
                }
            }
        }
        
        private Vector3 GetSummonPosition()
        {
            if (summonPoints != null && summonPoints.Length > 0)
            {
                return summonPoints[Random.Range(0, summonPoints.Length)].position;
            }
            
            // Random point around boss
            Vector2 randomCircle = Random.insideUnitCircle * summonRange;
            Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
            return transform.position + offset;
        }
        
        #endregion

        #region Animation
        
        private void UpdateAnimation()
        {
            if (_animator == null) return;
            
            float currentSpeed = 0f;
            if (navAgent != null && navAgent.enabled)
            {
                currentSpeed = navAgent.velocity.magnitude;
            }
            
            _animator.SetFloat(SpeedHash, currentSpeed);
        }
        
        #endregion
        
        #region Death
        
        protected override void Die()
        {
            if (navAgent != null) navAgent.enabled = false;
            base.Die();
        }
        
        #endregion
    }
}
