using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShootZombie.Core;

namespace ShootZombie.UI
{
    /// <summary>
    /// Central UI manager that controls all UI panels and screens.
    /// Listens to GameEvents to update UI state.
    /// </summary>
    public class UIManager : SceneSingleton<UIManager>
    {
        #region Inspector Fields
        
        [Header("Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject loadingPanel;
        
        [Header("Controllers")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameOverController gameOverController;
        [SerializeField] private VictoryController victoryController;
        
        #endregion

        #region Properties
        
        /// <summary>Is any menu currently open?</summary>
        public bool IsMenuOpen => pauseMenuPanel.activeSelf || gameOverPanel.activeSelf || victoryPanel.activeSelf;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void OnSingletonAwake()
        {
            // Hide all panels initially
            HideAllPanels();
        }

        private void Start()
        {
            // Show appropriate panel based on game state
            UpdateUIForGameState(GameManager.HasInstance ? GameManager.Instance.CurrentState : GameState.Menu);
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

        #region Event Subscription
        
        private void SubscribeToEvents()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnGameEnd -= HandleGameEnd;
        }
        
        #endregion

        #region Panel Control
        
        /// <summary>
        /// Hides all UI panels.
        /// </summary>
        public void HideAllPanels()
        {
            SetPanelActive(hudPanel, false);
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(pauseMenuPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(victoryPanel, false);
            SetPanelActive(loadingPanel, false);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private void UpdateUIForGameState(GameState state)
        {
            HideAllPanels();

            switch (state)
            {
                case GameState.Menu:
                    SetPanelActive(mainMenuPanel, true);
                    break;
                    
                case GameState.Playing:
                    SetPanelActive(hudPanel, true);
                    break;
                    
                case GameState.Paused:
                    SetPanelActive(hudPanel, true);
                    SetPanelActive(pauseMenuPanel, true);
                    break;
                    
                case GameState.GameOver:
                    SetPanelActive(hudPanel, true);
                    SetPanelActive(gameOverPanel, true);
                    break;
                    
                case GameState.Victory:
                    SetPanelActive(hudPanel, true);
                    SetPanelActive(victoryPanel, true);
                    break;
                    
                case GameState.Loading:
                    SetPanelActive(loadingPanel, true);
                    break;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Shows the pause menu.
        /// </summary>
        public void ShowPauseMenu()
        {
            SetPanelActive(pauseMenuPanel, true);
        }

        /// <summary>
        /// Hides the pause menu.
        /// </summary>
        public void HidePauseMenu()
        {
            SetPanelActive(pauseMenuPanel, false);
        }

        /// <summary>
        /// Shows the game over screen with final stats.
        /// </summary>
        public void ShowGameOver(int finalScore, int kills, int wave)
        {
            SetPanelActive(gameOverPanel, true);
            
            if (gameOverController != null)
            {
                gameOverController.SetStats(finalScore, kills, wave);
            }
        }

        /// <summary>
        /// Shows the victory screen with final stats.
        /// </summary>
        public void ShowVictory(int finalScore, int kills, int wave)
        {
            SetPanelActive(victoryPanel, true);
            
            if (victoryController != null)
            {
                victoryController.SetStats(finalScore, kills, wave);
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void HandleGameStateChanged(GameState newState)
        {
            UpdateUIForGameState(newState);
        }

        private void HandleGameEnd(bool isVictory)
        {
            if (GameManager.HasInstance)
            {
                var gm = GameManager.Instance;
                
                if (isVictory)
                {
                    ShowVictory(gm.Score, gm.TotalKills, gm.CurrentWave);
                }
                else
                {
                    ShowGameOver(gm.Score, gm.TotalKills, gm.CurrentWave);
                }
            }
        }
        
        #endregion

        #region Button Callbacks (for UI buttons)
        
        public void OnStartGameClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.StartGame();
            }
        }

        public void OnResumeClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        public void OnRestartClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RestartGame();
            }
        }

        public void OnMainMenuClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.GoToMainMenu();
            }
        }

        public void OnQuitClicked()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.QuitGame();
            }
        }
        
        #endregion
    }
}
