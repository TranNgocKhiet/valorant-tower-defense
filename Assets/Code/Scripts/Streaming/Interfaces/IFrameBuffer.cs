using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for buffering encoded frames during streaming.
    /// </summary>
    public interface IFrameBuffer
    {
        /// <summary>
        /// Adds an encoded frame to the buffer.
        /// </summary>
        /// <param name="frame">The encoded frame to buffer.</param>
        void Enqueue(EncodedFrame frame);
        
        /// <summary>
        /// Removes and returns the oldest frame from the buffer.
        /// </summary>
        /// <returns>The oldest encoded frame.</returns>
        EncodedFrame Dequeue();
        
        /// <summary>
        /// Attempts to remove and return the oldest frame from the buffer.
        /// </summary>
        /// <param name="frame">The dequeued frame, or null if buffer is empty.</param>
        /// <returns>True if a frame was dequeued, false if buffer is empty.</returns>
        bool TryDequeue(out EncodedFrame frame);
        
        /// <summary>
        /// Gets the current number of frames in the buffer.
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// Gets the current memory usage of the buffer in megabytes.
        /// </summary>
        float MemoryUsageMB { get; }
        
        /// <summary>
        /// Clears all frames from the buffer.
        /// </summary>
        void Clear();
    }
}
