using System;
using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for the main streaming orchestration component.
    /// </summary>
    public interface IStreamManager
    {
        /// <summary>
        /// Starts the streaming session.
        /// </summary>
        void StartStreaming();
        
        /// <summary>
        /// Stops the streaming session.
        /// </summary>
        void StopStreaming();
        
        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        /// <returns>The current connection state.</returns>
        ConnectionState GetConnectionState();
        
        /// <summary>
        /// Gets the current streaming statistics.
        /// </summary>
        /// <returns>Current streaming statistics.</returns>
        StreamingStats GetStats();
        
        /// <summary>
        /// Updates the streaming configuration.
        /// </summary>
        /// <param name="config">The new configuration to apply.</param>
        void UpdateConfiguration(StreamingConfig config);
        
        /// <summary>
        /// Event fired when the connection state changes.
        /// </summary>
        event Action<ConnectionState> OnConnectionStateChanged;
        
        /// <summary>
        /// Event fired when an error occurs.
        /// </summary>
        event Action<string> OnError;
        
        /// <summary>
        /// Event fired when streaming statistics are updated.
        /// </summary>
        event Action<StreamingStats> OnStatsUpdated;
    }
}
