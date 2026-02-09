using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Enemy
{
    /// <summary>
    /// Enemy Dơi: Patrol bay lượn, phát hiện Player thì đuổi theo, chạm vào thì nổ.
    /// </summary>
    public class BatEnemy : EnemyBase
    {
        #region Enums
        
        public enum BatState
        {
            Patrol,     // Bay lượn tự do
            Chase,      // Đuổi theo Player
            Exploding   // Đang nổ
        }
        
        #endregion

        #region Inspector Fields
        
        [Header("=== BAT SETTINGS ===")]
        
        [Header("Patrol Settings")]
        [Tooltip("Bán kính patrol quanh điểm spawn")]
        [SerializeField] private float patrolRadius = 8f;
        
        [Tooltip("Tốc độ patrol (chậm hơn chase)")]
        [SerializeField] private float patrolSpeed = 2f;
        
        [Tooltip("Thời gian chờ tại mỗi điểm patrol")]
        [SerializeField] private float patrolWaitTime = 1f;
        
        [Tooltip("Độ cao bay dao động")]
        [SerializeField] private float flyHeightVariation = 2f;
        
        [Tooltip("Tốc độ dao động lên xuống")]
        [SerializeField] private float bobSpeed = 2f;
        
        [Tooltip("Biên độ dao động")]
        [SerializeField] private float bobAmount = 0.5f;
        
        [Header("Detection Settings")]
        [Tooltip("Khoảng cách phát hiện Player")]
        [SerializeField] private float detectionRange = 15f;
        
        [Tooltip("Khoảng cách mất dấu Player")]
        [SerializeField] private float loseTargetRange = 25f;
        
        [Tooltip("Thời gian nhớ vị trí Player sau khi mất dấu")]
        [SerializeField] private float memoryDuration = 3f;
        
        [Header("Chase Settings")]
        [Tooltip("Tốc độ đuổi (nhanh hơn patrol)")]
        [SerializeField] private float chaseSpeed = 5f;
        
        [Tooltip("Tốc độ xoay")]
        [SerializeField] private float rotationSpeed = 8f;
        
        [Header("Flocking Behavior")]
        [Tooltip("Bật tính năng né dơi khác")]
        [SerializeField] private bool enableSeparation = true;
        
        [Tooltip("Khoảng cách bắt đầu né")]
        [SerializeField] private float separationRadius = 2f;
        
        [Tooltip("Lực đẩy né")]
        [SerializeField] private float separationForce = 3f;
        
        [Tooltip("Layer của dơi khác")]
        [SerializeField] private LayerMask batLayer;
        
        [Header("Wobble (Dao động ngẫu nhiên)")]
        [Tooltip("Bật dao động ngang khi bay")]
        [SerializeField] private bool enableWobble = true;
        
        [Tooltip("Biên độ dao động ngang")]
        [SerializeField] private float wobbleAmount = 1.5f;
        
        [Tooltip("Tốc độ dao động")]
        [SerializeField] private float wobbleSpeed = 3f;
        
        [Header("Obstacle Avoidance")]
        [Tooltip("Bật tính năng né chướng ngại vật")]
        [SerializeField] private bool enableObstacleAvoidance = true;
        
        [Tooltip("Khoảng cách phát hiện chướng ngại vật")]
        [SerializeField] private float obstacleDetectionRange = 3f;
        
        [Tooltip("Lực né chướng ngại vật")]
        [SerializeField] private float obstacleAvoidanceForce = 5f;
        
        [Tooltip("Layer của chướng ngại vật (tường, đá...)")]
        [SerializeField] private LayerMask obstacleLayer;
        
        [Tooltip("Số tia raycast để phát hiện")]
        [SerializeField] private int numRays = 5;
        
        [Tooltip("Góc quét (độ)")]
        [SerializeField] private float raySpreadAngle = 60f;
        
        [Header("Explosion Settings")]
        [Tooltip("Khoảng cách để kích nổ")]
        [SerializeField] private float explodeRange = 1.2f;
        
        [Tooltip("Bán kính gây damage")]
        [SerializeField] private float explosionRadius = 2f;
        
        [Tooltip("Effect nổ")]
        [SerializeField] private GameObject explosionEffectPrefab;
        
        [Tooltip("Sound nổ")]
        [SerializeField] private AudioClip explosionSound;
        
        #endregion

        #region Private Fields
        
        private BatState _currentState = BatState.Patrol;
        private Vector3 _spawnPosition;
        private Vector3 _patrolTarget;
        private float _patrolWaitTimer;
        private float _memoryTimer;
        private Vector3 _lastKnownPlayerPos;
        private float _bobTimer;
        private float _baseHeight;
        
        // Wobble & Separation
        private float _wobbleTimer;
        private float _wobbleOffset; // Random phase để mỗi dơi wobble khác nhau
        private Collider[] _nearbyBats = new Collider[10]; // Cache array
        
        // Obstacle avoidance
        private Vector3 _avoidanceDirection;
        private float _stuckTimer;
        private Vector3 _lastPosition;
        
        #endregion

        #region Properties
        
        public BatState CurrentState => _currentState;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
            
            // Lưu vị trí spawn làm trung tâm patrol
            _spawnPosition = transform.position;
            _baseHeight = transform.position.y;
            
            // Random phase cho wobble để mỗi dơi dao động khác nhau
            _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
            _wobbleTimer = Random.Range(0f, 10f);
            
            // Chọn điểm patrol đầu tiên
            PickNewPatrolTarget();
        }

        private void Update()
        {
            if (_isDying || !IsAlive) return;
            
            // Cập nhật bobbing (bay lên xuống)
            UpdateBobbing();
            
            // State machine
            switch (_currentState)
            {
                case BatState.Patrol:
                    UpdatePatrol();
                    CheckForPlayer();
                    break;
                    
                case BatState.Chase:
                    UpdateChase();
                    CheckLosePlayer();
                    break;
                    
                case BatState.Exploding:
                    // Đang nổ, không làm gì
                    break;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Explode();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Explode();
            }
        }
        
        #endregion

        #region Bobbing (Bay lên xuống)
        
        private void UpdateBobbing()
        {
            _bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(_bobTimer) * bobAmount;
            
            // Áp dụng offset cho Y
            Vector3 pos = transform.position;
            pos.y = _baseHeight + bobOffset;
            
            // Chỉ áp dụng khi không dùng Rigidbody
            if (GetComponent<Rigidbody>() == null)
            {
                transform.position = pos;
            }
        }
        
        #endregion

        #region Patrol State
        
        private void UpdatePatrol()
        {
            // Kiểm tra đã đến điểm patrol chưa
            float distanceToTarget = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(_patrolTarget.x, 0, _patrolTarget.z)
            );
            
            if (distanceToTarget < 0.5f)
            {
                // Đã đến, chờ một chút rồi chọn điểm mới
                _patrolWaitTimer -= Time.deltaTime;
                
                if (_patrolWaitTimer <= 0)
                {
                    PickNewPatrolTarget();
                }
                
                UpdateAnimator(0);
                return;
            }
            
            // Di chuyển đến điểm patrol
            MoveTowards(_patrolTarget, patrolSpeed);
            UpdateAnimator(patrolSpeed);
        }
        
        private void PickNewPatrolTarget()
        {
            // Chọn điểm ngẫu nhiên trong bán kính patrol
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Random độ cao
            _baseHeight = _spawnPosition.y + Random.Range(-flyHeightVariation, flyHeightVariation);
            
            _patrolWaitTimer = patrolWaitTime;
        }
        
        #endregion

        #region Detection
        
        private void CheckForPlayer()
        {
            if (Player == null)
            {
                FindPlayer();
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                // Phát hiện Player!
                _currentState = BatState.Chase;
                _lastKnownPlayerPos = Player.position;
                
                // Optional: Trigger animation phát hiện
                // _animator?.SetTrigger("Alert");
            }
        }
        
        private void CheckLosePlayer()
        {
            if (Player == null)
            {
                // Player chết, quay về patrol
                _currentState = BatState.Patrol;
                PickNewPatrolTarget();
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            
            if (distanceToPlayer > loseTargetRange)
            {
                // Mất dấu Player
                _memoryTimer -= Time.deltaTime;
                
                if (_memoryTimer <= 0)
                {
                    // Quên Player, quay về patrol
                    _currentState = BatState.Patrol;
                    PickNewPatrolTarget();
                }
            }
            else
            {
                // Vẫn thấy Player
                _memoryTimer = memoryDuration;
                _lastKnownPlayerPos = Player.position;
            }
        }
        
        #endregion

        #region Chase State
        
        private void UpdateChase()
        {
            if (Player == null)
            {
                _currentState = BatState.Patrol;
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            
            // Kiểm tra nổ
            if (distanceToPlayer <= explodeRange)
            {
                Explode();
                return;
            }
            
            // Tính vị trí target với wobble
            Vector3 targetPos = Player.position;
            targetPos.y = Player.position.y + 1f; // Bay cao hơn player một chút
            
            // Thêm wobble offset để bay tự nhiên hơn
            if (enableWobble)
            {
                targetPos += CalculateWobbleOffset();
            }
            
            // Tính separation để né dơi khác
            Vector3 separationVector = Vector3.zero;
            if (enableSeparation)
            {
                separationVector = CalculateSeparation();
            }
            
            // Di chuyển với separation
            MoveTowardsWithSeparation(targetPos, chaseSpeed, separationVector);
            UpdateAnimator(chaseSpeed);
        }
        
        /// <summary>
        /// Tính wobble offset để bay ngoằn ngoèo tự nhiên.
        /// </summary>
        private Vector3 CalculateWobbleOffset()
        {
            _wobbleTimer += Time.deltaTime * wobbleSpeed;
            
            // Dùng sin/cos với phase khác nhau để tạo đường cong tự nhiên
            float xOffset = Mathf.Sin(_wobbleTimer + _wobbleOffset) * wobbleAmount;
            float zOffset = Mathf.Cos(_wobbleTimer * 0.7f + _wobbleOffset) * wobbleAmount * 0.5f;
            
            return new Vector3(xOffset, 0, zOffset);
        }
        
        /// <summary>
        /// Tính vector separation để né dơi khác.
        /// </summary>
        private Vector3 CalculateSeparation()
        {
            Vector3 separation = Vector3.zero;
            int count = 0;
            
            // Tìm các dơi gần đó
            int numNearby = Physics.OverlapSphereNonAlloc(
                transform.position, 
                separationRadius, 
                _nearbyBats, 
                batLayer
            );
            
            for (int i = 0; i < numNearby; i++)
            {
                // Bỏ qua chính mình
                if (_nearbyBats[i].gameObject == gameObject) continue;
                
                Vector3 diff = transform.position - _nearbyBats[i].transform.position;
                float distance = diff.magnitude;
                
                if (distance > 0.01f && distance < separationRadius)
                {
                    // Càng gần càng đẩy mạnh
                    separation += diff.normalized / distance;
                    count++;
                }
            }
            
            if (count > 0)
            {
                separation /= count;
                separation *= separationForce;
            }
            
            return separation;
        }
        
        #endregion

        #region Movement
        
        private void MoveTowards(Vector3 target, float moveSpeed)
        {
            MoveTowardsWithSeparation(target, moveSpeed, Vector3.zero);
        }
        
        private void MoveTowardsWithSeparation(Vector3 target, float moveSpeed, Vector3 separation)
        {
            Vector3 direction = (target - transform.position).normalized;
            
            // Thêm separation vào hướng di chuyển
            Vector3 finalDirection = direction + separation * 0.5f;
            
            // Thêm obstacle avoidance
            if (enableObstacleAvoidance)
            {
                Vector3 avoidance = CalculateObstacleAvoidance(direction);
                finalDirection += avoidance;
            }
            
            // Check if stuck và xử lý
            if (enableObstacleAvoidance)
            {
                CheckIfStuck();
                if (_stuckTimer > 0.5f)
                {
                    // Đang bị kẹt, thêm hướng random để thoát
                    finalDirection += GetUnstuckDirection();
                }
            }
            
            finalDirection = finalDirection.normalized;
            
            // Xoay về hướng mục tiêu (vẫn xoay về Player, không xoay theo separation)
            if (direction.magnitude > 0.1f)
            {
                Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
                if (lookDir.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            
            // Di chuyển theo hướng cuối cùng (có separation + avoidance)
            transform.position += finalDirection * moveSpeed * Time.deltaTime;
            
            // Cập nhật base height khi chase
            if (_currentState == BatState.Chase)
            {
                _baseHeight = transform.position.y;
            }
        }
        
        /// <summary>
        /// Tính vector né chướng ngại vật bằng raycast.
        /// </summary>
        private Vector3 CalculateObstacleAvoidance(Vector3 moveDirection)
        {
            Vector3 avoidance = Vector3.zero;
            
            if (numRays <= 0) return avoidance;
            
            float angleStep = raySpreadAngle / (numRays - 1);
            float startAngle = -raySpreadAngle / 2f;
            
            for (int i = 0; i < numRays; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * moveDirection;
                
                // Raycast phía trước
                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, obstacleDetectionRange, obstacleLayer))
                {
                    // Có chướng ngại vật, tính hướng né
                    float weight = 1f - (hit.distance / obstacleDetectionRange);
                    Vector3 avoidDir = Vector3.Cross(Vector3.up, rayDirection).normalized;
                    
                    // Né về hướng xa chướng ngại vật
                    if (angle > 0) avoidDir = -avoidDir;
                    
                    avoidance += avoidDir * weight * obstacleAvoidanceForce;
                    
                    #if UNITY_EDITOR
                    Debug.DrawRay(transform.position, rayDirection * hit.distance, Color.red);
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.DrawRay(transform.position, rayDirection * obstacleDetectionRange, Color.green);
                    #endif
                }
            }
            
            // Raycast lên/xuống để né trần/sàn
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit downHit, obstacleDetectionRange * 0.5f, obstacleLayer))
            {
                avoidance.y += (1f - downHit.distance / (obstacleDetectionRange * 0.5f)) * obstacleAvoidanceForce;
            }
            if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit upHit, obstacleDetectionRange * 0.5f, obstacleLayer))
            {
                avoidance.y -= (1f - upHit.distance / (obstacleDetectionRange * 0.5f)) * obstacleAvoidanceForce;
            }
            
            return avoidance;
        }
        
        /// <summary>
        /// Kiểm tra xem có bị kẹt không (không di chuyển được).
        /// </summary>
        private void CheckIfStuck()
        {
            float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
            
            if (distanceMoved < 0.01f)
            {
                _stuckTimer += Time.deltaTime;
            }
            else
            {
                _stuckTimer = 0f;
            }
            
            _lastPosition = transform.position;
        }
        
        /// <summary>
        /// Lấy hướng random để thoát khi bị kẹt.
        /// </summary>
        private Vector3 GetUnstuckDirection()
        {
            // Thử random hướng mới
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f), // Bay lên một chút
                Random.Range(-1f, 1f)
            ).normalized;
            
            // Reset stuck timer nếu đã đổi hướng đủ lâu
            if (_stuckTimer > 2f)
            {
                _stuckTimer = 0f;
            }
            
            return randomDir * obstacleAvoidanceForce;
        }
        
        private void UpdateAnimator(float moveSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, moveSpeed / chaseSpeed); // Normalized 0-1
            }
        }
        
        #endregion

        #region Explosion
        
        public void Explode()
        {
            if (_isDying) return;
            _isDying = true;
            _currentState = BatState.Exploding;
            
            // Gây damage cho Player
            DealDamageToPlayer();
            
            // Spawn explosion effect
            if (explosionEffectPrefab != null)
            {
                var effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Play explosion sound
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            }
            
            // Broadcast death event
            GameEvents.TriggerEnemyKilled(gameObject, pointValue);
            
            // Cleanup
            if (useObjectPooling && ObjectPool.HasInstance)
            {
                ObjectPool.Instance.Despawn(poolTag, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void DealDamageToPlayer()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    var playerHealth = hitCollider.GetComponent<Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damageToPlayer);
                    }
                    break;
                }
            }
        }
        
        #endregion

        #region Override Methods
        
        public override void Initialize()
        {
            base.Initialize();
            _currentState = BatState.Patrol;
            _spawnPosition = transform.position;
            _baseHeight = transform.position.y;
            PickNewPatrolTarget();
        }
        
        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            _currentState = BatState.Patrol;
        }
        
        protected override void HandlePlayerDeath()
        {
            _currentState = BatState.Patrol;
            PickNewPatrolTarget();
        }
        
        protected override void HandleGameEnd(bool isVictory)
        {
            _currentState = BatState.Patrol;
        }
        
        #endregion

        #region Debug Gizmos
        
        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _spawnPosition : transform.position;
            
            // Patrol radius (xanh lá)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, patrolRadius);
            
            // Detection range (vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Lose target range (cam)
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);
            
            // Explode range (đỏ)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explodeRange);
            
            // Patrol target (xanh dương)
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_patrolTarget, 0.3f);
                Gizmos.DrawLine(transform.position, _patrolTarget);
            }
            
            // Current state label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, _currentState.ToString());
            #endif
        }
        
        #endregion
    }
}
