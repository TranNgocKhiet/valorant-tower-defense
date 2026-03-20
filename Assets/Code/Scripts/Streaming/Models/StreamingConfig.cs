using TowerDefense.Streaming.Core;
using QualityLevel = TowerDefense.Streaming.Core.QualityLevel;

namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Configuration settings for the streaming system.
    /// </summary>
    public class StreamingConfig
    {
        /// <summary>
        /// Hardcoded API endpoint URL for streaming.
        /// TODO: Replace with production API endpoint when ready.
        /// </summary>
        public const string DEFAULT_API_ENDPOINT = "https://ptf00pe25j.execute-api.ap-southeast-1.amazonaws.com";
        
        /// <summary>API endpoint URL for streaming data transmission.</summary>
        public string ApiEndpointUrl { get; set; }
        
        /// <summary>Frame capture rate (1-30 frames per second).</summary>
        public int FrameRate { get; set; }
        
        /// <summary>Quality level affecting frame resolution.</summary>
        public QualityLevel Quality { get; set; }
        
        /// <summary>JPEG compression quality (0-100).</summary>
        public int CompressionQuality { get; set; }
        
        /// <summary>Maximum buffer duration in seconds (default: 30).</summary>
        public int MaxBufferSeconds { get; set; }
        
        /// <summary>Maximum buffer size in megabytes (default: 100).</summary>
        public int MaxBufferSizeMB { get; set; }
        
        /// <summary>Interval between reconnection attempts in seconds (default: 5).</summary>
        public int ReconnectIntervalSeconds { get; set; }
        
        /// <summary>Maximum duration for reconnection attempts in seconds (default: 60).</summary>
        public int MaxReconnectDurationSeconds { get; set; }
        
        /// <summary>Maximum number of retry attempts for frame transmission (default: 3).</summary>
        public int MaxRetryAttempts { get; set; }
        
        /// <summary>
        /// Creates a default streaming configuration.
        /// </summary>
        public static StreamingConfig Default()
        {
            return new StreamingConfig
            {
                ApiEndpointUrl = DEFAULT_API_ENDPOINT,
                FrameRate = 15,
                Quality = QualityLevel.Medium,
                CompressionQuality = 75,
                MaxBufferSeconds = 30,
                MaxBufferSizeMB = 100,
                ReconnectIntervalSeconds = 5,
                MaxReconnectDurationSeconds = 60,
                MaxRetryAttempts = 3
            };
        }
    }
}
