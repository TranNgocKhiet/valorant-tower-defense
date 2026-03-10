using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Models;
using UnityEngine;
using QualityLevel = TowerDefense.Streaming.Core.QualityLevel;

namespace TowerDefense.Streaming.Interfaces
{
    /// <summary>
    /// Interface for capturing gameplay frames from the Unity rendering pipeline.
    /// </summary>
    public interface IFrameCaptureService
    {
        /// <summary>
        /// Captures a single gameplay frame with visual data and metadata.
        /// </summary>
        /// <returns>A GameplayFrame containing visual data and game state metadata.</returns>
        GameplayFrame CaptureFrame();
        
        /// <summary>
        /// Sets the frame capture rate.
        /// </summary>
        /// <param name="fps">Frames per second (1-30).</param>
        void SetFrameRate(int fps);
        
        /// <summary>
        /// Sets the quality level for frame capture, affecting resolution.
        /// </summary>
        /// <param name="quality">Quality level (Low, Medium, High).</param>
        void SetQuality(QualityLevel quality);
        
        /// <summary>
        /// Updates the target camera for frame capture.
        /// Useful when the camera changes during scene transitions.
        /// </summary>
        /// <param name="camera">The new camera to capture from.</param>
        void SetCamera(Camera camera);
    }
}
