using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Models;
using QualityLevel = TowerDefense.Streaming.Core.QualityLevel;

namespace TowerDefense.Streaming.UI
{
    /// <summary>
    /// MonoBehaviour that displays streaming status and provides a configuration interface.
    /// Requirements: 4.1, 4.2, 4.3, 7.1, 7.2, 7.3, 7.4, 7.5
    /// </summary>
    public class UIController : MonoBehaviour
    {
        #region Serialized Fields - StreamManager Reference
        
        [Header("Stream Manager")]
        [SerializeField] private StreamManager streamManager;
        
        #endregion
        
        #region Private Fields
        
        private bool _needsStreamManagerRefresh = false;
        private bool _wasStreaming = false; // Track if we were streaming to show end message
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Find StreamManager if not assigned
            EnsureStreamManagerReference();
        }
        
        private void OnEnable()
        {
            // Refresh StreamManager reference when UI becomes active
            // This handles the case where we navigate back to this scene
            _needsStreamManagerRefresh = true;
        }
        
        private void Update()
        {
            // Refresh StreamManager reference if needed
            if (_needsStreamManagerRefresh)
            {
                EnsureStreamManagerReference();
                _needsStreamManagerRefresh = false;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (streamManager != null)
            {
                UnsubscribeFromStreamManagerEvents();
            }
        }
        
        /// <summary>
        /// Ensures we have a valid reference to the StreamManager.
        /// Finds it dynamically if the serialized reference is null.
        /// </summary>
        private void EnsureStreamManagerReference()
        {
            // Use GetInstance to get or create the singleton
            StreamManager foundManager = StreamManager.GetInstance();
            
            // Check if we found a different StreamManager instance
            if (foundManager != streamManager)
            {
                // Unsubscribe from old manager if it exists
                if (streamManager != null)
                {
                    UnsubscribeFromStreamManagerEvents();
                }
                
                // Update reference
                streamManager = foundManager;
                
                if (streamManager != null)
                {
                    Debug.Log("UIController: Got StreamManager instance");
                    SubscribeToStreamManagerEvents();
                }
                else
                {
                    Debug.LogError("UIController: Failed to get StreamManager instance");
                }
            }
            else if (streamManager != null)
            {
                // Same manager, just ensure we're subscribed
                SubscribeToStreamManagerEvents();
            }
        }
        
        /// <summary>
        /// Subscribes to StreamManager events.
        /// </summary>
        private void SubscribeToStreamManagerEvents()
        {
            if (streamManager == null) return;
            
            // Unsubscribe first to prevent duplicate subscriptions
            UnsubscribeFromStreamManagerEvents();
            
            streamManager.OnConnectionStateChanged += UpdateConnectionState;
            streamManager.OnError += ShowErrorNotification;
            streamManager.OnStatsUpdated += UpdateStats;
            
            Debug.Log("UIController: Subscribed to StreamManager events");
        }
        
        /// <summary>
        /// Unsubscribes from StreamManager events.
        /// </summary>
        private void UnsubscribeFromStreamManagerEvents()
        {
            if (streamManager == null) return;
            
            streamManager.OnConnectionStateChanged -= UpdateConnectionState;
            streamManager.OnError -= ShowErrorNotification;
            streamManager.OnStatsUpdated -= UpdateStats;
            
            Debug.Log("UIController: Unsubscribed from StreamManager events");
        }
        
        /// <summary>
        /// Updates all stats from StreamingStats object.
        /// </summary>
        private void UpdateStats(StreamingStats stats)
        {
            UpdateFrameRate(stats.CurrentFrameRate);
            UpdateSessionDuration(stats.SessionDuration);
        }
        
        #endregion
        
        #region Serialized Fields - Status Display
        
        [Header("Status Display")]
        [SerializeField] private GameObject streamingIndicator;
        [SerializeField] private TextMeshProUGUI connectionStateText;
        [SerializeField] private TextMeshProUGUI frameRateText;
        [SerializeField] private TextMeshProUGUI sessionDurationText;
        
        [Header("Error Notifications")]
        [SerializeField] private GameObject errorNotificationPanel;
        [SerializeField] private TextMeshProUGUI errorMessageText;
        
        [Header("Configuration Interface")]
        [SerializeField] private GameObject configurationPanel;
        [SerializeField] private TMP_InputField apiUrlInput;
        [SerializeField] private TMP_Dropdown frameRateDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        
        #endregion
        
        #region Public Methods - Status Updates
        
        /// <summary>
        /// Updates the streaming indicator visibility.
        /// Requirement 7.1: Display streaming indicator while streaming is active.
        /// </summary>
        /// <param name="isStreaming">True if streaming is active, false otherwise.</param>
        public void UpdateStreamingIndicator(bool isStreaming)
        {
            if (streamingIndicator != null)
            {
                streamingIndicator.SetActive(isStreaming);
            }
        }
        
        /// <summary>
        /// Updates the connection state display.
        /// Requirement 7.2: Display the current connection state to the player.
        /// </summary>
        /// <param name="state">The current connection state.</param>
        public void UpdateConnectionState(ConnectionState state)
        {
            if (connectionStateText != null)
            {
                connectionStateText.text = $"Status: {GetConnectionStateDisplayText(state)}";
            }
            
            // Track streaming state changes
            bool isCurrentlyStreaming = (state == ConnectionState.Streaming);
            
            // If we were streaming and now we're idle, show "Stream has ended" message
            if (_wasStreaming && state == ConnectionState.Idle)
            {
                ShowSuccessMessage("Stream has ended");
                _wasStreaming = false;
            }
            else if (isCurrentlyStreaming)
            {
                _wasStreaming = true;
                // Hide any previous messages when starting to stream
                HideError();
            }
            
            // Update streaming indicator based on state
            UpdateStreamingIndicator(isCurrentlyStreaming);
        }
        
        /// <summary>
        /// Updates the frame rate display.
        /// Requirement 7.3: Display the current frame rate being transmitted.
        /// </summary>
        /// <param name="frameRate">The current frame rate in frames per second.</param>
        public void UpdateFrameRate(float frameRate)
        {
            if (frameRateText != null)
            {
                frameRateText.text = $"FPS: {frameRate:F1}";
            }
        }
        
        /// <summary>
        /// Updates the session duration display.
        /// Requirement 7.4: Display the total duration of the current stream session.
        /// </summary>
        /// <param name="duration">The session duration.</param>
        public void UpdateSessionDuration(TimeSpan duration)
        {
            if (sessionDurationText != null)
            {
                sessionDurationText.text = $"Duration: {duration:hh\\:mm\\:ss}";
            }
        }
        

        
        /// <summary>
        /// Displays an error notification to the player.
        /// Requirement 7.5: Display error notifications when streaming errors occur.
        /// </summary>
        /// <param name="errorMessage">The error message to display.</param>
        public void ShowError(string errorMessage)
        {
            if (errorNotificationPanel != null && errorMessageText != null)
            {
                errorMessageText.text = errorMessage;
                errorNotificationPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Displays a success notification to the player.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        public void ShowSuccessMessage(string message)
        {
            if (errorNotificationPanel != null && errorMessageText != null)
            {
                errorMessageText.text = message;
                errorNotificationPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hides the error notification panel.
        /// </summary>
        public void HideError()
        {
            if (errorNotificationPanel != null)
            {
                errorNotificationPanel.SetActive(false);
            }
        }
        
        #endregion
        
        #region Public Methods - Configuration Interface
        
        /// <summary>
        /// Shows the configuration panel.
        /// </summary>
        public void ShowConfigurationPanel()
        {
            if (configurationPanel != null)
            {
                configurationPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hides the configuration panel.
        /// </summary>
        public void HideConfigurationPanel()
        {
            if (configurationPanel != null)
            {
                configurationPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Loads configuration values into the UI controls.
        /// Requirements 4.2, 4.3: Provide configuration interface for frame rate and quality settings.
        /// Note: API URL is hardcoded and not user-configurable.
        /// </summary>
        /// <param name="config">The configuration to load.</param>
        public void LoadConfiguration(StreamingConfig config)
        {
            if (config == null) return;
            
            // API URL is hardcoded - hide or disable the input field
            if (apiUrlInput != null)
            {
                apiUrlInput.text = StreamingConfig.DEFAULT_API_ENDPOINT;
                apiUrlInput.interactable = false; // Make it read-only
            }
            
            // Load frame rate (map to dropdown index)
            if (frameRateDropdown != null)
            {
                frameRateDropdown.value = GetFrameRateDropdownIndex(config.FrameRate);
            }
            
            // Load quality level
            if (qualityDropdown != null)
            {
                qualityDropdown.value = (int)config.Quality;
            }
        }
        
        /// <summary>
        /// Retrieves the current configuration from the UI controls.
        /// Requirements 4.2, 4.3: Provide configuration interface for frame rate and quality settings.
        /// Note: API URL is hardcoded and not user-configurable.
        /// </summary>
        /// <returns>A StreamingConfig object with values from the UI.</returns>
        public StreamingConfig GetConfiguration()
        {
            var config = StreamingConfig.Default();
            
            // API URL is hardcoded - always use the constant
            // Don't read from UI input field
            config.ApiEndpointUrl = StreamingConfig.DEFAULT_API_ENDPOINT;
            
            // Get frame rate from dropdown
            if (frameRateDropdown != null)
            {
                config.FrameRate = GetFrameRateFromDropdownIndex(frameRateDropdown.value);
            }
            
            // Get quality level from dropdown
            if (qualityDropdown != null)
            {
                config.Quality = (QualityLevel)qualityDropdown.value;
            }
            
            return config;
        }
        
        #endregion
        
        #region Public Methods - Button Handlers
        
        /// <summary>
        /// Called when the Apply Config button is clicked.
        /// Reads configuration from UI and applies it to StreamManager.
        /// </summary>
        public void OnApplyConfigButtonClicked()
        {
            Debug.Log("UIController: Apply Config button clicked");
            
            // Ensure we have StreamManager reference
            EnsureStreamManagerReference();
            
            if (streamManager == null)
            {
                Debug.LogError("UIController: StreamManager reference is null - cannot apply config");
                ShowErrorNotification("StreamManager not found. Please restart the game.");
                return;
            }
            
            // Get configuration from UI
            StreamingConfig config = GetConfiguration();
            
            Debug.Log($"UIController: Applying configuration - URL: {config.ApiEndpointUrl}, FPS: {config.FrameRate}, Quality: {config.Quality}");
            
            // Apply to StreamManager
            streamManager.UpdateConfiguration(config);
            
            Debug.Log("UIController: Configuration applied successfully");
            
            // Optionally hide config panel after applying
            // HideConfigurationPanel();
        }
        
        /// <summary>
        /// Called when Start Streaming button is clicked.
        /// Public method to be called from Unity Button OnClick event.
        /// </summary>
        public void OnStartStreamingButtonClicked()
        {
            Debug.Log("UIController: Start Streaming button clicked");
            
            // Ensure we have StreamManager reference
            EnsureStreamManagerReference();
            
            if (streamManager == null)
            {
                Debug.LogError("UIController: StreamManager reference is null - cannot start streaming");
                ShowErrorNotification("StreamManager not found. Please restart the game.");
                return;
            }
            
            Debug.Log($"UIController: Starting streaming (current state: {streamManager.GetConnectionState()})");
            streamManager.StartStreaming();
        }
        
        /// <summary>
        /// Called when Stop Streaming button is clicked.
        /// Public method to be called from Unity Button OnClick event.
        /// </summary>
        public void OnStopStreamingButtonClicked()
        {
            Debug.Log("UIController: Stop Streaming button clicked");
            
            // Ensure we have StreamManager reference
            EnsureStreamManagerReference();
            
            if (streamManager == null)
            {
                Debug.LogError("UIController: StreamManager reference is null - cannot stop streaming");
                ShowErrorNotification("StreamManager not found. Please restart the game.");
                return;
            }
            
            // Check if StreamManager is ready
            if (!streamManager.IsReady())
            {
                Debug.LogError("UIController: StreamManager is not ready - reinitializing");
                ShowErrorNotification("StreamManager not ready. Please try again.");
                return;
            }
            
            Debug.Log($"UIController: Stopping streaming (current state: {streamManager.GetConnectionState()})");
            streamManager.StopStreaming();
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles connection state changes from StreamManager.
        /// Requirement 7.2: Display the current connection state to the player.
        /// </summary>
        /// <param name="state">The new connection state.</param>
        private void HandleConnectionStateChanged(ConnectionState state)
        {
            UpdateConnectionState(state);
        }
        
        /// <summary>
        /// Handles error notifications from StreamManager.
        /// Requirement 7.5: Display error notifications when streaming errors occur.
        /// </summary>
        /// <param name="errorMessage">The error message to display.</param>
        private void HandleError(string errorMessage)
        {
            ShowError(errorMessage);
        }
        
        /// <summary>
        /// Shows error notification (alias for ShowError for event subscription).
        /// </summary>
        private void ShowErrorNotification(string errorMessage)
        {
            ShowError(errorMessage);
        }
        
        /// <summary>
        /// Handles statistics updates from StreamManager.
        /// Requirements 7.3, 7.4: Display frame rate and session duration.
        /// </summary>
        /// <param name="stats">The updated streaming statistics.</param>
        private void HandleStatsUpdated(StreamingStats stats)
        {
            UpdateStats(stats);
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Converts a ConnectionState enum to a user-friendly display string.
        /// </summary>
        private string GetConnectionStateDisplayText(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Idle:
                    return "Idle";
                case ConnectionState.Connecting:
                    return "Connecting...";
                case ConnectionState.Connected:
                    return "Connected";
                case ConnectionState.Streaming:
                    return "Streaming";
                case ConnectionState.Disconnected:
                    return "Disconnected";
                case ConnectionState.Reconnecting:
                    return "Reconnecting...";
                case ConnectionState.Terminating:
                    return "Stopping...";
                case ConnectionState.Error:
                    return "Error";
                default:
                    return "Unknown";
            }
        }
        
        /// <summary>
        /// Maps a frame rate value to a dropdown index.
        /// Supports common frame rates: 5, 10, 15, 20, 25, 30 FPS.
        /// </summary>
        private int GetFrameRateDropdownIndex(int frameRate)
        {
            switch (frameRate)
            {
                case 5: return 0;
                case 10: return 1;
                case 15: return 2;
                case 20: return 3;
                case 25: return 4;
                case 30: return 5;
                default: return 2; // Default to 15 FPS
            }
        }
        
        /// <summary>
        /// Maps a dropdown index to a frame rate value.
        /// Supports common frame rates: 5, 10, 15, 20, 25, 30 FPS.
        /// </summary>
        private int GetFrameRateFromDropdownIndex(int index)
        {
            switch (index)
            {
                case 0: return 5;
                case 1: return 10;
                case 2: return 15;
                case 3: return 20;
                case 4: return 25;
                case 5: return 30;
                default: return 15; // Default to 15 FPS
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Initialize UI state
            if (errorNotificationPanel != null)
            {
                errorNotificationPanel.SetActive(false);
            }
            
            if (configurationPanel != null)
            {
                configurationPanel.SetActive(false);
            }
            
            if (streamingIndicator != null)
            {
                streamingIndicator.SetActive(false);
            }
            
            // Initialize dropdown options if needed
            InitializeDropdowns();
        }
        
        private void OnDisable()
        {
            // Unsubscribe from StreamManager events to prevent memory leaks
            if (streamManager != null)
            {
                streamManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
                streamManager.OnError -= HandleError;
                streamManager.OnStatsUpdated -= HandleStatsUpdated;
            }
        }
        
        /// <summary>
        /// Initializes dropdown options for frame rate and quality.
        /// </summary>
        private void InitializeDropdowns()
        {
            // Initialize frame rate dropdown
            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                frameRateDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "5 FPS",
                    "10 FPS",
                    "15 FPS",
                    "20 FPS",
                    "25 FPS",
                    "30 FPS"
                });
            }
            
            // Initialize quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Low (640x360)",
                    "Medium (1280x720)",
                    "High (1920x1080)"
                });
            }
        }
        
        #endregion
    }
}
