using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShootZombie.Core
{
    /// <summary>
    /// Central game manager that controls game state, score, and overall game flow.
    /// Persists across scenes using the Singleton pattern.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        #region Inspector Fields
        
        [Header("Game Settings")]
        [SerializeField] private int pointsPerKill = 100;
        [SerializeField] private int comboMultiplierMax = 10;
        [SerializeField] private float comboResetTime = 2f;
        
        [Header("Scene Names")]
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "GameScene";
        
        #endregion

        #region Properties
        
        /// <summary>Current game state</summary>
        public GameState CurrentState { get; private set; } = GameState.Menu;
        
        /// <summary>Current player score</summary>
        public int Score { get; private set; }
        
        /// <summary>Current high score (persisted)</summary>
        public int HighScore { get; private set; }
        
        /// <summary>Current wave number</summary>
        public int CurrentWave { get; private set; }
        
        /// <summary>Total enemies killed this session</summary>
        public int TotalKills { get; private set; }
        
        /// <summary>Current combo multiplier</summary>
        public int CurrentCombo { get; private set; }
        
        /// <summary>Is the game currently paused?</summary>
        public bool IsPaused => CurrentState == GameState.Paused;
        
        /// <summary>Is the game currently playing?</summary>
        public bool IsPlaying => CurrentState == GameState.Playing;
        
        #endregion

        #region Private Fields
        
        private float _lastKillTime;
        private GameState _previousState;
        
        private const string HIGH_SCORE_KEY = "ShootZombie_HighScore";
        
        #endregion

        #region Initialization
        
        protected override void OnSingletonAwake()
        {
            LoadHighScore();
            SubscribeToEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnWaveStart += HandleWaveStart;
            GameEvents.OnWaveComplete += HandleWaveComplete;
            GameEvents.OnAllSpawnersDestroyed += HandleAllSpawnersDestroyed;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnWaveStart -= HandleWaveStart;
            GameEvents.OnWaveComplete -= HandleWaveComplete;
            GameEvents.OnAllSpawnersDestroyed -= HandleAllSpawnersDestroyed;
        }
        
        #endregion

        #region Game State Management
        
        /// <summary>
        /// Changes the game state and broadcasts the change.
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            _previousState = CurrentState;
            CurrentState = newState;

            HandleStateTransition(newState);
            GameEvents.TriggerGameStateChanged(newState);

            Debug.Log($"[GameManager] State changed: {_previousState} -> {newState}");
        }

        private void HandleStateTransition(GameState newState)
        {
            switch (newState)
            {
                case GameState.Menu:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Confined;
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                    
                case GameState.Victory:
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    SaveHighScore();
                    break;
                    
                case GameState.Loading:
                    Time.timeScale = 1f;
                    break;
            }
        }
        
        #endregion

        #region Game Flow
        
        /// <summary>
        /// Starts a new game session.
        /// </summary>
        public void StartGame()
        {
            ResetGameStats();
            SetGameState(GameState.Playing);
            GameEvents.TriggerGameStart();
            
            Debug.Log("[GameManager] Game started!");
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            
            SetGameState(GameState.Paused);
            GameEvents.TriggerGamePaused();
        }

        /// <summary>
        /// Resumes the game from pause.
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            
            SetGameState(GameState.Playing);
            GameEvents.TriggerGameResumed();
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }

        /// <summary>
        /// Ends the game with victory or defeat.
        /// </summary>
        public void EndGame(bool isVictory)
        {
            SetGameState(isVictory ? GameState.Victory : GameState.GameOver);
            GameEvents.TriggerGameEnd(isVictory);
            
            Debug.Log($"[GameManager] Game ended. Victory: {isVictory}");
        }

        /// <summary>
        /// Restarts the current game.
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            StartGame();
        }

        /// <summary>
        /// Returns to the main menu.
        /// </summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SetGameState(GameState.Menu);
            
            if (!string.IsNullOrEmpty(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitGame()
        {
            SaveHighScore();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion

        #region Score Management
        
        /// <summary>
        /// Adds points to the current score.
        /// </summary>
        public void AddScore(int points)
        {
            int actualPoints = points * Mathf.Max(1, CurrentCombo);
            Score += actualPoints;
            
            GameEvents.TriggerScoreChanged(Score);

            // Check for new high score
            if (Score > HighScore)
            {
                HighScore = Score;
                GameEvents.TriggerNewHighScore(HighScore);
            }
        }

        /// <summary>
        /// Increments the combo counter.
        /// </summary>
        public void IncrementCombo()
        {
            if (Time.time - _lastKillTime <= comboResetTime)
            {
                CurrentCombo = Mathf.Min(CurrentCombo + 1, comboMultiplierMax);
            }
            else
            {
                CurrentCombo = 1;
            }
            
            _lastKillTime = Time.time;
            GameEvents.TriggerComboChanged(CurrentCombo);
        }

        /// <summary>
        /// Resets the combo counter.
        /// </summary>
        public void ResetCombo()
        {
            CurrentCombo = 0;
            GameEvents.TriggerComboChanged(CurrentCombo);
        }

        private void ResetGameStats()
        {
            Score = 0;
            CurrentWave = 0;
            TotalKills = 0;
            CurrentCombo = 0;
            _lastKillTime = 0f;
            
            GameEvents.TriggerScoreChanged(Score);
            GameEvents.TriggerComboChanged(CurrentCombo);
        }
        
        #endregion

        #region High Score Persistence
        
        private void LoadHighScore()
        {
            HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        }

        private void SaveHighScore()
        {
            if (Score > PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0))
            {
                PlayerPrefs.SetInt(HIGH_SCORE_KEY, HighScore);
                PlayerPrefs.Save();
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void HandleEnemyKilled(GameObject enemy, int points)
        {
            TotalKills++;
            IncrementCombo();
            AddScore(points > 0 ? points : pointsPerKill);
        }

        private void HandlePlayerDeath()
        {
            EndGame(false);
        }

        private void HandleWaveStart(int waveNumber)
        {
            CurrentWave = waveNumber;
            Debug.Log($"[GameManager] Wave {waveNumber} started!");
        }

        private void HandleWaveComplete(int waveNumber)
        {
            Debug.Log($"[GameManager] Wave {waveNumber} completed!");
        }

        private void HandleAllSpawnersDestroyed()
        {
            EndGame(true);
        }
        
        #endregion

        #region Input Handling
        
        private void Update()
        {
            // Handle pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
                {
                    TogglePause();
                }
            }
        }
        
        #endregion

        #region Debug
        
        [ContextMenu("Debug: Add 1000 Points")]
        private void DebugAddPoints()
        {
            AddScore(1000);
        }

        [ContextMenu("Debug: Trigger Victory")]
        private void DebugVictory()
        {
            EndGame(true);
        }

        [ContextMenu("Debug: Trigger Game Over")]
        private void DebugGameOver()
        {
            EndGame(false);
        }
        
        #endregion
    }
}
