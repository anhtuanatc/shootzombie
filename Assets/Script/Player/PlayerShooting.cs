using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Player
{
    /// <summary>
    /// Handles player shooting mechanics.
    /// Supports both regular shooting and object pooling for bullets.
    /// </summary>
    public class PlayerShooting : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Weapon Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletForce = 20f;
        [SerializeField] private float fireRate = 0.2f;
        
        [Header("Ammo Settings")]
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private int currentAmmo = 30;
        [SerializeField] private float reloadTime = 1.5f;
        [SerializeField] private bool infiniteAmmo = true;
        
        [Header("Audio")]
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip emptyClickSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.7f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private float muzzleFlashDuration = 0.1f;
        
        [Header("Object Pooling")]
        [SerializeField] private bool useObjectPooling = false;
        [SerializeField] private string bulletPoolTag = "Bullet";
        
        [Header("Movement While Shooting")]
        [Tooltip("Nếu true, player sẽ dừng di chuyển khi bắn")]
        [SerializeField] private bool stopWhenShooting = true;
        [Tooltip("Thời gian dừng sau khi bắn (giây)")]
        [SerializeField] private float stopDuration = 0.30f;
        
        [Header("Animation Sync")]
        [Tooltip("Delay trước khi đạn được bắn ra (để sync với animation)")]
        [SerializeField] private float shootDelay = 0.20f;
        
        [Header("Multi-Shot (Shotgun Style)")]
        [Tooltip("Số đạn bắn ra mỗi lần")]
        [SerializeField] private int bulletsPerShot = 3;
        
        [Tooltip("Góc spread giữa các viên đạn (độ)")]
        [SerializeField] private float spreadAngle = 15f;
        
        [Tooltip("Khoảng cách ngang giữa các viên đạn tại điểm spawn")]
        [SerializeField] private float spawnSpreadDistance = 0.3f;
        
        #endregion

        #region Properties
        
        /// <summary>Current ammo count</summary>
        public int CurrentAmmo => currentAmmo;
        
        /// <summary>Maximum ammo capacity</summary>
        public int MaxAmmo => maxAmmo;
        
        /// <summary>Is the player currently reloading?</summary>
        public bool IsReloading { get; private set; }
        
        /// <summary>Can the player currently shoot?</summary>
        public bool CanShoot => !IsReloading && (infiniteAmmo || currentAmmo > 0) && _canFire;
        
        #endregion

        #region Private Fields
        
        private AudioSource _audioSource;
        private Animator _animator;
        private InputHandler _input;
        private UnityEngine.Camera _mainCamera;
        
        private float _nextFireTime;
        private bool _canFire = true;
        
        // Cached aim direction for shooting
        private Vector3 _aimDirection;
        
        // Lưu hướng bắn chính xác tại thời điểm click (không bị thay đổi bởi delay)
        private Vector3 _savedShootDirection;
        
        // Reference to PlayerMovement for stopping
        private PlayerMovement _playerMovement;
        
        private static readonly int ShootTriggerHash = Animator.StringToHash("Shoot");
        private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _animator = GetComponentInChildren<Animator>();
            _input = GetComponent<InputHandler>();
            _mainCamera = UnityEngine.Camera.main;
            _playerMovement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            ValidateSetup();
            currentAmmo = maxAmmo;
        }

        private void Update()
        {
            if (GameManager.HasInstance && GameManager.Instance.IsPaused) return;
            if (!_canFire) return;
            
            HandleInput();
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

        #region Initialization
        
        private void ValidateSetup()
        {
            if (firePoint == null)
            {
                Debug.LogError("[PlayerShooting] Fire point not assigned!");
            }
            
            if (bulletPrefab == null)
            {
                Debug.LogError("[PlayerShooting] Bullet prefab not assigned!");
            }
            
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        #endregion

        #region Input Handling
        
        private void HandleInput()
        {
            // Shooting
            if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
            {
                if (CanShoot)
                {
                    Shoot();
                }
                else if (!infiniteAmmo && currentAmmo <= 0 && !IsReloading)
                {
                    PlayEmptyClick();
                    StartReload();
                }
            }
            
            // Manual reload
            if (Input.GetKeyDown(KeyCode.R) && !IsReloading && currentAmmo < maxAmmo)
            {
                StartReload();
            }
        }
        
        #endregion

        #region Shooting
        
        /// <summary>
        /// Fires a bullet from the fire point toward the mouse position.
        /// </summary>
        public void Shoot()
        {
            if (!CanShoot) return;
            
            _nextFireTime = Time.time + fireRate;
            
            // LOCK ROTATION TRƯỚC (ngăn PlayerMovement ghi đè)
            if (_playerMovement != null)
            {
                _playerMovement.CanRotate = false;
            }
            
            // Tính hướng ngắm và xoay player
            RotateTowardMouseInstant();
            
            // Lưu hướng bắn = hướng player đang nhìn sau khi đã xoay
            // QUAN TRỌNG: Dùng _aimDirection.normalized thay vì transform.forward
            // Lý do: Model player có thể bị nghiêng do animation/physics, khiến transform.forward hướng lên trời/xuống đất
            if (_aimDirection.sqrMagnitude > 0.001f)
            {
                _savedShootDirection = _aimDirection.normalized;
                _savedShootDirection.y = 0; // Đảm bảo bắn thẳng ngang
            }
            else
            {
                _savedShootDirection = transform.forward;
                _savedShootDirection.y = 0;
            }
            
            #if UNITY_EDITOR
            Debug.Log($"[Shoot] AimDir: {_aimDirection}, SavedDir: {_savedShootDirection}, Forward: {transform.forward}");
            #endif
            
            // Dừng di chuyển khi bắn
            if (stopWhenShooting && _playerMovement != null)
            {
                _playerMovement.CanMove = false;
                CancelInvoke(nameof(EnableMovement));
                Invoke(nameof(EnableMovement), stopDuration);
            }
            
            // Animation trigger NGAY
            if (_animator != null)
            {
                _animator.SetTrigger(ShootTriggerHash);
            }
            
            // Consume ammo NGAY
            if (!infiniteAmmo)
            {
                currentAmmo--;
            }
            
            // Delay spawn đạn để sync với animation
            if (shootDelay > 0)
            {
                Invoke(nameof(SpawnBulletDelayed), shootDelay); // Đạn bắn ra sau 0.20s
            }
            else
            {
                SpawnBulletDelayed(); // Không delay
            }
            
            // Broadcast event
            GameEvents.TriggerPlayerShoot();
        }
        
        /// <summary>
        /// Spawn đạn (được gọi sau delay để sync với animation).
        /// </summary>
        private void SpawnBulletDelayed()
        {
            // Bắn nhiều đạn với góc spread
            for (int i = 0; i < bulletsPerShot; i++)
            {
                // Tính góc cho viên đạn này
                float angleOffset = 0f;
                float positionOffset = 0f;
                
                if (bulletsPerShot > 1)
                {
                    // Chia đều góc spread
                    float halfSpread = spreadAngle / 2f;
                    angleOffset = Mathf.Lerp(-halfSpread, halfSpread, (float)i / (bulletsPerShot - 1));
                    
                    // Chia đều khoảng cách spawn
                    float halfDistance = spawnSpreadDistance * (bulletsPerShot - 1) / 2f;
                    positionOffset = Mathf.Lerp(-halfDistance, halfDistance, (float)i / (bulletsPerShot - 1));
                }
                
                // Tính hướng với góc offset
                Vector3 shootDirection = Quaternion.Euler(0, angleOffset, 0) * _savedShootDirection;
                
                // Tính vị trí spawn với offset ngang (perpendicular to shoot direction)
                Vector3 right = Vector3.Cross(Vector3.up, _savedShootDirection).normalized;
                Vector3 spawnPosition = firePoint.position + right * positionOffset;
                
                // Spawn bullet tại vị trí offset
                GameObject bullet = SpawnBulletAtPosition(spawnPosition);
                
                if (bullet != null)
                {
                    Rigidbody rb = bullet.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.AddForce(shootDirection * bulletForce, ForceMode.Impulse);
                        
                        #if UNITY_EDITOR
                        Debug.DrawRay(bullet.transform.position, shootDirection * 10f, Color.red, 2f);
                        Debug.Log($"[Shot {i}] SpawnPos: {spawnPosition} | Dir: {shootDirection}");
                        #endif
                    }
                }
            }
            
            // Effects (muzzle flash, sound)
            PlayShootEffects();
        }
        
        /// <summary>
        /// Cho phép di chuyển và xoay trở lại sau khi bắn.
        /// </summary>
        private void EnableMovement()
        {
            if (_playerMovement != null)
            {
                _playerMovement.CanMove = true;
                _playerMovement.CanRotate = true;
            }
        }
        
        /// <summary>
        /// Xoay player tức thời về hướng chuột (không smooth, để bắn chính xác).
        /// </summary>
        private void RotateTowardMouseInstant()
        {
            if (_mainCamera == null) _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Phương pháp CHÍNH: Dùng Plane tại độ cao của SÚNG (FirePoint)
            // Thay vì độ cao của Player (chân). Điều này giúp vị trí chuột và đạn trùng nhau về phối cảnh.
            float planeHeight = (firePoint != null) ? firePoint.position.y : transform.position.y;
            Plane playerPlane = new Plane(Vector3.up, new Vector3(0, planeHeight, 0));
            
            if (playerPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPosition = ray.GetPoint(distance);
                
                // Tính hướng: Vị trí chuột (ở độ cao súng) - Vị trí player (đã chỉnh Y = độ cao súng)
                Vector3 playerPosAtGunHeight = new Vector3(transform.position.x, planeHeight, transform.position.z);
                _aimDirection = mouseWorldPosition - playerPosAtGunHeight;
                _aimDirection.y = 0; // Giữ trên mặt phẳng ngang
                
                if (_aimDirection.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(_aimDirection);
                    
                    #if UNITY_EDITOR
                    // Debug: Vẽ đường từ player đến vị trí chuột (CYAN)
                    Debug.DrawLine(transform.position, mouseWorldPosition, Color.cyan, 2f);
                    // Debug: Vẽ hướng bắn (YELLOW)
                    Debug.DrawRay(transform.position, _aimDirection.normalized * 5f, Color.yellow, 2f);
                    
                    Debug.Log($"[Aim] WorldPos: {mouseWorldPosition} | AimDir: {_aimDirection.normalized}");
                    #endif
                }
            }
            else
            {
                // Fallback: Nếu Plane không hoạt động (rất hiếm)
                Debug.LogWarning("[PlayerShooting] Plane raycast failed! Camera might be parallel to ground.");
            }
        }

        private GameObject SpawnBullet()
        {
            return SpawnBulletAtPosition(firePoint.position);
        }
        
        private GameObject SpawnBulletAtPosition(Vector3 position)
        {
            if (useObjectPooling && ObjectPool.HasInstance)
            {
                return ObjectPool.Instance.Spawn(bulletPoolTag, position, firePoint.rotation);
            }
            else
            {
                return Instantiate(bulletPrefab, position, firePoint.rotation);
            }
        }
        
        #endregion

        #region Reload
        
        /// <summary>
        /// Starts the reload process.
        /// </summary>
        public void StartReload()
        {
            if (IsReloading || currentAmmo >= maxAmmo) return;
            if (infiniteAmmo) return;
            
            IsReloading = true;
            
            // Play reload sound
            if (reloadSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(reloadSound);
            }
            
            // Trigger animation
            if (_animator != null)
            {
                _animator.SetTrigger(ReloadTriggerHash);
            }
            
            // Complete reload after delay
            Invoke(nameof(CompleteReload), reloadTime);
        }

        private void CompleteReload()
        {
            currentAmmo = maxAmmo;
            IsReloading = false;
            Debug.Log("[PlayerShooting] Reload complete!");
        }
        
        #endregion

        #region Effects
        
        private void PlayShootEffects()
        {
            // Muzzle flash
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                Destroy(flash, muzzleFlashDuration);
            }
            
            // Sound
            if (shootSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(shootSound, shootVolume);
            }
        }

        private void PlayEmptyClick()
        {
            if (emptyClickSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(emptyClickSound);
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void HandlePlayerDeath()
        {
            _canFire = false;
            CancelInvoke(nameof(CompleteReload));
            IsReloading = false;
        }

        private void HandlePlayerRespawn()
        {
            _canFire = true;
            currentAmmo = maxAmmo;
            IsReloading = false;
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Adds ammo to the player's supply.
        /// </summary>
        public void AddAmmo(int amount)
        {
            currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        }

        /// <summary>
        /// Sets the weapon's fire rate.
        /// </summary>
        public void SetFireRate(float newFireRate)
        {
            fireRate = Mathf.Max(0.05f, newFireRate);
        }
        
        #endregion
    }
}
