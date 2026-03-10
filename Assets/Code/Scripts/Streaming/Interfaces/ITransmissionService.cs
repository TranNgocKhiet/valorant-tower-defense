using System.Threading.Tasks;
using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for transmitting streaming data to the API endpoint.
    /// </summary>
    public interface ITransmissionService
    {
        /// <summary>
        /// Asynchronously sends a frame to the API endpoint.
        /// </summary>
        /// <param name="frame">The encoded frame to transmit.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>Result of the transmission attempt.</returns>
        Task<TransmissionResult> SendFrameAsync(EncodedFrame frame, string authToken);
        
        /// <summary>
        /// Asynchronously sends a session initialization message.
        /// </summary>
        /// <param name="message">The session initialization message.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        Task<bool> SendSessionInitAsync(SessionInitMessage message, string authToken);
        
        /// <summary>
        /// Asynchronously sends a session termination message.
        /// </summary>
        /// <param name="message">The session termination message.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>True if termination was successful, false otherwise.</returns>
        Task<bool> SendSessionTerminateAsync(SessionTerminateMessage message, string authToken);
        
        /// <summary>
        /// Sets the API endpoint URL for transmission.
        /// </summary>
        /// <param name="url">The API endpoint URL.</param>
        void SetEndpoint(string url);
    }
}
