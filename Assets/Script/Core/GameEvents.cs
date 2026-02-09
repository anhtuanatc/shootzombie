using System;
using UnityEngine;

namespace ShootZombie.Core
{
    /// <summary>
    /// Central event system for decoupled communication between game systems.
    /// Uses C# events for type-safe, performant event broadcasting.
    /// </summary>
    public static class GameEvents
    {
        #region Game State Events
        
        /// <summary>Fired when game state changes (Menu, Playing, Paused, GameOver)</summary>
        public static event Action<GameState> OnGameStateChanged;
        
        /// <summary>Fired when a new game starts</summary>
        public static event Action OnGameStart;
        
        /// <summary>Fired when game is paused</summary>
        public static event Action OnGamePaused;
        
        /// <summary>Fired when game is resumed</summary>
        public static event Action OnGameResumed;
        
        /// <summary>Fired when game ends (win or lose)</summary>
        public static event Action<bool> OnGameEnd; // bool = isVictory
        
        #endregion

        #region Player Events
        
        /// <summary>Fired when player health changes</summary>
        public static event Action<int, int> OnPlayerHealthChanged; // current, max
        
        /// <summary>Fired when player dies</summary>
        public static event Action OnPlayerDeath;
        
        /// <summary>Fired when player respawns</summary>
        public static event Action OnPlayerRespawn;
        
        /// <summary>Fired when player shoots</summary>
        public static event Action OnPlayerShoot;
        
        /// <summary>Fired when player takes damage</summary>
        public static event Action<int> OnPlayerDamaged; // damage amount
        
        #endregion

        #region Enemy Events
        
        /// <summary>Fired when an enemy is spawned</summary>
        public static event Action<GameObject> OnEnemySpawned;
        
        /// <summary>Fired when an enemy dies</summary>
        public static event Action<GameObject, int> OnEnemyKilled; // enemy, points
        
        /// <summary>Fired when enemy count changes</summary>
        public static event Action<int> OnEnemyCountChanged;
        
        #endregion

        #region Wave Events
        
        /// <summary>Fired when a new wave starts</summary>
        public static event Action<int> OnWaveStart; // wave number
        
        /// <summary>Fired when a wave is completed</summary>
        public static event Action<int> OnWaveComplete; // wave number
        
        /// <summary>Fired when all waves are completed</summary>
        public static event Action OnAllWavesComplete;
        
        #endregion

        #region Score Events
        
        /// <summary>Fired when score changes</summary>
        public static event Action<int> OnScoreChanged;
        
        /// <summary>Fired when a new high score is achieved</summary>
        public static event Action<int> OnNewHighScore;
        
        /// <summary>Fired when combo is updated</summary>
        public static event Action<int> OnComboChanged;
        
        #endregion

        #region Spawner Events
        
        /// <summary>Fired when a spawner is destroyed</summary>
        public static event Action<GameObject> OnSpawnerDestroyed;
        
        /// <summary>Fired when all spawners are destroyed</summary>
        public static event Action OnAllSpawnersDestroyed;
        
        #endregion

        #region Event Triggers
        
        // Game State
        public static void TriggerGameStateChanged(GameState newState) => OnGameStateChanged?.Invoke(newState);
        public static void TriggerGameStart() => OnGameStart?.Invoke();
        public static void TriggerGamePaused() => OnGamePaused?.Invoke();
        public static void TriggerGameResumed() => OnGameResumed?.Invoke();
        public static void TriggerGameEnd(bool isVictory) => OnGameEnd?.Invoke(isVictory);

        // Player
        public static void TriggerPlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);
        public static void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();
        public static void TriggerPlayerRespawn() => OnPlayerRespawn?.Invoke();
        public static void TriggerPlayerShoot() => OnPlayerShoot?.Invoke();
        public static void TriggerPlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);

        // Enemy
        public static void TriggerEnemySpawned(GameObject enemy) => OnEnemySpawned?.Invoke(enemy);
        public static void TriggerEnemyKilled(GameObject enemy, int points) => OnEnemyKilled?.Invoke(enemy, points);
        public static void TriggerEnemyCountChanged(int count) => OnEnemyCountChanged?.Invoke(count);

        // Wave
        public static void TriggerWaveStart(int waveNumber) => OnWaveStart?.Invoke(waveNumber);
        public static void TriggerWaveComplete(int waveNumber) => OnWaveComplete?.Invoke(waveNumber);
        public static void TriggerAllWavesComplete() => OnAllWavesComplete?.Invoke();

        // Score
        public static void TriggerScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
        public static void TriggerNewHighScore(int highScore) => OnNewHighScore?.Invoke(highScore);
        public static void TriggerComboChanged(int combo) => OnComboChanged?.Invoke(combo);

        // Spawner
        public static void TriggerSpawnerDestroyed(GameObject spawner) => OnSpawnerDestroyed?.Invoke(spawner);
        public static void TriggerAllSpawnersDestroyed() => OnAllSpawnersDestroyed?.Invoke();
        
        #endregion

        #region Cleanup
        
        /// <summary>
        /// Clears all event subscriptions. Call this when loading a new scene
        /// or when needing to reset all listeners.
        /// </summary>
        public static void ClearAllEvents()
        {
            OnGameStateChanged = null;
            OnGameStart = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameEnd = null;

            OnPlayerHealthChanged = null;
            OnPlayerDeath = null;
            OnPlayerRespawn = null;
            OnPlayerShoot = null;
            OnPlayerDamaged = null;

            OnEnemySpawned = null;
            OnEnemyKilled = null;
            OnEnemyCountChanged = null;

            OnWaveStart = null;
            OnWaveComplete = null;
            OnAllWavesComplete = null;

            OnScoreChanged = null;
            OnNewHighScore = null;
            OnComboChanged = null;

            OnSpawnerDestroyed = null;
            OnAllSpawnersDestroyed = null;
        }
        
        #endregion
    }
}
