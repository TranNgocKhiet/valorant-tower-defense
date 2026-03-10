namespace TowerDefense.Streaming.Models
{
    /// <summary>
    /// Result of a frame transmission attempt.
    /// </summary>
    public class TransmissionResult
    {
        /// <summary>Whether the transmission was successful.</summary>
        public bool Success { get; set; }
        
        /// <summary>HTTP status code from the API response.</summary>
        public int StatusCode { get; set; }
        
        /// <summary>Error message if transmission failed.</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Number of retry attempts made.</summary>
        public int RetryCount { get; set; }
    }
}
