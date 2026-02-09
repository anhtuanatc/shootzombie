namespace ShootZombie.Core
{
    /// <summary>
    /// Represents the current state of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>Game is in the main menu</summary>
        Menu,
        
        /// <summary>Game is actively being played</summary>
        Playing,
        
        /// <summary>Game is paused</summary>
        Paused,
        
        /// <summary>Player has won</summary>
        Victory,
        
        /// <summary>Player has lost (game over)</summary>
        GameOver,
        
        /// <summary>Game is loading</summary>
        Loading
    }
}
