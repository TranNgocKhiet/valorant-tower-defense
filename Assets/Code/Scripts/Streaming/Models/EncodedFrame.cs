namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Represents an encoded frame ready for transmission.
    /// </summary>
    public class EncodedFrame
    {
        public string streamDomain { get; set; }
        /// <summary>Unique sequence number for frame ordering.</summary>
        public long SequenceNumber { get; set; }
        
        /// <summary>ISO 8601 formatted timestamp string.</summary>
        public string Timestamp { get; set; }
        
        /// <summary>Base64-encoded visual frame data.</summary>
        public string VisualDataBase64 { get; set; }
        
        /// <summary>Game state metadata at the time of capture.</summary>
        public GameStateMetadata Metadata { get; set; }
        
        /// <summary>Format version identifier for API compatibility.</summary>
        public int FormatVersion { get; set; }
        
        /// <summary>Total size of the encoded frame in bytes.</summary>
        public int SizeBytes { get; set; }
    }
}
