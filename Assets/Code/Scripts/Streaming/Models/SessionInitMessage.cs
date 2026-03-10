using System;

namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Message sent to initialize a streaming session.
    /// </summary>
    public class SessionInitMessage
    {
        /// <summary>Unique identifier for the player.</summary>
        public string PlayerId { get; set; }
        
        /// <summary>Game version string.</summary>
        public string GameVersion { get; set; }
        
        /// <summary>UTC timestamp when the session started.</summary>
        public DateTime SessionStartTime { get; set; }
        
        /// <summary>Streaming configuration for this session.</summary>
        public StreamingConfig Config { get; set; }
    }
}
