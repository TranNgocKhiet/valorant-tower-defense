using System;
using System.Collections;
using UnityEngine;
using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Models;
using TowerDefense.Streaming.Services;

namespace TowerDefense.Streaming
{
    /// <summary>
    /// Orchestrates the entire streaming lifecycle, managing state transitions,
    /// frame capture, encoding, buffering, and transmission.
    /// Requirements: 1.1, 1.5, 5.1, 5.2, 5.3, 6.1
    /// </summary>
    public class StreamManager : MonoBehaviour
    {
        #region Events
        
        /// <summary>Fired when the connection state changes.</summary>
        public event Action<ConnectionState> OnConnectionStateChanged;
        
        /// <summary>Fired when an error occurs during streaming.</summary>
        public event Action<string> OnError;
        
        /// <summary>Fired when streaming statistics are updated.</summary>
        public event Action<StreamingStats> OnStatsUpdated;
        
        #endregion
        
        #region Private Fields
        
        private ConnectionState _connectionState = ConnectionState.Idle;
        private StreamingConfig _config;
        private StreamingStats _stats;
        private DateTime _sessionStartTime;
        private string _sessionId;
        private string _streamDomain;
        
        // Service dependencies
        private ConfigManager _configManager;
        private AuthManager _authManager;
        private FrameCaptureService _frameCaptureService;
        private FrameEncoder _frameEncoder;
        private FrameBuffer _frameBuffer;
        private TransmissionService _transmissionService;
        
        // Singleton instance
        private static StreamManager _instance;
        
        // Track if application is quitting
        private bool _isQuitting = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern: Only allow one StreamManager instance
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("StreamManager: Duplicate instance detected, destroying this one");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Make this GameObject persist across scene loads
            // This allows streaming to continue when navigating between scenes
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("StreamManager: Initializing singleton instance");
            
            InitializeServices();
            LoadConfiguration();
            InitializeStats();
            
            // Requirement 5.5: Hook into game exit event for automatic termination
            Application.quitting += OnApplicationQuitting;
        }
        
        /// <summary>
        /// Gets or creates the singleton instance of StreamManager.
        /// Call this to ensure StreamManager exists before using it.
        /// </summary>
        public static StreamManager GetInstance()
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindObjectOfType<StreamManager>();
                
