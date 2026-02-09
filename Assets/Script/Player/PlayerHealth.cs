using UnityEngine;
using ShootZombie.Core;

namespace ShootZombie.Player
{
    /// <summary>
    /// Manages the player's health, damage handling, and death state.
    /// Broadcasts events for UI updates and game state changes.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invincibilityDuration = 0.5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject damageEffect;
        [SerializeField] private float damageFlashDuration = 0.1f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;
        
        #endregion

        #region Properties
        
        /// <summary>Current health value</summary>
        public int CurrentHealth { get; private set; }
        
        /// <summary>Maximum health value</summary>
        public int MaxHealth => maxHealth;
        
        /// <summary>Health as a normalized value (0-1)</summary>
        public float HealthNormalized => (float)CurrentHealth / maxHealth;
        
        /// <summary>Is the player currently alive?</summary>
        public bool IsAlive => CurrentHealth > 0;
        
        /// <summary>Is the player currently invincible?</summary>
        public bool IsInvincible { get; private set; }
        
        #endregion

        #region Private Fields
        
        private AudioSource _audioSource;
        private Renderer _renderer;
        private Color _originalColor;
        private float _invincibilityTimer;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _renderer = GetComponentInChildren<Renderer>();
            
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateInvincibility();
        }

        private void OnEnable()
        {
            // Subscribe to game events
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes or resets the player's health.
        /// </summary>
        public void Initialize()
        {
            CurrentHealth = maxHealth;
            IsInvincible = false;
            _invincibilityTimer = 0f;
            
            // Broadcast initial health
            GameEvents.TriggerPlayerHealthChanged(CurrentHealth, maxHealth);
        }

        private void HandleGameStart()
        {
            Initialize();
        }
        
        #endregion

        #region Damage & Healing
        
        /// <summary>
        /// Applies damage to the player.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        /// <returns>True if damage was applied, false if player is invincible</returns>
        public bool TakeDamage(int damageAmount)
        {
            // Don't take damage if invincible or already dead
            if (IsInvincible || !IsAlive)
            {
                return false;
            }

            // Apply damage
            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
            
            // Broadcast events
            GameEvents.TriggerPlayerHealthChanged(CurrentHealth, maxHealth);
            GameEvents.TriggerPlayerDamaged(damageAmount);
            
            // Visual & audio feedback
            PlayDamageEffects();
            
            // Check for death
            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Start invincibility frames
                StartInvincibility();
            }

            Debug.Log($"[PlayerHealth] Took {damageAmount} damage. Health: {CurrentHealth}/{maxHealth}");
            return true;
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="healAmount">Amount of health to restore</param>
        public void Heal(int healAmount)
        {
            if (!IsAlive) return;
            
            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healAmount);
            
            int actualHeal = CurrentHealth - previousHealth;
            if (actualHeal > 0)
            {
                GameEvents.TriggerPlayerHealthChanged(CurrentHealth, maxHealth);
                Debug.Log($"[PlayerHealth] Healed {actualHeal}. Health: {CurrentHealth}/{maxHealth}");
            }
        }

        /// <summary>
        /// Fully restores the player's health.
        /// </summary>
        public void FullHeal()
        {
            Heal(maxHealth);
        }
        
        #endregion

        #region Death & Respawn
        
        private void Die()
        {
            Debug.Log("[PlayerHealth] Player died!");
            
            // Play death effects
            if (deathSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(deathSound);
            }
            
            // Broadcast death event
            GameEvents.TriggerPlayerDeath();
            
            // Disable player controls (handled by other scripts via event)
            // The GameManager will handle game over state
        }

        /// <summary>
        /// Respawns the player at a given position.
        /// </summary>
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            Initialize();
            gameObject.SetActive(true);
            
            GameEvents.TriggerPlayerRespawn();
            Debug.Log("[PlayerHealth] Player respawned!");
        }
        
        #endregion

        #region Invincibility
        
        private void StartInvincibility()
        {
            if (invincibilityDuration <= 0) return;
            
            IsInvincible = true;
            _invincibilityTimer = invincibilityDuration;
        }

        private void UpdateInvincibility()
        {
            if (!IsInvincible) return;
            
            _invincibilityTimer -= Time.deltaTime;
            
            // Flash effect during invincibility
            if (_renderer != null)
            {
                bool flash = Mathf.FloorToInt(_invincibilityTimer * 10) % 2 == 0;
                _renderer.material.color = flash ? _originalColor : Color.red;
            }
            
            if (_invincibilityTimer <= 0)
            {
                IsInvincible = false;
                if (_renderer != null)
                {
                    _renderer.material.color = _originalColor;
                }
            }
        }
        
        #endregion

        #region Effects
        
        private void PlayDamageEffects()
        {
            // Spawn damage effect
            if (damageEffect != null)
            {
                var effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // Play hurt sound
            if (hurtSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hurtSound);
            }
            
            // Flash damage color
            if (_renderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            _renderer.material.color = Color.red;
            yield return new WaitForSeconds(damageFlashDuration);
            _renderer.material.color = _originalColor;
        }
        
        #endregion

        #region Debug
        
        [ContextMenu("Debug: Take 25 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(25);
        }

        [ContextMenu("Debug: Heal 25")]
        private void DebugHeal()
        {
            Heal(25);
        }
        
        #endregion
    }
}
