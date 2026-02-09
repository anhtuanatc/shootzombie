using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShootZombie.Core;

namespace ShootZombie.UI
{
    /// <summary>
    /// Controls the main menu UI.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;
        
        [Header("High Score")]
        [SerializeField] private TextMeshProUGUI highScoreText;
        
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private bool animateTitle = true;
        
        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            SetupButtons();
            ShowMainPanel();
            UpdateHighScoreDisplay();
            
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
            
            if (animateTitle)
            {
                StartCoroutine(AnimateTitleCoroutine());
            }
        }

        private void OnEnable()
        {
            UpdateHighScoreDisplay();
        }
        
        #endregion

        #region Setup
        
        private void SetupButtons()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
                
            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);
                
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        #endregion

        #region Panel Control
        
        private void ShowMainPanel()
        {
            SetPanelActive(mainPanel, true);
            SetPanelActive(optionsPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        #endregion

        #region Button Handlers
        
        public void OnPlayClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.StartGame();
            }
        }

        public void OnOptionsClicked()
        {
            SetPanelActive(mainPanel, false);
            SetPanelActive(optionsPanel, true);
        }

        public void OnCreditsClicked()
        {
            SetPanelActive(mainPanel, false);
            SetPanelActive(creditsPanel, true);
        }

        public void OnBackClicked()
        {
            ShowMainPanel();
        }

        public void OnQuitClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.QuitGame();
            }
        }
        
        #endregion

        #region High Score
        
        private void UpdateHighScoreDisplay()
        {
            if (highScoreText != null && GameManager.HasInstance)
            {
                highScoreText.text = $"HIGH SCORE: {GameManager.Instance.HighScore:N0}";
            }
        }
        
        #endregion

        #region Animations
        
        private System.Collections.IEnumerator AnimateTitleCoroutine()
        {
            if (titleText == null) yield break;
            
            Vector3 originalScale = titleText.transform.localScale;
            
            while (true)
            {
                // Subtle pulse animation
                float t = Time.time * 2f;
                float scale = 1f + Mathf.Sin(t) * 0.02f;
                titleText.transform.localScale = originalScale * scale;
                
                yield return null;
            }
        }
        
        #endregion
    }
}
