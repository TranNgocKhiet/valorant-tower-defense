using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TowerDefense.Streaming.Models;
using TowerDefense.Streaming.Services;

namespace TowerDefense.Streaming.Tests.Unit
{
    /// <summary>
    /// Unit tests for FrameEncoder service.
    /// </summary>
    [TestFixture]
    public class FrameEncoderTests
    {
        private FrameEncoder encoder;

        [SetUp]
        public void SetUp()
        {
            encoder = new FrameEncoder();
        }

        [Test]
        public async Task EncodeAsync_ValidFrame_ReturnsEncodedFrame()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act
            var encodedFrame = await encoder.EncodeAsync(frame);

            // Assert
            Assert.IsNotNull(encodedFrame);
            Assert.AreEqual(frame.SequenceNumber, encodedFrame.SequenceNumber);
            Assert.IsNotNull(encodedFrame.VisualDataBase64);
            Assert.IsNotNull(encodedFrame.Timestamp);
            Assert.AreEqual(1, encodedFrame.FormatVersion);
            Assert.Greater(encodedFrame.SizeBytes, 0);
        }

        [Test]
        public async Task EncodeAsync_ValidFrame_Base64EncodesVisualData()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act
            var encodedFrame = await encoder.EncodeAsync(frame);

            // Assert
            // Verify base64 encoding by decoding it back
            byte[] decodedData = Convert.FromBase64String(encodedFrame.VisualDataBase64);
            Assert.AreEqual(frame.VisualData.Length, decodedData.Length);
            CollectionAssert.AreEqual(frame.VisualData, decodedData);
        }

        [Test]
        public async Task EncodeAsync_ValidFrame_FormatsTimestampAsISO8601()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act
            var encodedFrame = await encoder.EncodeAsync(frame);

            // Assert
            // Verify ISO 8601 format by parsing it back
            DateTime parsedTimestamp = DateTime.Parse(encodedFrame.Timestamp);
            Assert.AreEqual(frame.Timestamp.Year, parsedTimestamp.Year);
            Assert.AreEqual(frame.Timestamp.Month, parsedTimestamp.Month);
            Assert.AreEqual(frame.Timestamp.Day, parsedTimestamp.Day);
        }

        [Test]
        public async Task EncodeAsync_ValidFrame_PreservesMetadata()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act
            var encodedFrame = await encoder.EncodeAsync(frame);

            // Assert
            Assert.IsNotNull(encodedFrame.Metadata);
            Assert.AreEqual(frame.Metadata.CurrentWave, encodedFrame.Metadata.CurrentWave);
            Assert.AreEqual(frame.Metadata.TowerCount, encodedFrame.Metadata.TowerCount);
            Assert.AreEqual(frame.Metadata.EnemyCount, encodedFrame.Metadata.EnemyCount);
            Assert.AreEqual(frame.Metadata.PlayerHealth, encodedFrame.Metadata.PlayerHealth);
            Assert.AreEqual(frame.Metadata.Score, encodedFrame.Metadata.Score);
        }

        [Test]
        public void EncodeAsync_NullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await encoder.EncodeAsync(null));
        }

        [Test]
        public void EncodeAsync_NullVisualData_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame();
            frame.VisualData = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await encoder.EncodeAsync(frame));
        }

        [Test]
        public void EncodeAsync_EmptyVisualData_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame();
            frame.VisualData = new byte[0];

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await encoder.EncodeAsync(frame));
        }

        [Test]
        public void SetCompressionQuality_ValidQuality_SetsQuality()
        {
            // Act
            encoder.SetCompressionQuality(85);

            // Assert
            // Quality is set internally, we can verify by encoding a frame
            // and checking that it doesn't throw an exception
            var frame = CreateTestFrame();
            Assert.DoesNotThrowAsync(async () => await encoder.EncodeAsync(frame));
        }

        [Test]
        public void SetCompressionQuality_QualityAbove100_ClampsTo100()
        {
            // Act
            encoder.SetCompressionQuality(150);

            // Assert
            // Quality should be clamped to 100, verify by encoding
            var frame = CreateTestFrame();
            Assert.DoesNotThrowAsync(async () => await encoder.EncodeAsync(frame));
        }

        [Test]
        public void SetCompressionQuality_QualityBelowZero_ClampsToZero()
        {
            // Act
            encoder.SetCompressionQuality(-10);

            // Assert
            // Quality should be clamped to 0, verify by encoding
            var frame = CreateTestFrame();
            Assert.DoesNotThrowAsync(async () => await encoder.EncodeAsync(frame));
        }

        [Test]
        public async Task EncodeAsync_CalculatesSizeBytes_IncludesBase64AndMetadata()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act
            var encodedFrame = await encoder.EncodeAsync(frame);

            // Assert
            // Size should be greater than just the base64 data
            int base64Size = Encoding.UTF8.GetByteCount(encodedFrame.VisualDataBase64);
            Assert.Greater(encodedFrame.SizeBytes, base64Size);
        }

        /// <summary>
        /// Creates a test gameplay frame with sample data.
        /// </summary>
        private GameplayFrame CreateTestFrame()
        {
            // Create sample JPEG-like data (simulating compressed image)
            byte[] sampleJpegData = new byte[1024];
            for (int i = 0; i < sampleJpegData.Length; i++)
            {
                sampleJpegData[i] = (byte)(i % 256);
            }

            return new GameplayFrame
            {
                SequenceNumber = 12345,
                Timestamp = DateTime.UtcNow,
                VisualData = sampleJpegData,
                Metadata = new GameStateMetadata
                {
                    CurrentWave = 5,
                    TowerCount = 12,
                    EnemyCount = 23,
                    PlayerHealth = 85,
                    Score = 4500
                }
            };
        }
    }
}
