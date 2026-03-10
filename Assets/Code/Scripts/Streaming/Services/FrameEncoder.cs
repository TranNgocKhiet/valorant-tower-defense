using System;
using System.Text;
using System.Threading.Tasks;
using TowerDefense.Streaming.Models;
using UnityEngine;
using Newtonsoft.Json;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Encodes gameplay frames for transmission by compressing visual data,
    /// encoding to base64, and serializing to JSON format.
    /// </summary>
    public class FrameEncoder
    {
        private int compressionQuality = 75; // Default JPEG quality (0-100)
        private const int FORMAT_VERSION = 1;

        /// <summary>
        /// Sets the JPEG compression quality for visual data encoding.
        /// </summary>
        /// <param name="quality">Quality value from 0 (lowest) to 100 (highest)</param>
        public void SetCompressionQuality(int quality)
        {
            compressionQuality = Mathf.Clamp(quality, 0, 100);
        }

        /// <summary>
        /// Asynchronously encodes a gameplay frame by compressing visual data,
        /// encoding to base64, and preparing for transmission.
        /// </summary>
        /// <param name="frame">The gameplay frame to encode</param>
        /// <returns>An encoded frame ready for transmission</returns>
        public async Task<EncodedFrame> EncodeAsync(GameplayFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            if (frame.VisualData == null || frame.VisualData.Length == 0)
            {
                throw new ArgumentException("Frame visual data cannot be null or empty", nameof(frame));
            }

            // Perform encoding on background thread to avoid blocking game loop
            return await Task.Run(() => EncodeFrame(frame));
        }

        /// <summary>
        /// Internal method that performs the actual encoding work.
        /// Runs on a background thread.
        /// </summary>
        private EncodedFrame EncodeFrame(GameplayFrame frame)
        {
            // Compress visual data to JPEG format
            byte[] compressedData = CompressToJPEG(frame.VisualData);

            // Encode compressed data to base64 string
            string base64Data = Convert.ToBase64String(compressedData);

            // Create encoded frame with all required fields
            var encodedFrame = new EncodedFrame
            {
                SequenceNumber = frame.SequenceNumber,
                Timestamp = frame.Timestamp.ToString("o"), // ISO 8601 format
                VisualDataBase64 = base64Data,
                Metadata = frame.Metadata,
                FormatVersion = FORMAT_VERSION,
                SizeBytes = CalculateEncodedSize(base64Data, frame.Metadata)
            };

            return encodedFrame;
        }

        /// <summary>
        /// Processes JPEG-compressed image data.
        /// The visual data from GameplayFrame is already JPEG-encoded by FrameCaptureService,
        /// so this method validates and passes through the compressed data.
        /// </summary>
        /// <param name="compressedData">Already JPEG-compressed image data</param>
        /// <returns>The JPEG-compressed data</returns>
        private byte[] CompressToJPEG(byte[] compressedData)
        {
            // The FrameCaptureService already compresses frames to JPEG format
            // using Unity's Texture2D.EncodeToJPG() with the configured quality.
            // This method validates the data and passes it through.
            
            if (compressedData == null || compressedData.Length == 0)
            {
                throw new ArgumentException("Compressed data cannot be null or empty");
            }

            // The compressionQuality setting in FrameEncoder is intended for
            // coordination with FrameCaptureService. In a full implementation,
            // this setting would be passed to FrameCaptureService during initialization.
            
            return compressedData;
        }

        /// <summary>
        /// Calculates the total size of the encoded frame in bytes.
        /// Includes base64 string size and JSON metadata overhead.
        /// </summary>
        private int CalculateEncodedSize(string base64Data, GameStateMetadata metadata)
        {
            // Calculate base64 string size in bytes (UTF-8 encoding)
            int base64Size = Encoding.UTF8.GetByteCount(base64Data);

            // Estimate metadata JSON size
            string metadataJson = JsonConvert.SerializeObject(metadata);
            int metadataSize = Encoding.UTF8.GetByteCount(metadataJson);

            // Add overhead for JSON structure (field names, format version, etc.)
            // Approximate overhead: ~200 bytes for field names and JSON syntax
            int overhead = 200;

            return base64Size + metadataSize + overhead;
        }
    }
}
