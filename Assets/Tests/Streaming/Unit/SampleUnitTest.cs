using NUnit.Framework;
using TowerDefense.Streaming.Models;
using TowerDefense.Streaming.Core;

namespace TowerDefense.Streaming.Tests.Unit
{
    /// <summary>
    /// Sample unit test demonstrating NUnit setup.
    /// This file can be removed once actual unit tests are implemented.
    /// </summary>
    [TestFixture]
    public class SampleUnitTest
    {
        [Test]
        public void StreamingConfig_Default_HasExpectedValues()
        {
            // Arrange & Act
            var config = StreamingConfig.Default();
            
            // Assert
            Assert.AreEqual(15, config.FrameRate);
            Assert.AreEqual(QualityLevel.Medium, config.Quality);
            Assert.AreEqual(75, config.CompressionQuality);
            Assert.AreEqual(30, config.MaxBufferSeconds);
            Assert.AreEqual(100, config.MaxBufferSizeMB);
            Assert.AreEqual(5, config.ReconnectIntervalSeconds);
            Assert.AreEqual(60, config.MaxReconnectDurationSeconds);
            Assert.AreEqual(3, config.MaxRetryAttempts);
        }
        
        [Test]
        public void GameStateMetadata_CanBeInstantiated()
        {
            // Arrange & Act
            var metadata = new GameStateMetadata
            {
                CurrentWave = 5,
                TowerCount = 10,
                EnemyCount = 20,
                PlayerHealth = 85,
                Score = 1000
            };
            
            // Assert
            Assert.AreEqual(5, metadata.CurrentWave);
            Assert.AreEqual(10, metadata.TowerCount);
            Assert.AreEqual(20, metadata.EnemyCount);
            Assert.AreEqual(85, metadata.PlayerHealth);
            Assert.AreEqual(1000, metadata.Score);
        }
    }
}
