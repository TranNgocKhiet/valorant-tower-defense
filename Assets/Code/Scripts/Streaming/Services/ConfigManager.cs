using System;
using System.Text.RegularExpressions;
using UnityEngine;
using TowerDefense.Streaming.Core;
using TowerDefense.Streaming.Models;
using QualityLevel = TowerDefense.Streaming.Core.QualityLevel;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Manages streaming configuration persistence and validation using Unity PlayerPrefs.
    /// </summary>
    public class ConfigManager
    {
        private const string PREF_API_ENDPOINT = "Streaming_ApiEndpoint";
        private const string PREF_FRAME_RATE = "Streaming_FrameRate";
        private const string PREF_QUALITY = "Streaming_Quality";
        private const string PREF_COMPRESSION_QUALITY = "Streaming_CompressionQuality";
        private const string PREF_MAX_BUFFER_SECONDS = "Streaming_MaxBufferSeconds";
        private const string PREF_MAX_BUFFER_SIZE_MB = "Streaming_MaxBufferSizeMB";
        private const string PREF_RECONNECT_INTERVAL = "Streaming_ReconnectInterval";
        private const string PREF_MAX_RECONNECT_DURATION = "Streaming_MaxReconnectDuration";
        private const string PREF_MAX_RETRY_ATTEMPTS = "Streaming_MaxRetryAttempts";

        /// <summary>
        /// Loads streaming configuration from PlayerPrefs.
        /// Returns default configuration if no saved settings exist.
        /// </summary>
        public StreamingConfig LoadConfig()
        {
            if (!PlayerPrefs.HasKey(PREF_API_ENDPOINT))
            {
                return StreamingConfig.Default();
            }

            var config = new StreamingConfig
            {
                ApiEndpointUrl = PlayerPrefs.GetString(PREF_API_ENDPOINT, ""),
                FrameRate = PlayerPrefs.GetInt(PREF_FRAME_RATE, 15),
                Quality = (QualityLevel)PlayerPrefs.GetInt(PREF_QUALITY, (int)QualityLevel.Medium),
                CompressionQuality = PlayerPrefs.GetInt(PREF_COMPRESSION_QUALITY, 75),
                MaxBufferSeconds = PlayerPrefs.GetInt(PREF_MAX_BUFFER_SECONDS, 30),
                MaxBufferSizeMB = PlayerPrefs.GetInt(PREF_MAX_BUFFER_SIZE_MB, 100),
                ReconnectIntervalSeconds = PlayerPrefs.GetInt(PREF_RECONNECT_INTERVAL, 5),
                MaxReconnectDurationSeconds = PlayerPrefs.GetInt(PREF_MAX_RECONNECT_DURATION, 60),
                MaxRetryAttempts = PlayerPrefs.GetInt(PREF_MAX_RETRY_ATTEMPTS, 3)
            };

            return config;
        }

        /// <summary>
        /// Saves streaming configuration to PlayerPrefs.
        /// </summary>
        public void SaveConfig(StreamingConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            PlayerPrefs.SetString(PREF_API_ENDPOINT, config.ApiEndpointUrl ?? "");
            PlayerPrefs.SetInt(PREF_FRAME_RATE, config.FrameRate);
            PlayerPrefs.SetInt(PREF_QUALITY, (int)config.Quality);
            PlayerPrefs.SetInt(PREF_COMPRESSION_QUALITY, config.CompressionQuality);
            PlayerPrefs.SetInt(PREF_MAX_BUFFER_SECONDS, config.MaxBufferSeconds);
            PlayerPrefs.SetInt(PREF_MAX_BUFFER_SIZE_MB, config.MaxBufferSizeMB);
            PlayerPrefs.SetInt(PREF_RECONNECT_INTERVAL, config.ReconnectIntervalSeconds);
            PlayerPrefs.SetInt(PREF_MAX_RECONNECT_DURATION, config.MaxReconnectDurationSeconds);
            PlayerPrefs.SetInt(PREF_MAX_RETRY_ATTEMPTS, config.MaxRetryAttempts);
            
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Validates streaming configuration according to system requirements.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool ValidateConfig(StreamingConfig config)
        {
            if (config == null)
            {
                return false;
            }

            // Validate frame rate (1-30 FPS)
            if (config.FrameRate < 1 || config.FrameRate > 30)
            {
                return false;
            }

            // Validate API endpoint URL format
            if (!IsValidUrl(config.ApiEndpointUrl))
            {
                return false;
            }

            // Validate compression quality (0-100)
            if (config.CompressionQuality < 0 || config.CompressionQuality > 100)
            {
                return false;
            }

            // Validate quality level enum
            if (!Enum.IsDefined(typeof(QualityLevel), config.Quality))
            {
                return false;
            }

            // Validate buffer settings (positive values)
            if (config.MaxBufferSeconds <= 0 || config.MaxBufferSizeMB <= 0)
            {
                return false;
            }

            // Validate reconnection settings (positive values)
            if (config.ReconnectIntervalSeconds <= 0 || config.MaxReconnectDurationSeconds <= 0)
            {
                return false;
            }

            // Validate retry attempts (non-negative)
            if (config.MaxRetryAttempts < 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates URL format for API endpoint.
        /// </summary>
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            // Check for valid HTTP/HTTPS URL format
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return false;
            }

            // Ensure scheme is http or https
            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return true;
        }
    }
}
