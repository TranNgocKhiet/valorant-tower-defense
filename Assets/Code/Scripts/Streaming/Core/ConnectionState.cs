namespace TowerDefense.Streaming.Core
{
    /// <summary>
    /// Represents the current state of the streaming connection.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>No streaming activity.</summary>
        Idle,
        
        /// <summary>Attempting to establish connection to API endpoint.</summary>
        Connecting,
        
        /// <summary>Connection established, session not yet initialized.</summary>
        Connected,
        
        /// <summary>Actively streaming gameplay frames.</summary>
        Streaming,
        
        /// <summary>Connection lost during streaming.</summary>
        Disconnected,
        
        /// <summary>Attempting to reconnect after connection loss.</summary>
        Reconnecting,
        
        /// <summary>Terminating the streaming session.</summary>
        Terminating,
        
        /// <summary>Error state requiring user intervention.</summary>
        Error
    }
}
