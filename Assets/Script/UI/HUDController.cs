using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShootZombie.Core;
using System.Collections;

namespace ShootZombie.UI
{
    /// <summary>
    /// Controls the in-game HUD (Heads-Up Display).
    /// Displays health, score, wave, combo, and other game info.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Health Display")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private bool animateHealthChange = true;
        [SerializeField] private float healthAnimationSpeed = 5f;
        
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private bool animateScoreChange = true;
        [SerializeField] private float scoreAnimationDuration = 0.5f;
        
        [Header("Wave Display")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private GameObject waveAnnouncementPanel;
        [SerializeField] private TextMeshProUGUI waveAnnouncementText;
        [SerializeField] private float waveAnnouncementDuration = 2f;
        
        [Header("Combo Display")]
        [SerializeField] private GameObject comboPanel;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI comboMultiplierText;
        [SerializeField] private float comboHideDuration = 2f;
        
        [Header("Enemy Count")]
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private Image enemyCountIcon;
        
        [Header("Ammo Display")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject reloadingIndicator;
        
        [Header("Kill Counter")]
        [SerializeField] private TextMeshProUGUI killCountText;
        
        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color crosshairNormalColor = Color.white;
        [SerializeField] private Color crosshairEnemyColor = Color.red;
        
        [Header("Damage Indicators")]
        [SerializeField] private Image damageVignetteImage;
        [SerializeField] private float vignetteFadeDuration = 0.5f;
        [SerializeField] private Color lowHealthVignetteColor = new Color(1, 0, 0, 0.3f);
        
        #endregion

        #region Private Fields
        
        private float _targetHealth;
        private float _currentDisplayedHealth;
        private int _targetScore;
        private int _currentDisplayedScore;
        private Coroutine _scoreAnimationCoroutine;
        private Coroutine _comboHideCoroutine;
        private Coroutine _waveAnnouncementCoroutine;
        private Coroutine _vignetteFadeCoroutine;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeHUD();
        }

        private void Update()
        {
            UpdateHealthAnimation();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region Initialization
        
        private void InitializeHUD()
        {
            // Initialize with current values if GameManager exists
            if (GameManager.HasInstance)
            {
                var gm = GameManager.Instance;
                UpdateScore(gm.Score);
                UpdateHighScore(gm.HighScore);
                UpdateWave(gm.CurrentWave);
            }
            
            // Hide optional elements
            if (comboPanel != null) comboPanel.SetActive(false);
            if (waveAnnouncementPanel != null) waveAnnouncementPanel.SetActive(false);
            if (reloadingIndicator != null) reloadingIndicator.SetActive(false);
            if (damageVignetteImage != null) damageVignetteImage.color = Color.clear;
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnNewHighScore += HandleNewHighScore;
            GameEvents.OnWaveStart += HandleWaveStart;
            GameEvents.OnWaveComplete += HandleWaveComplete;
            GameEvents.OnComboChanged += HandleComboChanged;
            GameEvents.OnEnemyCountChanged += HandleEnemyCountChanged;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnPlayerShoot += HandlePlayerShoot;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnNewHighScore -= HandleNewHighScore;
            GameEvents.OnWaveStart -= HandleWaveStart;
            GameEvents.OnWaveComplete -= HandleWaveComplete;
            GameEvents.OnComboChanged -= HandleComboChanged;
            GameEvents.OnEnemyCountChanged -= HandleEnemyCountChanged;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnPlayerShoot -= HandlePlayerShoot;
        }
        
        #endregion

        #region Health Display
        
        private void HandlePlayerHealthChanged(int current, int max)
        {
            UpdateHealth(current, max);
        }

        private void UpdateHealth(int current, int max)
        {
            float healthPercent = (float)current / max;
            _targetHealth = healthPercent;
            
            if (!animateHealthChange)
            {
                _currentDisplayedHealth = healthPercent;
                ApplyHealthDisplay(healthPercent, current, max);
            }
            
            // Low health vignette
            if (healthPercent <= 0.3f && damageVignetteImage != null)
            {
                damageVignetteImage.color = lowHealthVignetteColor * (1 - healthPercent / 0.3f);
            }
            else if (damageVignetteImage != null)
            {
                damageVignetteImage.color = Color.clear;
            }
        }

        private void UpdateHealthAnimation()
        {
            if (!animateHealthChange) return;
            if (Mathf.Approximately(_currentDisplayedHealth, _targetHealth)) return;
            
            _currentDisplayedHealth = Mathf.Lerp(_currentDisplayedHealth, _targetHealth, healthAnimationSpeed * Time.deltaTime);
            
            // Get actual values for text display
            int maxHealth = 100;
            int currentHealth = Mathf.RoundToInt(_currentDisplayedHealth * maxHealth);
            
            ApplyHealthDisplay(_currentDisplayedHealth, currentHealth, maxHealth);
        }

        private void ApplyHealthDisplay(float percent, int current, int max)
        {
            if (healthSlider != null)
            {
                healthSlider.value = percent;
            }
            
            if (healthFillImage != null && healthGradient != null)
            {
                healthFillImage.color = healthGradient.Evaluate(percent);
            }
            
            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";
            }
        }
        
        #endregion

        #region Score Display
        
        private void HandleScoreChanged(int newScore)
        {
            UpdateScore(newScore);
        }

        private void HandleNewHighScore(int highScore)
        {
            UpdateHighScore(highScore);
        }

        private void UpdateScore(int newScore)
        {
            _targetScore = newScore;
            
            if (animateScoreChange && gameObject.activeInHierarchy)
            {
                if (_scoreAnimationCoroutine != null)
                {
                    StopCoroutine(_scoreAnimationCoroutine);
                }
                _scoreAnimationCoroutine = StartCoroutine(AnimateScoreCoroutine(newScore));
            }
            else
            {
                _currentDisplayedScore = newScore;
                ApplyScoreDisplay(newScore);
            }
        }

        private IEnumerator AnimateScoreCoroutine(int targetScore)
        {
            int startScore = _currentDisplayedScore;
            float elapsed = 0f;
            
            while (elapsed < scoreAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scoreAnimationDuration;
                
                // Ease out
                t = 1 - Mathf.Pow(1 - t, 3);
                
                _currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
                ApplyScoreDisplay(_currentDisplayedScore);
                
                yield return null;
            }
            
            _currentDisplayedScore = targetScore;
            ApplyScoreDisplay(targetScore);
        }

        private void ApplyScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");
            }
        }

        private void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"BEST: {highScore:N0}";
            }
        }
        
        #endregion

        #region Wave Display
        
        private void HandleWaveStart(int waveNumber)
        {
            UpdateWave(waveNumber);
            ShowWaveAnnouncement(waveNumber);
        }

        private void HandleWaveComplete(int waveNumber)
        {
            ShowWaveCompleteAnnouncement(waveNumber);
        }

        private void UpdateWave(int waveNumber)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {waveNumber}";
            }
        }

        private void ShowWaveAnnouncement(int waveNumber)
        {
            if (waveAnnouncementPanel == null || waveAnnouncementText == null) return;
            
            if (_waveAnnouncementCoroutine != null)
            {
                StopCoroutine(_waveAnnouncementCoroutine);
            }
            
            waveAnnouncementText.text = $"WAVE {waveNumber}";
            _waveAnnouncementCoroutine = StartCoroutine(ShowAnnouncementCoroutine(waveAnnouncementPanel));
        }

        private void ShowWaveCompleteAnnouncement(int waveNumber)
        {
            if (waveAnnouncementPanel == null || waveAnnouncementText == null) return;
            
            if (_waveAnnouncementCoroutine != null)
            {
                StopCoroutine(_waveAnnouncementCoroutine);
            }
            
            waveAnnouncementText.text = $"WAVE {waveNumber} COMPLETE!";
            _waveAnnouncementCoroutine = StartCoroutine(ShowAnnouncementCoroutine(waveAnnouncementPanel));
        }

        private IEnumerator ShowAnnouncementCoroutine(GameObject panel)
        {
            panel.SetActive(true);
            
            // Scale in animation
            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.zero;
                float elapsed = 0f;
                
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / 0.3f;
                    t = 1 - Mathf.Pow(1 - t, 3); // Ease out
                    rect.localScale = Vector3.one * t;
                    yield return null;
                }
                
                rect.localScale = Vector3.one;
            }
            
            yield return new WaitForSeconds(waveAnnouncementDuration);
            
            panel.SetActive(false);
        }
        
        #endregion

        #region Combo Display
        
        private void HandleComboChanged(int combo)
        {
            UpdateCombo(combo);
        }

        private void UpdateCombo(int combo)
        {
            if (comboPanel == null) return;
            
            if (combo <= 1)
            {
                comboPanel.SetActive(false);
                return;
            }
            
            comboPanel.SetActive(true);
            
            if (comboText != null)
            {
                comboText.text = $"{combo} COMBO";
            }
            
            if (comboMultiplierText != null)
            {
                comboMultiplierText.text = $"x{combo}";
            }
            
            // Auto-hide after duration
            if (_comboHideCoroutine != null)
            {
                StopCoroutine(_comboHideCoroutine);
            }
            _comboHideCoroutine = StartCoroutine(HideComboAfterDelay());
            
            // Punch scale animation
            StartCoroutine(PunchScaleCoroutine(comboPanel.transform));
        }

        private IEnumerator HideComboAfterDelay()
        {
            yield return new WaitForSeconds(comboHideDuration);
            
            if (comboPanel != null)
            {
                comboPanel.SetActive(false);
            }
        }

        private IEnumerator PunchScaleCoroutine(Transform target)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = originalScale * 1.2f;
            
            target.localScale = punchScale;
            
            float elapsed = 0f;
            float duration = 0.15f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }
            
            target.localScale = originalScale;
        }
        
        #endregion

        #region Enemy Count Display
        
        private void HandleEnemyCountChanged(int count)
        {
            UpdateEnemyCount(count);
        }

        private void UpdateEnemyCount(int count)
        {
            if (enemyCountText != null)
            {
                enemyCountText.text = count.ToString();
            }
        }
        
        #endregion

        #region Kill Counter
        
        private void HandleEnemyKilled(GameObject enemy, int points)
        {
            UpdateKillCount();
        }

        private void UpdateKillCount()
        {
            if (killCountText != null && GameManager.HasInstance)
            {
                killCountText.text = GameManager.Instance.TotalKills.ToString();
            }
        }
        
        #endregion

        #region Damage Feedback
        
        private void HandlePlayerDamaged(int damage)
        {
            ShowDamageVignette();
        }

        private void ShowDamageVignette()
        {
            if (damageVignetteImage == null) return;
            
            if (_vignetteFadeCoroutine != null)
            {
                StopCoroutine(_vignetteFadeCoroutine);
            }
            
            _vignetteFadeCoroutine = StartCoroutine(DamageVignetteCoroutine());
        }

        private IEnumerator DamageVignetteCoroutine()
        {
            Color targetColor = new Color(1, 0, 0, 0.5f);
            damageVignetteImage.color = targetColor;
            
            float elapsed = 0f;
            
            while (elapsed < vignetteFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / vignetteFadeDuration;
                damageVignetteImage.color = Color.Lerp(targetColor, Color.clear, t);
                yield return null;
            }
            
            damageVignetteImage.color = Color.clear;
        }
        
        #endregion

        #region Other Events
        
        private void HandlePlayerShoot()
        {
            // Could add visual feedback here
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Updates ammo display.
        /// </summary>
        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{current}/{max}";
            }
        }

        /// <summary>
        /// Shows or hides the reloading indicator.
        /// </summary>
        public void SetReloading(bool isReloading)
        {
            if (reloadingIndicator != null)
            {
                reloadingIndicator.SetActive(isReloading);
            }
        }

        /// <summary>
        /// Updates crosshair color based on target.
        /// </summary>
        public void SetCrosshairOnEnemy(bool onEnemy)
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = onEnemy ? crosshairEnemyColor : crosshairNormalColor;
            }
        }
        
        #endregion
    }
}
