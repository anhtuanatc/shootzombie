using UnityEngine;
using UnityEngine.AI;
using ShootZombie.Core;
using ShootZombie.Player;

namespace ShootZombie.Enemy
{
    /// <summary>
    /// Zombie enemy AI. Chases the player and attacks on contact.
    /// Supports both simple movement and NavMesh pathfinding.
    /// </summary>
    public class ZombieAI : EnemyBase
    {
        #region Inspector Fields
        
        [Header("AI Settings")]
        [SerializeField] private float detectionRange = 50f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Movement")]
        [SerializeField] private bool useNavMesh = false;
        [SerializeField] private float stoppingDistance = 1f;
        
        [Header("Behavior")]
        [SerializeField] private bool canAttack = true;
        [SerializeField] private float stunDuration = 0.2f;
        
        #endregion

        #region Private Fields
        
        private NavMeshAgent _navAgent;
        private float _lastAttackTime;
        private bool _isStunned;
        private float _stunTimer;
        
        private enum ZombieState { Idle, Chasing, Attacking, Stunned, Dead }
        private ZombieState _currentState = ZombieState.Idle;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            _navAgent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();
            
            // Configure NavMesh if available
            if (_navAgent != null && useNavMesh)
            {
                _navAgent.speed = speed;
                _navAgent.stoppingDistance = stoppingDistance;
            }
        }

        private void Update()
        {
            if (_isDying) return;
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;
            
            UpdateStun();
            
            if (!_isStunned)
            {
                UpdateBehavior();
            }
            
            UpdateAnimation();
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision);
        }
        
        #endregion

        #region Initialization
        
        public override void Initialize()
        {
            base.Initialize();
            
            _currentState = ZombieState.Idle;
            _lastAttackTime = -attackCooldown;
            _isStunned = false;
            _stunTimer = 0f;
            
            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _navAgent.speed = speed;
            }
        }
        
        #endregion

        #region AI Behavior
        
        private void UpdateBehavior()
        {
            if (Player == null)
            {
                FindPlayer();
                if (Player == null)
                {
                    _currentState = ZombieState.Idle;
                    return;
                }
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            
            // State machine
            if (distanceToPlayer <= attackRange && canAttack)
            {
                _currentState = ZombieState.Attacking;
                TryAttack();
            }
            else if (distanceToPlayer <= detectionRange)
            {
                _currentState = ZombieState.Chasing;
                ChasePlayer();
            }
            else
            {
                _currentState = ZombieState.Idle;
                StopMoving();
            }
        }

        private void ChasePlayer()
        {
            if (Player == null) return;
            
            if (useNavMesh && _navAgent != null && _navAgent.enabled)
            {
                // Use NavMesh pathfinding
                _navAgent.SetDestination(Player.position);
            }
            else
            {
                // Simple direct movement
                MoveTowardPlayer();
            }
            
            // Always face the player
            RotateTowardPlayer();
        }

        private void MoveTowardPlayer()
        {
            Vector3 direction = (Player.position - transform.position).normalized;
            direction.y = 0; // Keep on horizontal plane
            
            transform.position += direction * speed * Time.deltaTime;
        }

        private void RotateTowardPlayer()
        {
            if (Player == null) return;
            
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
            if (_navAgent != null && _navAgent.enabled)
            {
                _navAgent.ResetPath();
            }
        }
        
        #endregion

        #region Attack
        
        private void TryAttack()
        {
            if (Time.time < _lastAttackTime + attackCooldown) return;
            
            _lastAttackTime = Time.time;
            
            // Animation
            if (_animator != null)
            {
                _animator.SetTrigger(AttackTriggerHash);
            }
            
            // Sound
            if (attackSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(attackSound);
            }
        }

        private void HandleCollision(Collision collision)
        {
            if (_isDying) return;
            if (Time.time < _lastAttackTime + attackCooldown) return;
            
            // Check for player
            var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && canAttack)
            {
                DealDamageToPlayer(playerHealth);
            }
        }

        private void DealDamageToPlayer(PlayerHealth playerHealth)
        {
            playerHealth.TakeDamage(damageToPlayer);
            _lastAttackTime = Time.time;
            
            // Attack animation
            if (_animator != null)
            {
                _animator.SetTrigger(AttackTriggerHash);
            }
        }
        
        #endregion

        #region Stun
        
        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);
            
            // Apply stun on hit
            if (IsAlive && stunDuration > 0)
            {
                ApplyStun(stunDuration);
            }
        }

        public void ApplyStun(float duration)
        {
            _isStunned = true;
            _stunTimer = duration;
            _currentState = ZombieState.Stunned;
            
            // Stop NavMesh movement
            if (_navAgent != null && _navAgent.enabled)
            {
                _navAgent.ResetPath();
            }
        }

        private void UpdateStun()
        {
            if (!_isStunned) return;
            
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0)
            {
                _isStunned = false;
            }
        }
        
        #endregion

        #region Animation
        
        private void UpdateAnimation()
        {
            if (_animator == null) return;
            
            float currentSpeed = 0f;
            
            if (_navAgent != null && _navAgent.enabled)
            {
                currentSpeed = _navAgent.velocity.magnitude;
            }
            else if (_currentState == ZombieState.Chasing)
            {
                currentSpeed = speed;
            }
            
            _animator.SetFloat(SpeedHash, currentSpeed);
        }
        
        #endregion

        #region Event Handlers
        
        protected override void HandlePlayerDeath()
        {
            base.HandlePlayerDeath();
            _currentState = ZombieState.Idle;
            StopMoving();
        }

        protected override void HandleGameEnd(bool isVictory)
        {
            base.HandleGameEnd(isVictory);
            StopMoving();
            enabled = false;
        }
        
        #endregion

        #region Death Override
        
        protected override void Die()
        {
            _currentState = ZombieState.Dead;
            
            // Disable NavMesh
            if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }
            
            base.Die();
        }
        
        #endregion

        #region Pool Override
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            
            _currentState = ZombieState.Idle;
            
            if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            
            if (_navAgent != null && useNavMesh)
            {
                _navAgent.enabled = true;
            }
        }
        
        #endregion

        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
        
        #endregion
    }
}
