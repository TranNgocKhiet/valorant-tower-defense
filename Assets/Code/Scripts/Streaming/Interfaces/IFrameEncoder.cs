using System.Threading.Tasks;
using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for encoding gameplay frames for transmission.
    /// </summary>
    public interface IFrameEncoder
    {
        /// <summary>
        /// Asynchronously encodes a gameplay frame to a transmission-ready format.
        /// </summary>
        /// <param name="frame">The gameplay frame to encode.</param>
        /// <returns>An encoded frame ready for transmission.</returns>
        Task<EncodedFrame> EncodeAsync(GameplayFrame frame);
        
        /// <summary>
        /// Sets the compression quality for frame encoding.
        /// </summary>
        /// <param name="quality">Compression quality (0-100).</param>
        void SetCompressionQuality(int quality);
    }
}
