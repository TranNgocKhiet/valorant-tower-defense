using System;

namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Message sent to terminate a streaming session.
    /// </summary>
    public class SessionTerminateMessage
    {
        /// <summary>Unique identifier for the session being terminated.</summary>
        public string SessionId { get; set; }
        
        /// <summary>UTC timestamp when the session ended.</summary>
        public DateTime SessionEndTime { get; set; }
        
        /// <summary>Final statistics for the completed session.</summary>
        public StreamingStats FinalStats { get; set; }
    }
}
