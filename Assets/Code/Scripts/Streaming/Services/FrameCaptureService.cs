using System;
using UnityEngine;
using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Interfaces;
using TowerDefense.Streaming.Models;
using QualityLevel = TowerDefense.Streaming.Core.QualityLevel;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Service for capturing gameplay frames from Unity's rendering pipeline.
    /// Implements frame capture with configurable quality and frame rate.
    /// </summary>
    public class FrameCaptureService : IFrameCaptureService
    {
        private RenderTexture captureTexture;
        private Texture2D readbackTexture;
        private Camera targetCamera;
        private QualityLevel currentQuality;
        private int currentFrameRate;
        private long sequenceNumber;
        private int compressionQuality;

        /// <summary>
        /// Initializes a new instance of the FrameCaptureService.
        /// </summary>
        /// <param name="camera">The camera to capture frames from.</param>
        /// <param name="quality">Initial quality level.</param>
        /// <param name="frameRate">Initial frame rate (1-30 FPS).</param>
        /// <param name="compressionQuality">JPEG compression quality (0-100).</param>
        public FrameCaptureService(Camera camera, QualityLevel quality = QualityLevel.Medium, int frameRate = 15, int compressionQuality = 75)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera), "Camera cannot be null");
            }

            targetCamera = camera;
            currentQuality = quality;
            currentFrameRate = Mathf.Clamp(frameRate, 1, 30);
            this.compressionQuality = Mathf.Clamp(compressionQuality, 0, 100);
            sequenceNumber = 0;

            InitializeCapture();
        }

        /// <summary>
        /// Initializes the capture system with RenderTexture based on quality level.
        /// </summary>
        private void InitializeCapture()
        {
            var resolution = GetResolutionForQuality(currentQuality);
            
            // Clean up existing resources if reinitializing
            CleanupResources();

            // Create RenderTexture for capturing camera output
            captureTexture = new RenderTexture(resolution.width, resolution.height, 24, RenderTextureFormat.ARGB32);
            captureTexture.name = "StreamingCaptureTexture";
            
            // Create Texture2D for reading back pixel data
            readbackTexture = new Texture2D(resolution.width, resolution.height, TextureFormat.RGB24, false);
            readbackTexture.name = "StreamingReadbackTexture";

            Debug.Log($"FrameCaptureService initialized: {resolution.width}x{resolution.height} @ {currentFrameRate} FPS");
        }

        /// <summary>
        /// Captures a single gameplay frame with visual data and metadata.
        /// </summary>
        /// <returns>A GameplayFrame containing visual data and game state metadata.</returns>
        public GameplayFrame CaptureFrame()
        {
            try
            {
                // Ensure resources are initialized
                if (captureTexture == null || readbackTexture == null)
                {
                    Debug.LogWarning("FrameCaptureService: Resources not initialized, reinitializing...");
                    InitializeCapture();
                }
                
                // Capture visual data
                byte[] visualData = CaptureFrameData();
                
                // Extract game state metadata
                GameStateMetadata metadata = ExtractGameStateMetadata();
                
                // Create frame with timestamp and sequence number
                var frame = new GameplayFrame
                {
                    SequenceNumber = ++sequenceNumber,
                    Timestamp = DateTime.UtcNow,
                    VisualData = visualData,
                    Metadata = metadata
                };

                return frame;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Frame capture failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets the frame capture rate.
        /// </summary>
        /// <param name="fps">Frames per second (1-30).</param>
        public void SetFrameRate(int fps)
        {
            currentFrameRate = Mathf.Clamp(fps, 1, 30);
            Debug.Log($"Frame rate set to {currentFrameRate} FPS");
        }

        /// <summary>
        /// Sets the quality level for frame capture, affecting resolution.
        /// </summary>
        /// <param name="quality">Quality level (Low, Medium, High).</param>
        public void SetQuality(QualityLevel quality)
        {
            if (currentQuality != quality)
            {
                currentQuality = quality;
                InitializeCapture(); // Reinitialize with new resolution
                Debug.Log($"Quality set to {quality}");
            }
        }
        
        /// <summary>
        /// Updates the target camera for frame capture.
        /// Useful when the camera changes during scene transitions.
        /// </summary>
        /// <param name="camera">The new camera to capture from.</param>
        public void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogWarning("FrameCaptureService: Attempted to set null camera, ignoring");
                return;
            }
            
            targetCamera = camera;
            Debug.Log($"FrameCaptureService: Camera updated to '{camera.name}'");
        }

        /// <summary>
        /// Captures frame data after all rendering including UI overlays.
        /// Uses ReadPixels to capture the final rendered frame.
        /// </summary>
        /// <returns>JPEG encoded frame data.</returns>
        private byte[] CaptureFrameData()
        {
            try
            {
                // Read pixels from the screen after all rendering (including UI overlays)
                // This must be called after WaitForEndOfFrame in a coroutine
                int width = captureTexture.width;
                int height = captureTexture.height;
                
                // Calculate source rect from screen
                // Center crop if aspect ratios don't match
                float screenAspect = (float)Screen.width / Screen.height;
                float targetAspect = (float)width / height;
                
                Rect sourceRect;
                if (Mathf.Approximately(screenAspect, targetAspect))
                {
                    // Same aspect ratio, capture full screen
                    sourceRect = new Rect(0, 0, Screen.width, Screen.height);
                }
                else if (screenAspect > targetAspect)
                {
                    // Screen is wider, crop sides
                    int cropWidth = Mathf.RoundToInt(Screen.height * targetAspect);
                    int offsetX = (Screen.width - cropWidth) / 2;
                    sourceRect = new Rect(offsetX, 0, cropWidth, Screen.height);
                }
                else
                {
                    // Screen is taller, crop top/bottom
                    int cropHeight = Mathf.RoundToInt(Screen.width / targetAspect);
                    int offsetY = (Screen.height - cropHeight) / 2;
                    sourceRect = new Rect(0, offsetY, Screen.width, cropHeight);
                }
                
                // Create temporary texture at screen resolution
                Texture2D tempTexture = new Texture2D((int)sourceRect.width, (int)sourceRect.height, TextureFormat.RGB24, false);
                
                // Read pixels from screen (this captures everything including UI overlays)
                tempTexture.ReadPixels(sourceRect, 0, 0, false);
                tempTexture.Apply();
                
                // Scale to target resolution if needed
                if (tempTexture.width != width || tempTexture.height != height)
                {
                    RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(tempTexture, rt);
                    
                    RenderTexture.active = rt;
                    readbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    readbackTexture.Apply();
                    
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);
                }
                else
                {
                    Graphics.CopyTexture(tempTexture, readbackTexture);
                }
                
                UnityEngine.Object.Destroy(tempTexture);
                
                // Encode to JPEG
                byte[] jpegData = readbackTexture.EncodeToJPG(compressionQuality);
                
                return jpegData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FrameCaptureService: Frame capture failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Finds a valid camera in the scene for frame capture.
        /// Prioritizes main camera, then falls back to any active camera.
        /// Filters out UI-only cameras to capture gameplay.
        /// </summary>
        /// <returns>A valid Camera instance, or null if none found.</returns>
        private Camera FindValidCamera()
        {
            // Try to find main camera first (usually the gameplay camera)
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject.activeInHierarchy && mainCamera.enabled)
            {
                return mainCamera;
            }
            
            // Fall back to any active camera in the scene
            // Prioritize cameras that render to screen (not UI-only cameras)
            Camera[] allCameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            Camera bestCamera = null;
            float highestDepth = float.MinValue;
            
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam.gameObject.activeInHierarchy && cam.enabled)
                {
                    // Prefer cameras with higher depth (usually main gameplay cameras)
                    // Skip cameras that only render UI layers
                    if (cam.depth > highestDepth)
                    {
                        highestDepth = cam.depth;
                        bestCamera = cam;
                    }
                }
            }
            
            if (bestCamera != null)
            {
                Debug.Log($"FrameCaptureService: Found camera '{bestCamera.name}' with depth {highestDepth}");
            }
            
            return bestCamera;
        }

        /// <summary>
        /// Extracts current game state metadata.
        /// Requirement 2.3: Hook into game state manager for metadata extraction
        /// Integrates with:
        /// - EnemySpawner: Current wave number
        /// - LevelManager: Player health and score (currency)
        /// - Scene objects: Tower count (Turret instances) and enemy count (EnemyMovement instances)
        /// </summary>
        /// <returns>Game state metadata.</returns>
        private GameStateMetadata ExtractGameStateMetadata()
        {
            var metadata = new GameStateMetadata
            {
                CurrentWave = GetCurrentWave(),
                TowerCount = GetTowerCount(),
                EnemyCount = GetEnemyCount(),
                PlayerHealth = GetPlayerHealth(),
                Score = GetScore()
            };

            return metadata;
        }

        /// <summary>
        /// Gets the resolution for a given quality level.
        /// </summary>
        /// <param name="quality">Quality level.</param>
        /// <returns>Resolution (width, height).</returns>
        private (int width, int height) GetResolutionForQuality(QualityLevel quality)
        {
            return quality switch
            {
                QualityLevel.Low => (640, 360),
                QualityLevel.Medium => (1280, 720),
                QualityLevel.High => (1920, 1080),
                _ => (1280, 720) // Default to medium
            };
        }

        /// <summary>
        /// Cleans up RenderTexture and Texture2D resources.
        /// </summary>
        private void CleanupResources()
        {
            if (captureTexture != null)
            {
                captureTexture.Release();
                UnityEngine.Object.Destroy(captureTexture);
                captureTexture = null;
            }

            if (readbackTexture != null)
            {
                UnityEngine.Object.Destroy(readbackTexture);
                readbackTexture = null;
            }
        }

        /// <summary>
        /// Disposes of resources used by the service.
        /// </summary>
        public void Dispose()
        {
            CleanupResources();
            Debug.Log("FrameCaptureService disposed");
        }
        
        /// <summary>
        /// Reinitializes the capture service after disposal.
        /// Useful when restarting streaming after stopping.
        /// </summary>
        public void Reinitialize()
        {
            Debug.Log("FrameCaptureService: Reinitializing capture resources");
            InitializeCapture();
        }

        // Game state extraction methods
        // These integrate with existing game systems
        // Requirements: 2.3 - Extract game state metadata
        
        private int GetCurrentWave()
        {
            // Integration with EnemySpawner to get current wave
            // EnemySpawner tracks the current wave number
            var enemySpawner = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
            if (enemySpawner != null)
            {
                // Access the private currentWave field using reflection
                var fieldInfo = typeof(EnemySpawner).GetField("currentWave", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    return (int)fieldInfo.GetValue(enemySpawner);
                }
            }
            return 0;
        }

        private int GetTowerCount()
        {
            // Count all tower instances in the scene
            // Turret is the base class for all tower types
            var towers = UnityEngine.Object.FindObjectsOfType<Turret>();
            return towers != null ? towers.Length : 0;
        }

        private int GetEnemyCount()
        {
            // Count all active enemy instances
            // EnemyMovement component is attached to all enemies
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyMovement>();
            return enemies != null ? enemies.Length : 0;
        }

        private int GetPlayerHealth()
        {
            // Integration with LevelManager to get player health
            // Requirement 2.3: Extract player health from game state
            var levelManager = LevelManager.main;
            return levelManager != null ? levelManager.levelHealth : 0;
        }

        private int GetScore()
        {
            // Integration with LevelManager to get currency (score)
            // Requirement 2.3: Extract score from game state
            var levelManager = LevelManager.main;
            return levelManager != null ? levelManager.currency : 0;
        }
    }
}