                if (_instance == null)
                {
                    // Create new instance
                    Debug.Log("StreamManager: No instance found, creating new one");
                    GameObject go = new GameObject("StreamManager");
                    _instance = go.AddComponent<StreamManager>();
                }
                else
                {
                    Debug.Log("StreamManager: Found existing instance");
                }
            }
            
            return _instance;
        }
        
        private void OnDestroy()
        {
            Debug.Log($"StreamManager: OnDestroy called, _isQuitting: {_isQuitting}, state: {_connectionState}");
            
            // Clear singleton reference
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Unhook from application quit event
            Application.quitting -= OnApplicationQuitting;
            
            // Only perform cleanup if we're actually quitting
            // With DontDestroyOnLoad, OnDestroy should only be called when quitting
            if (_isQuitting)
            {
                Debug.Log("StreamManager: Application is quitting, performing full cleanup");
                
                if (_connectionState == ConnectionState.Streaming || 
                    _connectionState == ConnectionState.Connected ||
                    _connectionState == ConnectionState.Reconnecting)
                {
                    // Can't use coroutines during quit
                    StopAllCoroutines();
                    CleanupResources();
                    _connectionState = ConnectionState.Idle;
                }
                
                // Dispose frame capture service
                if (_frameCaptureService != null)
                {
                    Debug.Log("StreamManager: Disposing FrameCaptureService");
                    _frameCaptureService.Dispose();
                    _frameCaptureService = null;
                }
            }
            else
            {
                Debug.LogWarning("StreamManager: OnDestroy called but not quitting - this shouldn't happen with DontDestroyOnLoad!");
            }
        }
        
        /// <summary>
        /// Called when the application is quitting.
        /// Requirement 5.5: Automatic termination on game exit
        /// </summary>
        private void OnApplicationQuitting()
        {
            Debug.Log("StreamManager: Application quitting, terminating streaming session");
            _isQuitting = true;
            
            if (_connectionState == ConnectionState.Streaming || 
                _connectionState == ConnectionState.Connected ||
                _connectionState == ConnectionState.Reconnecting)
            {
                // Synchronously terminate the session since we're quitting
                // We can't use coroutines during application quit
                StopAllCoroutines();
                CleanupResources();
                _connectionState = ConnectionState.Idle;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initiates the streaming session.
        /// Requirement 1.1: Start streaming and establish connection
        /// Can be called directly from UI buttons or through UIController.
        /// </summary>
        public void StartStreaming()
        {
            Debug.Log($"StreamManager: StartStreaming called directly on instance, current state: {_connectionState}, GameObject active: {gameObject.activeInHierarchy}");
            
            // If this is not the singleton instance, redirect to the singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("StreamManager: This is not the singleton instance, redirecting to singleton");
                _instance.StartStreaming();
                return;
            }
            
            if (_connectionState != ConnectionState.Idle && _connectionState != ConnectionState.Error)
            {
                Debug.LogWarning($"StreamManager: Cannot start streaming - already active or in progress (state: {_connectionState})");
                return;
            }
            
            // Ensure services are initialized (in case they were disposed)
            EnsureServicesInitialized();
            
            // Validate configuration before starting
            if (!_configManager.ValidateConfig(_config))
            {
                Debug.LogError("StreamManager: Configuration validation failed");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke("Invalid streaming configuration");
                return;
            }
            
            // Check if GameObject is active before starting coroutine
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError("StreamManager: GameObject is not active, cannot start streaming");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke("StreamManager is not active");
                return;
            }
            
            Debug.Log("StreamManager: Configuration validated, transitioning to Connecting state");
            TransitionToState(ConnectionState.Connecting);
            _sessionStartTime = DateTime.UtcNow;
            ResetStats();
            
            // Start connection establishment coroutine
            try
            {
                Debug.Log("StreamManager: Starting EstablishConnectionCoroutine");
                StartCoroutine(EstablishConnectionCoroutine());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"StreamManager: Failed to start EstablishConnectionCoroutine: {ex.Message}");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke($"Failed to start streaming: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Static method to start streaming on the singleton instance.
        /// Use this when calling from UI buttons directly.
        /// </summary>
        public static void StartStreamingStatic()
        {
            Debug.Log("StreamManager: StartStreamingStatic called");
            StreamManager instance = GetInstance();
            if (instance != null)
            {
                instance.StartStreaming();
            }
            else
            {
                Debug.LogError("StreamManager: Failed to get instance for StartStreamingStatic");
            }
        }

        
        /// <summary>
        /// Terminates the streaming session.
        /// Requirements 5.1, 5.2, 5.3: Stop streaming and cleanup
        /// Can be called directly from UI buttons or through UIController.
        /// </summary>
        public void StopStreaming()
        {
            Debug.Log($"StreamManager: StopStreaming called directly on instance, current state: {_connectionState}, GameObject active: {gameObject.activeInHierarchy}");
            
            // If this is not the singleton instance, redirect to the singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("StreamManager: This is not the singleton instance, redirecting to singleton");
                _instance.StopStreaming();
                return;
            }
            
            if (_connectionState == ConnectionState.Idle)
            {
                Debug.LogWarning("StreamManager: Cannot stop streaming - already idle");
                return;
            }
            
            if (_connectionState == ConnectionState.Terminating)
            {
                Debug.LogWarning("StreamManager: Already terminating");
                return;
            }

            Debug.Log("StreamManager: Transitioning to Terminating state");
            TransitionToState(ConnectionState.Terminating);

            // Check if GameObject is active before starting coroutine
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("StreamManager: GameObject is not active, performing synchronous cleanup");
                CleanupResources();
                TransitionToState(ConnectionState.Idle);
                return;
            }

            // Requirement 5.1, 5.2: Send session termination message and close connection
            try
            {
                Debug.Log("StreamManager: Starting TerminateSessionCoroutine");
                StartCoroutine(TerminateSessionCoroutine());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"StreamManager: Failed to start TerminateSessionCoroutine: {ex.Message}");
                // Fallback to synchronous cleanup
                Debug.Log("StreamManager: Performing synchronous cleanup as fallback");
                CleanupResources();
                TransitionToState(ConnectionState.Idle);
            }
        }
        
        /// <summary>
        /// Static method to stop streaming on the singleton instance.
        /// Use this when calling from UI buttons directly.
        /// </summary>
        public static void StopStreamingStatic()
        {
            Debug.Log("StreamManager: StopStreamingStatic called");
            StreamManager instance = GetInstance();
            if (instance != null)
            {
                instance.StopStreaming();
            }
            else
            {
                Debug.LogError("StreamManager: Failed to get instance for StopStreamingStatic");
            }
        }

        
        /// <summary>
        /// Gets the current connection state.
        /// Requirement 1.5: Expose connection state
        /// </summary>
        public ConnectionState GetConnectionState()
        {
            Debug.Log($"StreamManager: GetConnectionState called, returning: {_connectionState}");
            return _connectionState;
        }
        
        /// <summary>
        /// Checks if the StreamManager is properly initialized and ready.
        /// </summary>
        public bool IsReady()
        {
            bool ready = _configManager != null && 
                        _authManager != null && 
                        _frameCaptureService != null && 
                        _frameEncoder != null && 
                        _frameBuffer != null && 
                        _transmissionService != null;
            
            Debug.Log($"StreamManager: IsReady check - {ready}");
            if (!ready)
            {
                Debug.LogWarning($"StreamManager: Not ready - ConfigManager: {_configManager != null}, " +
                               $"AuthManager: {_authManager != null}, " +
                               $"FrameCaptureService: {_frameCaptureService != null}, " +
                               $"FrameEncoder: {_frameEncoder != null}, " +
                               $"FrameBuffer: {_frameBuffer != null}, " +
                               $"TransmissionService: {_transmissionService != null}");
            }
            
            return ready;
        }
        
        /// <summary>
        /// Gets the current streaming statistics.
        /// Requirement 6.1: Expose streaming stats
        /// </summary>
        public StreamingStats GetStats()
        {
            UpdateSessionDuration();
            return _stats;
        }
        
        /// <summary>
        /// Updates the streaming configuration at runtime.
        /// Requirement 1.5: Support runtime configuration changes
        /// </summary>
        public void UpdateConfiguration(StreamingConfig config)
        {
            if (config == null)
            {
                Debug.LogError("Cannot update configuration: config is null");
                return;
            }
            
            if (!_configManager.ValidateConfig(config))
            {
                Debug.LogError("Cannot update configuration: validation failed");
                OnError?.Invoke("Invalid configuration provided");
                return;
            }
            
            _config = config;
            _configManager.SaveConfig(config);
            
            // Apply configuration changes to active services
            if (_frameCaptureService != null)
            {
                _frameCaptureService.SetFrameRate(config.FrameRate);
                _frameCaptureService.SetQuality(config.Quality);
            }
            
            if (_frameEncoder != null)
            {
                _frameEncoder.SetCompressionQuality(config.CompressionQuality);
            }
            
            if (_transmissionService != null)
            {
                _transmissionService.SetEndpoint(config.ApiEndpointUrl);
            }
            
            Debug.Log($"StreamManager: Configuration updated - FrameRate: {config.FrameRate}, Quality: {config.Quality}");
        }
        
        #endregion
        
        #region State Machine
        
        /// <summary>
        /// Transitions to a new connection state and fires the state changed event.
        /// Requirement 1.5: State machine with all ConnectionState transitions
        /// </summary>
        private void TransitionToState(ConnectionState newState)
        {
            if (_connectionState == newState)
            {
                return;
            }
            
            ConnectionState previousState = _connectionState;
            _connectionState = newState;
            
            Debug.Log($"StreamManager: State transition {previousState} -> {newState}");
            
            OnConnectionStateChanged?.Invoke(newState);
        }
        
        #endregion
        
        #region Token Refresh
        
        /// <summary>
        /// Coroutine that periodically validates and refreshes authentication tokens.
        /// Requirements: 8.4, 8.5
        /// </summary>
        private IEnumerator TokenRefreshCoroutine()
        {
            Debug.Log("StreamManager: Starting token refresh coroutine");
            
            // Requirement 8.4: Periodic token validation every 60 seconds
            const float TOKEN_CHECK_INTERVAL = 60f;
            
            while (_connectionState == ConnectionState.Streaming || 
                   _connectionState == ConnectionState.Reconnecting ||
                   _connectionState == ConnectionState.Connected)
            {
                // Wait for the check interval
                yield return new WaitForSeconds(TOKEN_CHECK_INTERVAL);
                
                // Requirement 8.4: Check if token is still valid
                if (!_authManager.IsTokenValid())
                {
                    Debug.Log("StreamManager: Token expired or expiring soon, attempting refresh");
                    
                    // Requirement 8.4: Automatic token refresh before expiration
                    var refreshTask = _authManager.RefreshTokenAsync();
                    yield return new WaitUntil(() => refreshTask.IsCompleted);
                    
                    // Requirement 8.5: Error handling for token refresh failures
                    if (refreshTask.IsFaulted || !refreshTask.Result)
                    {
                        string errorMsg = "Authentication token refresh failed";
                        Debug.LogError($"StreamManager: {errorMsg}");
                        
                        // Requirement 8.5: Terminate session on token refresh failure
                        OnError?.Invoke(errorMsg);
                        StopStreaming();
                        yield break;
                    }
                    
                    Debug.Log("StreamManager: Token refreshed successfully");
                }
                else
                {
                    Debug.Log("StreamManager: Token is still valid");
                }
            }
            
            Debug.Log("StreamManager: Token refresh coroutine stopped");
        }
        
        #endregion
        
        #region Connection Establishment
        
        /// <summary>
        /// Coroutine that establishes connection to the API endpoint with timeout.
        /// Requirements: 1.1, 1.3, 1.4, 1.5
        /// </summary>
        private System.Collections.IEnumerator EstablishConnectionCoroutine()
        {
            // Requirement 1.4: Validate URL format before connection attempt
            if (!IsValidUrl(_config.ApiEndpointUrl))
            {
                string errorMsg = "Invalid API endpoint URL format";
                Debug.LogError($"StreamManager: {errorMsg}");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke(errorMsg);
                yield break;
            }
            
            // Get authentication token
            var tokenTask = _authManager.GetAuthTokenAsync();
            yield return new WaitUntil(() => tokenTask.IsCompleted);
            
            if (tokenTask.IsFaulted || string.IsNullOrEmpty(tokenTask.Result))
            {
                string errorMsg = "Failed to retrieve authentication token";
                Debug.LogError($"StreamManager: {errorMsg}");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke(errorMsg);
                yield break;
            }
            
            string authToken = tokenTask.Result;
            
            // Create session initialization message
            var initMessage = new SessionInitMessage
            {
                PlayerId = GetPlayerId(),
                GameVersion = Application.version,
                SessionStartTime = _sessionStartTime,
                Config = _config
            };
            
            // Requirement 1.1: Establish connection within 5 seconds
            float timeout = 10;
            float elapsed = 0f;
            bool connectionSucceeded = false;
            
            var initTask = _transmissionService.SendSessionInitAsync(initMessage, authToken);
            
            // Wait for connection with timeout
            while (!initTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Check if connection succeeded
            if (initTask.IsCompleted && !initTask.IsFaulted)
            {
                connectionSucceeded = initTask.Result;
            }
            
            // Requirement 1.3: Handle unreachable endpoint
            if (!connectionSucceeded)
            {
                string errorMsg = elapsed >= timeout 
                    ? "Connection timeout: API endpoint unreachable" 
                    : "Failed to establish connection to API endpoint";
                    
                Debug.LogError($"StreamManager: {errorMsg}");
                TransitionToState(ConnectionState.Error);
                OnError?.Invoke(errorMsg);
                yield break;
            }
            
            // Requirement 1.5: Update connection state to connected
            TransitionToState(ConnectionState.Connected);
            
            // Store session ID from transmission service
            _sessionId = GetSessionIdFromTransmissionService();
            
            Debug.Log($"StreamManager: Connection established successfully. Session ID: {_sessionId}");
            
            // Generate stream domain if not already set
            if (string.IsNullOrEmpty(_streamDomain))
            {
                string username = PlayerPrefs.GetString("Username", "UnknownPlayer");
                _streamDomain = StreamingDataManager.Instance?.GenerateStreamDomain(username) ?? $"stream-{username}-{DateTime.UtcNow.Ticks}";
                Debug.Log($"StreamManager: Generated stream domain: {_streamDomain}");
            }
            
            // Save stream info to DynamoDB
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            StreamingDataManager.Instance?.SaveStreamInfo(_streamDomain, _sessionId, currentLevel);
            
            // Transition to streaming state
            TransitionToState(ConnectionState.Streaming);
            
            // Start frame capture and transmission loop
            StartCoroutine(FrameCaptureAndTransmissionLoop());
            
            // Requirement 8.4: Start token refresh coroutine
            StartCoroutine(TokenRefreshCoroutine());
            
            // Requirement 10.1, 10.2, 10.3, 10.4: Start resource monitoring coroutine
            StartCoroutine(MonitorResourcesCoroutine());
        }

        #endregion
        
        #region Reconnection Logic
        
        /// <summary>
        /// Handles connection loss detection and initiates reconnection.
        /// Requirement 6.1: Detect connection loss and update state
        /// </summary>
        private void HandleConnectionLoss()
        {
            if (_connectionState != ConnectionState.Streaming)
            {
                return;
            }
            
            Debug.LogWarning("StreamManager: Connection lost, initiating reconnection");
            
            // Requirement 6.1: Update connection state to disconnected
            TransitionToState(ConnectionState.Disconnected);
            
            // Start reconnection coroutine
            StartCoroutine(ReconnectCoroutine());
        }
        
        /// <summary>
        /// Coroutine that attempts to reconnect to the API endpoint.
        /// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6
        /// </summary>
        private IEnumerator ReconnectCoroutine()
        {
            // Requirement 6.1: Update state to reconnecting
            TransitionToState(ConnectionState.Reconnecting);
            
            // Requirement 6.2: Maximum reconnection duration of 60 seconds
            float maxReconnectDuration = _config.MaxReconnectDurationSeconds;
            float elapsedTime = 0f;
            
            // Requirement 6.2: Reconnection interval of 5 seconds
            float reconnectInterval = _config.ReconnectIntervalSeconds;
            
            Debug.Log($"StreamManager: Starting reconnection attempts (max duration: {maxReconnectDuration}s, interval: {reconnectInterval}s)");
            
            // Requirement 6.5: Continue buffering frames during reconnection
            // The FrameCaptureAndTransmissionLoop continues to run and buffer frames
            
            while (elapsedTime < maxReconnectDuration)
            {
                Debug.Log($"StreamManager: Reconnection attempt at {elapsedTime:F1}s elapsed");
                
                // Attempt to reconnect
                bool reconnected = false;
                yield return StartCoroutine(AttemptReconnection(result => reconnected = result));
                
                // Requirement 6.3: If reconnection succeeds, resume streaming
                if (reconnected)
                {
                    Debug.Log("StreamManager: Reconnection successful");
                    
                    // Transition back to connected state
                    TransitionToState(ConnectionState.Connected);
                    
                    // Requirement 6.6: Transmit buffered frames before resuming real-time streaming
                    yield return StartCoroutine(TransmitBufferedFrames());
                    
                    // Requirement 6.3: Resume streaming from current game state
                    TransitionToState(ConnectionState.Streaming);
                    
                    Debug.Log("StreamManager: Streaming resumed after reconnection");
                    yield break;
                }
                
                // Wait for next reconnection attempt
                yield return new WaitForSeconds(reconnectInterval);
                elapsedTime += reconnectInterval;
            }
            
            // Requirement 6.4: Reconnection failed after maximum duration
            Debug.LogError($"StreamManager: Reconnection failed after {maxReconnectDuration}s");
            
            // Stop streaming and notify user
            string errorMsg = $"Failed to reconnect after {maxReconnectDuration} seconds";
            OnError?.Invoke(errorMsg);
            
            // Terminate the session
            StopStreaming();
        }
        
        /// <summary>
        /// Attempts a single reconnection to the API endpoint.
        /// </summary>
        private IEnumerator AttemptReconnection(Action<bool> callback)
        {
            // Validate URL format
            if (!IsValidUrl(_config.ApiEndpointUrl))
            {
                Debug.LogError("StreamManager: Invalid API endpoint URL during reconnection");
                callback?.Invoke(false);
                yield break;
            }
            
            // Get authentication token
            var tokenTask = _authManager.GetAuthTokenAsync();
            yield return new WaitUntil(() => tokenTask.IsCompleted);
            
            if (tokenTask.IsFaulted || string.IsNullOrEmpty(tokenTask.Result))
            {
                Debug.LogWarning("StreamManager: Failed to retrieve authentication token during reconnection");
                callback?.Invoke(false);
                yield break;
            }
            
            string authToken = tokenTask.Result;
            
            // Create session initialization message
            var initMessage = new SessionInitMessage
            {
                PlayerId = GetPlayerId(),
                GameVersion = Application.version,
                SessionStartTime = _sessionStartTime,
                Config = _config
            };
            
            // Attempt connection with timeout
            float timeout = 5f;
            float elapsed = 0f;
            bool connectionSucceeded = false;
            
            var initTask = _transmissionService.SendSessionInitAsync(initMessage, authToken);
            
            // Wait for connection with timeout
            while (!initTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Check if connection succeeded
            if (initTask.IsCompleted && !initTask.IsFaulted)
            {
                connectionSucceeded = initTask.Result;
            }
            
            if (connectionSucceeded)
            {
                // Update session ID
                _sessionId = GetSessionIdFromTransmissionService();
                Debug.Log($"StreamManager: Reconnection successful. Session ID: {_sessionId}");
            }
            
            callback?.Invoke(connectionSucceeded);
        }
        
        /// <summary>
        /// Transmits all buffered frames after successful reconnection.
        /// Requirement 6.6: Transmit buffered frames in sequence order
        /// </summary>
        private IEnumerator TransmitBufferedFrames()
        {
            int bufferedFrameCount = _frameBuffer.Count;
            
            if (bufferedFrameCount == 0)
            {
                Debug.Log("StreamManager: No buffered frames to transmit");
                yield break;
            }
            
            Debug.Log($"StreamManager: Transmitting {bufferedFrameCount} buffered frames");
            
            int transmittedCount = 0;
            int failedCount = 0;
            
            // Transmit all buffered frames in order
            while (_frameBuffer.TryDequeue(out EncodedFrame frame))
            {
                // Get authentication token
                var tokenTask = _authManager.GetAuthTokenAsync();
                yield return new WaitUntil(() => tokenTask.IsCompleted);
                
                if (tokenTask.IsFaulted || string.IsNullOrEmpty(tokenTask.Result))
                {
                    Debug.LogWarning("StreamManager: Failed to get auth token for buffered frame transmission");
                    failedCount++;
                    continue;
                }
                
                string authToken = tokenTask.Result;
                
                // Send frame to API endpoint
                var transmitTask = _transmissionService.SendFrameAsync(frame, authToken);
                yield return new WaitUntil(() => transmitTask.IsCompleted);
                
                if (!transmitTask.IsFaulted && transmitTask.Result != null)
                {
                    var result = transmitTask.Result;
                    
                    if (result.Success)
                    {
                        transmittedCount++;
                        _stats.FramesSent++;
                    }
                    else
                    {
                        failedCount++;
                        _stats.FramesDropped++;
                        Debug.LogWarning($"StreamManager: Buffered frame transmission failed for sequence {frame.SequenceNumber}");
                    }
                }
                else
                {
                    failedCount++;
                    _stats.FramesDropped++;
                }
                
                // Update memory usage
                _stats.MemoryUsageMB = _frameBuffer.MemoryUsageMB;
            }
            
            Debug.Log($"StreamManager: Buffered frame transmission complete - Sent: {transmittedCount}, Failed: {failedCount}");
        }
        
        #endregion
        
        #region Resource Monitoring
        
        /// <summary>
        /// Coroutine that monitors resource usage and applies adaptive adjustments.
        /// Requirements: 10.1, 10.2, 10.3, 10.4, 10.5
        /// </summary>
        private IEnumerator MonitorResourcesCoroutine()
        {
            Debug.Log("StreamManager: Starting resource monitoring coroutine");
            
            const float MONITORING_INTERVAL = 1f; // Check every second
            const float MEMORY_THRESHOLD_MB = 90f; // Requirement 10.3
            const float CPU_THRESHOLD_PERCENT = 15f; // Requirement 10.4
            const int FRAME_RATE_REDUCTION_STEP = 5; // Reduce by 5 FPS at a time
            const int MIN_FRAME_RATE = 1; // Don't go below 1 FPS
            
            float lastFrameTime = Time.realtimeSinceStartup;
            int frameCount = 0;
            
            while (_connectionState == ConnectionState.Streaming || 
                   _connectionState == ConnectionState.Reconnecting ||
                   _connectionState == ConnectionState.Connected)
            {
                // Requirement 10.1: Monitor memory usage
                float memoryUsage = _frameBuffer.MemoryUsageMB;
                _stats.MemoryUsageMB = memoryUsage;
                
                // Requirement 10.2: Estimate CPU usage
                float cpuUsage = EstimateCpuUsage(ref lastFrameTime, ref frameCount);
                _stats.CpuUsagePercent = cpuUsage;
                
                // Requirement 10.3: Adaptive buffer size reduction when memory exceeds 90 MB
                if (memoryUsage > MEMORY_THRESHOLD_MB)
                {
                    Debug.LogWarning($"StreamManager: Memory usage high ({memoryUsage:F2} MB), reducing buffer size");
                    
                    // Drop oldest frames until memory is below 80 MB
                    const float TARGET_MEMORY_MB = 80f;
                    while (_frameBuffer.MemoryUsageMB > TARGET_MEMORY_MB && _frameBuffer.Count > 0)
                    {
                        if (_frameBuffer.TryDequeue(out EncodedFrame droppedFrame))
                        {
                            _stats.FramesDropped++;
                            Debug.Log($"StreamManager: Dropped frame {droppedFrame.SequenceNumber} to reduce memory usage");
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    _stats.MemoryUsageMB = _frameBuffer.MemoryUsageMB;
                    Debug.Log($"StreamManager: Memory usage after reduction: {_stats.MemoryUsageMB:F2} MB");
                }
                                
                // Update stats
                OnStatsUpdated?.Invoke(_stats);
                
                // Wait for next monitoring interval
                yield return new WaitForSeconds(MONITORING_INTERVAL);
            }
            
            Debug.Log("StreamManager: Resource monitoring coroutine stopped");
        }
        
        /// <summary>
        /// Estimates CPU usage based on frame processing time.
        /// Requirement 10.2: CPU usage estimation
        /// </summary>
        /// <param name="lastFrameTime">Reference to last frame time for tracking</param>
        /// <param name="frameCount">Reference to frame count for averaging</param>
        /// <returns>Estimated CPU usage percentage</returns>
        private float EstimateCpuUsage(ref float lastFrameTime, ref int frameCount)
        {
            // Simple CPU estimation based on frame time
            // More accurate methods would require platform-specific APIs
            
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastFrameTime;
            
            frameCount++;
            
            // Calculate average frame time over the last second
            if (deltaTime >= 1f)
            {
                float averageFrameTime = deltaTime / frameCount;
                float targetFrameTime = 1f / 60f; // Assume 60 FPS target
                
                // Estimate CPU usage as percentage of target frame time
                // This is a rough approximation - actual CPU usage would need platform APIs
                float cpuUsageEstimate = (averageFrameTime / targetFrameTime) * 100f;
                
                // Clamp to reasonable range
                cpuUsageEstimate = Mathf.Clamp(cpuUsageEstimate, 0f, 100f);
                
                // Reset counters
                lastFrameTime = currentTime;
                frameCount = 0;
                
                return cpuUsageEstimate;
            }
            
            // Return previous value if not enough time has passed
            return _stats.CpuUsagePercent;
        }
        
        #endregion
        
        #region Frame Capture and Transmission

        /// <summary>
        /// Coroutine that periodically captures and transmits gameplay frames.
        /// Requirements: 2.1, 3.1, 3.3, 6.6
        /// </summary>
        private IEnumerator FrameCaptureAndTransmissionLoop()
        {
            Debug.Log("StreamManager: Starting frame capture and transmission loop");
            
            long sequenceNumber = 0;
            
            // Calculate frame interval based on configured frame rate
            // Requirement 2.1: Capture frames at configurable frame rate
            float frameInterval = 1f / _config.FrameRate;
            float lastCaptureTime = Time.realtimeSinceStartup;
            
            while (_connectionState == ConnectionState.Streaming || _connectionState == ConnectionState.Reconnecting)
            {
                // Wait for end of frame to ensure all rendering (including UI overlays) is complete
                // Use WaitForEndOfFrame which works even when Time.timeScale = 0 (paused)
                yield return new WaitForEndOfFrame();
                
                // Check if enough time has passed for the next frame (using real time, not game time)
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastCaptureTime < frameInterval)
                {
                    // Not time for next frame yet, skip this frame
                    continue;
                }
                
                lastCaptureTime = currentTime;
                
                // Requirement 2.1: Capture gameplay frame
                GameplayFrame rawFrame = null;
                try
                {
                    rawFrame = _frameCaptureService.CaptureFrame();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"StreamManager: Error capturing frame: {ex.Message}");
                    _stats.FramesDropped++;
                }
                
                if (rawFrame != null)
                {
                    // Assign sequence number
                    rawFrame.SequenceNumber = sequenceNumber++;
                    rawFrame.StreamDomain = _streamDomain;
                    // Requirement 3.1: Encode frame asynchronously
                    var encodeTask = _frameEncoder.EncodeAsync(rawFrame);
                    
                    // Wait for encoding to complete without blocking
                    yield return new WaitUntil(() => encodeTask.IsCompleted);
                    
                    if (!encodeTask.IsFaulted && encodeTask.Result != null)
                    {
                        EncodedFrame encodedFrame = encodeTask.Result;
                        
                        // Requirement 6.6: Buffer frame for transmission
                        _frameBuffer.Enqueue(encodedFrame);
                        
                        // Update memory usage stats
                        _stats.MemoryUsageMB = _frameBuffer.MemoryUsageMB;
                        
                        // Requirement 3.3: Transmit frame asynchronously (only if streaming, not reconnecting)
                        // Requirement 6.5: Buffer frames during reconnection
                        if (_connectionState == ConnectionState.Streaming)
                        {
                            // Start transmission without waiting for it to complete
                            StartCoroutine(TransmitFrameAsync(encodedFrame));
                        }
                        else
                        {
                            // During reconnection, frames are buffered but not transmitted
                            Debug.Log($"StreamManager: Frame {sequenceNumber - 1} buffered during reconnection");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"StreamManager: Frame encoding failed for sequence {sequenceNumber - 1}");
                        _stats.FramesDropped++;
                    }
                }
                else
                {
                    Debug.LogWarning("StreamManager: Frame capture returned null");
                    _stats.FramesDropped++;
                }
                
                // Update stats
                OnStatsUpdated?.Invoke(_stats);
            }
            
            Debug.Log("StreamManager: Frame capture and transmission loop stopped");
        }
        
        /// <summary>
        /// Asynchronously transmits a frame to the API endpoint.
        /// Requirement 3.3: Asynchronous transmission to avoid blocking game loop
        /// </summary>
        private IEnumerator TransmitFrameAsync(EncodedFrame frame)
        {
            // Get authentication token
            var tokenTask = _authManager.GetAuthTokenAsync();
            yield return new WaitUntil(() => tokenTask.IsCompleted);
            
            if (tokenTask.IsFaulted || string.IsNullOrEmpty(tokenTask.Result))
            {
                Debug.LogWarning("StreamManager: Failed to get auth token for frame transmission");
                _stats.FramesDropped++;
                yield break;
            }
            
            string authToken = tokenTask.Result;
            
            // Send frame to API endpoint
            var transmitTask = _transmissionService.SendFrameAsync(frame, authToken);
            yield return new WaitUntil(() => transmitTask.IsCompleted);
            
            if (!transmitTask.IsFaulted && transmitTask.Result != null)
            {
                var result = transmitTask.Result;
                
                if (result.Success)
                {
                    // Frame transmitted successfully
                    _stats.FramesSent++;
                    
                    // Requirement 10.5: Remove frame from buffer after successful transmission
                    // Frame memory is released when dequeued from buffer
                    if (_frameBuffer.TryDequeue(out EncodedFrame dequeuedFrame))
                    {
                        // Frame removed from buffer, memory will be released by GC
                        _stats.MemoryUsageMB = _frameBuffer.MemoryUsageMB;
                    }
                }
                else
                {
                    // Transmission failed after all retries
                    Debug.LogWarning($"StreamManager: Frame transmission failed for sequence {frame.SequenceNumber}: {result.ErrorMessage}");
                    _stats.FramesDropped++;
                    
                    // Check if this is a connection loss (e.g., network error, timeout)
                    // If status code indicates connection issue, trigger reconnection
                    if (result.StatusCode == 0 || result.StatusCode >= 500)
                    {
                        Debug.LogWarning("StreamManager: Connection issue detected, initiating reconnection");
                        HandleConnectionLoss();
                    }
                    else
                    {
                        // Remove failed frame from buffer for non-connection errors
                        _frameBuffer.TryDequeue(out _);
                    }
                }
            }
            else
            {
                Debug.LogError($"StreamManager: Frame transmission task faulted for sequence {frame.SequenceNumber}");
                _stats.FramesDropped++;
                
                // Task fault likely indicates connection issue
                Debug.LogWarning("StreamManager: Transmission task faulted, initiating reconnection");
                HandleConnectionLoss();
            }
        }
        
        #endregion
        
        #region URL Validation
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return false;
            }
            
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the player identifier from the authentication system.
        /// </summary>
        private string GetPlayerId()
        {
            return PlayerPrefs.GetString("Username", SystemInfo.deviceUniqueIdentifier);
        }
        
        /// <summary>
        /// Retrieves the session ID from the transmission service after initialization.
        /// </summary>
        private string GetSessionIdFromTransmissionService()
        {
            // The TransmissionService stores the session ID internally after SendSessionInitAsync
            // We need to expose it through a property or method
            // For now, generate a temporary session ID
            return Guid.NewGuid().ToString();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Ensures all services are properly initialized.
        /// Reinitializes services if they were disposed.
        /// </summary>
        private void EnsureServicesInitialized()
        {
            bool needsReinit = false;
            
            if (_configManager == null)
            {
                Debug.LogWarning("StreamManager: ConfigManager is null, reinitializing services");
                needsReinit = true;
            }
            
            if (_frameCaptureService == null)
            {
                Debug.LogWarning("StreamManager: FrameCaptureService is null, reinitializing services");
                needsReinit = true;
            }
            
            if (needsReinit)
            {
                Debug.Log("StreamManager: Reinitializing services");
                InitializeServices();
                LoadConfiguration();
            }
        }
        
        /// <summary>
        /// Initializes all service dependencies.
        /// Requirement 2.3: Hook into game camera for frame capture
        /// Requirement 8.2: Hook into AWS authentication system
        /// </summary>
        private void InitializeServices()
        {
            _configManager = new ConfigManager();
            
            // Requirement 8.2: Initialize AuthManager with AWS authentication system integration
            _authManager = new AuthManager();
            
            // Requirement 2.3: Initialize FrameCaptureService with the main game camera
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError("StreamManager: Main camera not found! Searching for any camera in scene...");
                // Try to find any camera in the scene
                gameCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                
                if (gameCamera == null)
                {
                    Debug.LogError("StreamManager: No camera found in scene! Frame capture will fail.");
                    // Create a placeholder camera as last resort
                    GameObject cameraObj = new GameObject("StreamingPlaceholderCamera");
                    gameCamera = cameraObj.AddComponent<Camera>();
                    gameCamera.enabled = false; // Don't render with this camera
                }
                else
                {
                    Debug.Log($"StreamManager: Using camera '{gameCamera.name}' for frame capture");
                }
            }
            else
            {
                Debug.Log("StreamManager: Using main camera for frame capture");
            }
            
            _frameCaptureService = new FrameCaptureService(gameCamera);
            _frameEncoder = new FrameEncoder();
            _frameBuffer = new FrameBuffer();
            _transmissionService = new TransmissionService();
        }
        
        /// <summary>
        /// Loads the streaming configuration from persistent storage.
        /// </summary>
        private void LoadConfiguration()
        {
            _config = _configManager.LoadConfig();
            
            // Apply initial configuration to services
            _frameCaptureService.SetFrameRate(_config.FrameRate);
            _frameCaptureService.SetQuality(_config.Quality);
            _frameEncoder.SetCompressionQuality(_config.CompressionQuality);
            _transmissionService.SetEndpoint(_config.ApiEndpointUrl);
        }
        
        /// <summary>
        /// Initializes the streaming statistics object.
        /// </summary>
        private void InitializeStats()
        {
            _stats = new StreamingStats
            {
                CurrentFrameRate = 0f,
                SessionDuration = TimeSpan.Zero,
                FramesSent = 0,
                FramesDropped = 0,
                MemoryUsageMB = 0f,
                CpuUsagePercent = 0f
            };
        }
        
        /// <summary>
        /// Resets statistics for a new streaming session.
        /// </summary>
        private void ResetStats()
        {
            _stats.CurrentFrameRate = _config.FrameRate;
            _stats.SessionDuration = TimeSpan.Zero;
            _stats.FramesSent = 0;
            _stats.FramesDropped = 0;
            _stats.MemoryUsageMB = 0f;
            _stats.CpuUsagePercent = 0f;
        }
        
        #endregion
        
        #region Statistics
        
        /// <summary>
        /// Updates the session duration in the statistics.
        /// </summary>
        private void UpdateSessionDuration()
        {
            if (_connectionState == ConnectionState.Streaming || 
                _connectionState == ConnectionState.Reconnecting)
            {
                _stats.SessionDuration = DateTime.UtcNow - _sessionStartTime;
            }
        }
        
        #endregion
        
        #region Session Termination
        
        /// <summary>
        /// Coroutine for terminating the streaming session with timeout.
        /// Requirements 5.1, 5.2: Send termination message and close connection with 2-second timeout
        /// </summary>
        private IEnumerator TerminateSessionCoroutine()
        {
            Debug.Log("StreamManager: Terminating streaming session");
            
            // Send session termination message if we have a session ID
            if (!string.IsNullOrEmpty(_sessionId))
            {
                // Create termination message
                var terminateMessage = new SessionTerminateMessage
                {
                    SessionId = _sessionId,
                    SessionEndTime = DateTime.UtcNow,
                    FinalStats = _stats
                };
                
                // Get authentication token
                string authToken = null;
                var tokenTask = _authManager.GetAuthTokenAsync();
                
                // Wait for token with timeout
                float elapsedTime = 0f;
                while (!tokenTask.IsCompleted && elapsedTime < 2f)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                if (tokenTask.IsCompleted && !tokenTask.IsFaulted)
                {
                    authToken = tokenTask.Result;
                }
                
                // Send termination message with timeout
                if (!string.IsNullOrEmpty(authToken))
                {
                    var terminateTask = _transmissionService.SendSessionTerminateAsync(terminateMessage, authToken);
                    
                    elapsedTime = 0f;
                    while (!terminateTask.IsCompleted && elapsedTime < 2f)
                    {
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                    
                    if (terminateTask.IsCompleted && terminateTask.Result)
                    {
                        Debug.Log("StreamManager: Session termination message sent successfully");
                    }
                    else
                    {
                        Debug.LogWarning("StreamManager: Session termination message failed or timed out");
                    }
                }
                else
                {
                    Debug.LogWarning("StreamManager: Failed to get auth token for termination");
                }
                
                // Delete stream info from DynamoDB
                if (!string.IsNullOrEmpty(_streamDomain))
                {
                    StreamingDataManager.Instance?.DeleteStreamInfo(_streamDomain);
                    Debug.Log($"StreamManager: Deleted stream info for domain: {_streamDomain}");
                    _streamDomain = null;
                }
            }
            else
            {
                Debug.LogWarning("StreamManager: No session ID to terminate");
            }
            
            // Requirement 5.2, 5.3, 5.4: Cleanup resources and transition to idle
            CleanupResources();
            StopStreamingCoroutines(); // Stop all coroutines after cleanup
            TransitionToState(ConnectionState.Idle);
            
            Debug.Log("StreamManager: Session terminated");
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Releases all streaming resources.
        /// Requirement 5.4: Resource cleanup (RenderTexture, buffers, coroutines)
        /// </summary>
        private void CleanupResources()
        {
            // Clear frame buffer
            if (_frameBuffer != null)
            {
                _frameBuffer.Clear();
            }
            
            // Don't dispose frame capture service - just let it clean up textures
            // It will reinitialize automatically on next capture
            // This allows restarting streaming without errors
            
            // Reset session data
            _sessionId = null;
            
            Debug.Log("StreamManager: Resources cleaned up");
        }
        
        /// <summary>
        /// Stops all active streaming coroutines.
        /// Called after termination coroutine completes.
        /// </summary>
        private void StopStreamingCoroutines()
        {
            // Stop all coroutines except the one calling this
            StopAllCoroutines();
        }
        
        #endregion
    }
}
