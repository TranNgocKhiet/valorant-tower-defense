namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Contains game state information for a specific frame.
    /// </summary>
    public class GameStateMetadata
    {
        /// <summary>Current wave number in the game.</summary>
        public int CurrentWave { get; set; }
        
        /// <summary>Total number of towers placed.</summary>
        public int TowerCount { get; set; }
        
        /// <summary>Current number of enemies alive.</summary>
        public int EnemyCount { get; set; }
        
        /// <summary>Player's current health points.</summary>
        public int PlayerHealth { get; set; }
        
        /// <summary>Player's current score.</summary>
        public int Score { get; set; }
    }
}
