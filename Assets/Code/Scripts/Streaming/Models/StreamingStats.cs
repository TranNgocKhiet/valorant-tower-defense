using System;

namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Contains statistics about the current streaming session.
    /// </summary>
    public class StreamingStats
    {
        /// <summary>Current frame rate being transmitted (frames per second).</summary>
        public float CurrentFrameRate { get; set; }
        
        /// <summary>Total duration of the current streaming session.</summary>
        public TimeSpan SessionDuration { get; set; }
        
        /// <summary>Total number of frames successfully sent.</summary>
        public int FramesSent { get; set; }
        
        /// <summary>Total number of frames dropped due to transmission failures.</summary>
        public int FramesDropped { get; set; }
        
        /// <summary>Current memory usage for frame buffering in megabytes.</summary>
        public float MemoryUsageMB { get; set; }
        
        /// <summary>Estimated CPU usage percentage for streaming operations.</summary>
        public float CpuUsagePercent { get; set; }
    }
}
