using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TowerDefense.Streaming.Interfaces;
using TowerDefense.Streaming.Models;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Service for transmitting streaming data to the API endpoint via HTTP.
    /// Implements retry logic with exponential backoff for reliable transmission.
    /// </summary>
    public class TransmissionService : ITransmissionService
    {
        private string _apiEndpointUrl;
        private string _sessionId;
        private const int MaxRetryAttempts = 3;
        
        /// <summary>
        /// Sets the API endpoint URL for all transmission requests.
        /// </summary>
        /// <param name="url">The base API endpoint URL.</param>
        public void SetEndpoint(string url)
        {
            _apiEndpointUrl = url?.TrimEnd('/');
        }
        
        /// <summary>
        /// Sets the session ID for frame transmission requests.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
        }
        
        /// <summary>
        /// Asynchronously sends a frame to the API endpoint with retry logic.
        /// </summary>
        /// <param name="frame">The encoded frame to transmit.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>Result of the transmission attempt including retry count.</returns>
        public async Task<TransmissionResult> SendFrameAsync(EncodedFrame frame, string authToken)
        {
            if (string.IsNullOrEmpty(_apiEndpointUrl))
            {
                return new TransmissionResult
                {
                    Success = false,
                    StatusCode = 0,
                    ErrorMessage = "API endpoint URL not set",
                    RetryCount = 0
                };
            }
            
            string url = $"{_apiEndpointUrl}/stream/frame";
            string json = JsonConvert.SerializeObject(frame);
            
            int retryCount = 0;
            TransmissionResult result = null;
            
            while (retryCount < MaxRetryAttempts)
            {
                result = await SendPostRequestAsync(url, json, authToken, _sessionId);
                
                if (result.Success)
                {
                    result.RetryCount = retryCount;
                    return result;
                }
                
                retryCount++;
                
                if (retryCount < MaxRetryAttempts)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    float backoffDelay = Mathf.Pow(2, retryCount);
                    await Task.Delay(TimeSpan.FromSeconds(backoffDelay));
                }
            }
            
            // All retries failed
            result.RetryCount = retryCount;
            return result;
        }

        /// <summary>
        /// Asynchronously sends a session initialization message.
        /// </summary>
        /// <param name="message">The session initialization message.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        public async Task<bool> SendSessionInitAsync(SessionInitMessage message, string authToken)
        {
            if (string.IsNullOrEmpty(_apiEndpointUrl))
            {
                Debug.LogError("TransmissionService: API endpoint URL not set");
                return false;
            }
            
            string url = $"{_apiEndpointUrl}/stream/init";
            string json = JsonConvert.SerializeObject(message);
            
            TransmissionResult result = await SendPostRequestAsync(url, json, authToken, null);
            
            if (result.Success)
            {
                // Parse session ID from response
                try
                {
                    var response = JsonConvert.DeserializeObject<SessionInitResponse>(result.ErrorMessage);
                    if (response != null && !string.IsNullOrEmpty(response.SessionId))
                    {
                        _sessionId = response.SessionId;
                        Debug.Log($"TransmissionService: Session initialized with ID: {_sessionId}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"TransmissionService: Failed to parse session ID from response: {ex.Message}");
                }
            }
            
            return result.Success;
        }
        
        /// <summary>
        /// Asynchronously sends a session termination message.
        /// </summary>
        /// <param name="message">The session termination message.</param>
        /// <param name="authToken">Authentication token for the request.</param>
        /// <returns>True if termination was successful, false otherwise.</returns>
        public async Task<bool> SendSessionTerminateAsync(SessionTerminateMessage message, string authToken)
        {
            if (string.IsNullOrEmpty(_apiEndpointUrl))
            {
                Debug.LogError("TransmissionService: API endpoint URL not set");
                return false;
            }
            
            string url = $"{_apiEndpointUrl}/stream/terminate";
            string json = JsonConvert.SerializeObject(message);
            
            TransmissionResult result = await SendPostRequestAsync(url, json, authToken, _sessionId);
            
            if (result.Success)
            {
                Debug.Log("TransmissionService: Session terminated successfully");
                _sessionId = null;
            }
            
            return result.Success;
        }
        
        /// <summary>
        /// Sends a POST request to the specified URL with authentication and session headers.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="jsonBody">The JSON body content.</param>
        /// <param name="authToken">Authentication token.</param>
        /// <param name="sessionId">Optional session ID for frame requests.</param>
        /// <returns>Transmission result with status and error information.</returns>
        private async Task<TransmissionResult> SendPostRequestAsync(string url, string jsonBody, string authToken, string sessionId)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                // Add authentication token header
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                }
                
                // Add session ID header for frame requests
                if (!string.IsNullOrEmpty(sessionId))
                {
                    request.SetRequestHeader("X-Session-Id", sessionId);
                }
                
                // Send request and await completion
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                // Process result
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return new TransmissionResult
                    {
                        Success = true,
                        StatusCode = (int)request.responseCode,
                        ErrorMessage = request.downloadHandler.text,
                        RetryCount = 0
                    };
                }
                else
                {
                    string errorMessage = $"{request.error} (Code: {request.responseCode})";
                    
                    if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        errorMessage += $" - {request.downloadHandler.text}";
                    }
                    
                    return new TransmissionResult
                    {
                        Success = false,
                        StatusCode = (int)request.responseCode,
                        ErrorMessage = errorMessage,
                        RetryCount = 0
                    };
                }
            }
        }
        
        /// <summary>
        /// Response structure for session initialization.
        /// </summary>
        private class SessionInitResponse
        {
            public string SessionId { get; set; }
            public bool Success { get; set; }
        }
    }
}
