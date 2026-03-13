using System;

namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Represents a captured gameplay frame with visual data and metadata.
    /// </summary>
    public class GameplayFrame
    {
        public string StreamDomain { get; set; }
        /// <summary>Unique sequence number for frame ordering.</summary>
        public long SequenceNumber { get; set; }
        
        /// <summary>UTC timestamp when the frame was captured.</summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>Raw visual frame data (JPEG encoded bytes).</summary>
        public byte[] VisualData { get; set; }
        
        /// <summary>Game state metadata at the time of capture.</summary>
        public GameStateMetadata Metadata { get; set; }
    }
}
