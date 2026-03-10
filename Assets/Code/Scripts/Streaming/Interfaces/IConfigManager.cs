using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for managing streaming configuration.
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// Loads the streaming configuration from persistent storage.
        /// </summary>
        /// <returns>The loaded streaming configuration.</returns>
        StreamingConfig LoadConfig();
        
        /// <summary>
        /// Saves the streaming configuration to persistent storage.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        void SaveConfig(StreamingConfig config);
        
        /// <summary>
        /// Validates a streaming configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        bool ValidateConfig(StreamingConfig config);
    }
}
