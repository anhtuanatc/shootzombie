using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShootZombie.Core;

namespace ShootZombie.UI
{
    /// <summary>
    /// Controls the pause menu UI.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        
        [Header("Current Stats")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI killsText;
        
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject confirmPanel;
        
        [Header("Confirm Dialog")]
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        [Header("Background")]
        [SerializeField] private Image backgroundOverlay;
        [SerializeField] private float overlayAlpha = 0.7f;
        
        #endregion

        #region Private Fields
        
        private System.Action _pendingConfirmAction;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            SetupButtons();
        }

        private void OnEnable()
        {
            ShowPausePanel();
            UpdateStats();
            AnimateIn();
        }
        
        #endregion

        #region Setup
        
        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
                
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
                
            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);
                
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
                
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
                
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
        
        #endregion

        #region Panel Control
        
        private void ShowPausePanel()
        {
            SetPanelActive(pausePanel, true);
            SetPanelActive(optionsPanel, false);
            SetPanelActive(confirmPanel, false);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        #endregion

        #region Stats Display
        
        private void UpdateStats()
        {
            if (!GameManager.HasInstance) return;
            
            var gm = GameManager.Instance;
            
            if (scoreText != null)
                scoreText.text = $"SCORE: {gm.Score:N0}";
                
            if (waveText != null)
                waveText.text = $"WAVE: {gm.CurrentWave}";
                
            if (killsText != null)
                killsText.text = $"KILLS: {gm.TotalKills}";
        }
        
        #endregion

        #region Button Handlers
        
        public void OnResumeClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        public void OnRestartClicked()
        {
            ShowConfirmDialog("Restart the game?", () =>
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.RestartGame();
                }
            });
        }

        public void OnOptionsClicked()
        {
            SetPanelActive(pausePanel, false);
            SetPanelActive(optionsPanel, true);
        }

        public void OnBackClicked()
        {
            ShowPausePanel();
        }

        public void OnMainMenuClicked()
        {
            ShowConfirmDialog("Return to main menu?\nProgress will be lost.", () =>
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.GoToMainMenu();
                }
            });
        }

        public void OnQuitClicked()
        {
            ShowConfirmDialog("Quit the game?", () =>
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.QuitGame();
                }
            });
        }
        
        #endregion

        #region Confirm Dialog
        
        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            _pendingConfirmAction = onConfirm;
            
            if (confirmText != null)
            {
                confirmText.text = message;
            }
            
            SetPanelActive(pausePanel, false);
            SetPanelActive(confirmPanel, true);
        }

        private void OnConfirmYes()
        {
            _pendingConfirmAction?.Invoke();
            _pendingConfirmAction = null;
            SetPanelActive(confirmPanel, false);
        }

        private void OnConfirmNo()
        {
            _pendingConfirmAction = null;
            ShowPausePanel();
        }
        
        #endregion

        #region Animation
        
        private void AnimateIn()
        {
            if (backgroundOverlay != null)
            {
                StartCoroutine(FadeInOverlay());
            }
        }

        private System.Collections.IEnumerator FadeInOverlay()
        {
            Color startColor = backgroundOverlay.color;
            startColor.a = 0f;
            Color endColor = startColor;
            endColor.a = overlayAlpha;
            
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                backgroundOverlay.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            
            backgroundOverlay.color = endColor;
        }
        
        #endregion
    }
}
