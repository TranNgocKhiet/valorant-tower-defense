namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Represents streaming session information stored in DynamoDB.
    /// </summary>
    [System.Serializable]
    public class StreamInfo
    {
        /// <summary>Unique stream domain identifier (e.g., "stream-abc123").</summary>
        public string StreamDomain { get; set; }
        
        /// <summary>Player/streamer username.</summary>
        public string PlayerID { get; set; }
        
        /// <summary>ISO 8601 formatted timestamp when stream started.</summary>
        public string StreamStartTime { get; set; }
        
        /// <summary>Current game level being played.</summary>
        public int CurrentLevel { get; set; }
        
        /// <summary>Stream status: "active" or "ended".</summary>
        public string Status { get; set; }
        
        /// <summary>Session ID from the streaming system.</summary>
        public string SessionId { get; set; }
    }
}
