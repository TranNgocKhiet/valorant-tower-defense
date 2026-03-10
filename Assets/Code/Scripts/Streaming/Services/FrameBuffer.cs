using System;
using System.Collections.Generic;
using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Thread-safe circular buffer for encoded frames with automatic memory management.
    /// Implements size limits and automatic oldest-frame dropping when buffer is full.
    /// </summary>
    public class FrameBuffer
    {
        private readonly Queue<EncodedFrame> buffer;
        private readonly object lockObject = new object();
        
        private const int MAX_BUFFER_SIZE_MB = 100;
        private const int MAX_BUFFER_SIZE_BYTES = MAX_BUFFER_SIZE_MB * 1024 * 1024;
        
        private int maxBufferSeconds = 30;
        private int frameRate = 15; // Default frame rate for capacity calculation
        private long currentMemoryUsageBytes = 0;

        /// <summary>
        /// Gets the current number of frames in the buffer.
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return buffer.Count;
                }
            }
        }

        /// <summary>
        /// Gets the current memory usage in megabytes.
        /// </summary>
        public float MemoryUsageMB
        {
            get
            {
                lock (lockObject)
                {
                    return currentMemoryUsageBytes / (1024f * 1024f);
                }
            }
        }

        /// <summary>
        /// Gets the maximum capacity based on time limit (30 seconds of frames).
        /// </summary>
        private int MaxCapacity => maxBufferSeconds * frameRate;

        /// <summary>
        /// Initializes a new instance of the FrameBuffer class.
        /// </summary>
        /// <param name="frameRate">Expected frame rate for capacity calculation</param>
        /// <param name="maxBufferSeconds">Maximum buffer duration in seconds (default: 30)</param>
        public FrameBuffer(int frameRate = 15, int maxBufferSeconds = 30)
        {
            this.frameRate = frameRate;
            this.maxBufferSeconds = maxBufferSeconds;
            this.buffer = new Queue<EncodedFrame>(MaxCapacity);
        }

        /// <summary>
        /// Adds an encoded frame to the buffer.
        /// Automatically drops oldest frames if buffer is full (time or memory limit).
        /// Thread-safe operation.
        /// </summary>
        /// <param name="frame">The encoded frame to add</param>
        public void Enqueue(EncodedFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            lock (lockObject)
            {
                // Drop oldest frames if we've exceeded time-based capacity
                while (buffer.Count >= MaxCapacity)
                {
                    DropOldestFrame();
                }

                // Drop oldest frames if adding this frame would exceed memory limit
                while (currentMemoryUsageBytes + frame.SizeBytes > MAX_BUFFER_SIZE_BYTES && buffer.Count > 0)
                {
                    DropOldestFrame();
                }

                // Add the new frame
                buffer.Enqueue(frame);
                currentMemoryUsageBytes += frame.SizeBytes;
            }
        }

        /// <summary>
        /// Removes and returns the oldest frame from the buffer.
        /// Throws InvalidOperationException if buffer is empty.
        /// Thread-safe operation.
        /// </summary>
        /// <returns>The oldest encoded frame</returns>
        public EncodedFrame Dequeue()
        {
            lock (lockObject)
            {
                if (buffer.Count == 0)
                {
                    throw new InvalidOperationException("Buffer is empty");
                }

                EncodedFrame frame = buffer.Dequeue();
                currentMemoryUsageBytes -= frame.SizeBytes;
                return frame;
            }
        }

        /// <summary>
        /// Attempts to remove and return the oldest frame from the buffer.
        /// Non-blocking operation that returns false if buffer is empty.
        /// Thread-safe operation.
        /// </summary>
        /// <param name="frame">The dequeued frame, or null if buffer is empty</param>
        /// <returns>True if a frame was dequeued, false if buffer is empty</returns>
        public bool TryDequeue(out EncodedFrame frame)
        {
            lock (lockObject)
            {
                if (buffer.Count == 0)
                {
                    frame = null;
                    return false;
                }

                frame = buffer.Dequeue();
                currentMemoryUsageBytes -= frame.SizeBytes;
                return true;
            }
        }

        /// <summary>
        /// Removes all frames from the buffer and resets memory tracking.
        /// Thread-safe operation.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                buffer.Clear();
                currentMemoryUsageBytes = 0;
            }
        }

        /// <summary>
        /// Updates the frame rate for capacity calculation.
        /// Does not affect existing buffered frames.
        /// </summary>
        /// <param name="newFrameRate">The new frame rate</param>
        public void SetFrameRate(int newFrameRate)
        {
            lock (lockObject)
            {
                frameRate = newFrameRate;
            }
        }

        /// <summary>
        /// Internal method to drop the oldest frame from the buffer.
        /// Must be called within a lock.
        /// </summary>
        private void DropOldestFrame()
        {
            if (buffer.Count > 0)
            {
                EncodedFrame droppedFrame = buffer.Dequeue();
                currentMemoryUsageBytes -= droppedFrame.SizeBytes;
            }
        }
    }
}
