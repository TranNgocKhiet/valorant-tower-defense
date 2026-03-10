using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using System;
using System.Threading.Tasks;
using TowerDefense.Streaming.Interfaces;
using UnityEngine;

namespace TowerDefense.Streaming.Services
{
    /// <summary>
    /// Manages authentication tokens for streaming sessions using AWS Cognito.
    /// Integrates with the existing AWS authentication system to retrieve and refresh tokens.
    /// </summary>
    public class AuthManager : IAuthManager
    {
        private readonly string _userPoolId;
        private readonly string _clientId;
        private readonly RegionEndpoint _region;
        
        private AmazonCognitoIdentityProviderClient _provider;
        private CognitoUserPool _userPool;
        private CognitoUser _currentUser;
        
        private string _cachedIdToken;
        private DateTime _tokenExpirationTime;
        
        private const int TOKEN_EXPIRATION_BUFFER_MINUTES = 5;

        /// <summary>
        /// Initializes a new instance of the AuthManager with default configuration.
        /// Retrieves AWS configuration from the existing AWSLoginManager.
        /// </summary>
        public AuthManager() : this(
            GetUserPoolIdFromAWSLoginManager(),
            GetClientIdFromAWSLoginManager(),
            GetRegionFromAWSLoginManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthManager.
        /// </summary>
        /// <param name="userPoolId">AWS Cognito User Pool ID</param>
        /// <param name="clientId">AWS Cognito Client ID</param>
        /// <param name="region">AWS Region</param>
        public AuthManager(string userPoolId, string clientId, RegionEndpoint region)
        {
            _userPoolId = userPoolId;
            _clientId = clientId;
            _region = region;
            
            InitializeAWSClient();
        }

        /// <summary>
        /// Initializes the AWS Cognito client and user pool.
        /// </summary>
        private void InitializeAWSClient()
        {
            _provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), _region);
            _userPool = new CognitoUserPool(_userPoolId, _clientId, _provider);
        }

        /// <summary>
        /// Asynchronously retrieves a valid authentication token.
        /// If the cached token is valid, returns it. Otherwise, attempts to refresh.
        /// </summary>
        /// <returns>A valid authentication token string.</returns>
        public async Task<string> GetAuthTokenAsync()
        {
            // Return cached token if still valid
            if (IsTokenValid() && !string.IsNullOrEmpty(_cachedIdToken))
            {
                return _cachedIdToken;
            }

            // Try to refresh the token
            bool refreshed = await RefreshTokenAsync();
            
            if (refreshed && !string.IsNullOrEmpty(_cachedIdToken))
            {
                return _cachedIdToken;
            }

            throw new InvalidOperationException("Unable to retrieve valid authentication token. User may need to log in again.");
        }

        /// <summary>
        /// Asynchronously refreshes the current authentication token.
        /// Retrieves the current user from PlayerPrefs and attempts to get a new session.
        /// </summary>
        /// <returns>True if refresh was successful, false otherwise.</returns>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // Get the username and refresh token from PlayerPrefs (set during login)
                string username = PlayerPrefs.GetString("Username", "");
                string refreshToken = PlayerPrefs.GetString("RefreshToken", "");
                
                if (string.IsNullOrEmpty(username))
                {
                    Debug.LogError("AuthManager: No username found in PlayerPrefs. User must log in first.");
                    return false;
                }

                if (string.IsNullOrEmpty(refreshToken))
                {
                    Debug.LogError("AuthManager: No refresh token found in PlayerPrefs. User must log in again to obtain a refresh token.");
                    return false;
                }

                // Reinitialize AWS client to ensure fresh connection
                InitializeAWSClient();
                
                // Create CognitoUser instance
                _currentUser = new CognitoUser(username, _clientId, _userPool, _provider);
                
                // Set the refresh token on the user's session
                _currentUser.SessionTokens = new CognitoUserSession(
                    null, // IdToken - will be refreshed
                    null, // AccessToken - will be refreshed
                    refreshToken, // RefreshToken - from PlayerPrefs
                    DateTime.UtcNow, // IssuedTime
                    DateTime.UtcNow.AddHours(1) // ExpirationTime - will be updated after refresh
                );

                // Attempt to start a session with refresh token
                var authResponse = await _currentUser.StartWithRefreshTokenAuthAsync(new InitiateRefreshTokenAuthRequest
                {
                    AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
                });
                
                if (authResponse != null && authResponse.AuthenticationResult != null)
                {
                    _cachedIdToken = authResponse.AuthenticationResult.IdToken;
                    
                    // Set expiration time (Cognito tokens typically expire in 1 hour)
                    // We'll set it to 55 minutes to have a buffer for refresh
                    _tokenExpirationTime = DateTime.UtcNow.AddMinutes(55);
                    
                    Debug.Log("AuthManager: Token refreshed successfully");
                    return true;
                }
                else
                {
                    Debug.LogWarning("AuthManager: Failed to refresh token - no authentication result");
                    return false;
                }
            }
            catch (NotAuthorizedException ex)
            {
                Debug.LogError($"AuthManager: Not authorized to refresh token. User may need to log in again. Error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AuthManager: Failed to refresh token. Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the current token is valid and not expired.
        /// </summary>
        /// <returns>True if token is valid, false otherwise.</returns>
        public bool IsTokenValid()
        {
            // Check if we have a cached token
            if (string.IsNullOrEmpty(_cachedIdToken))
            {
                return false;
            }

            // Check if token has expired (with buffer)
            if (DateTime.UtcNow >= _tokenExpirationTime.AddMinutes(-TOKEN_EXPIRATION_BUFFER_MINUTES))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clears the cached token. Useful for logout scenarios.
        /// </summary>
        public void ClearToken()
        {
            _cachedIdToken = null;
            _tokenExpirationTime = DateTime.MinValue;
            _currentUser = null;
        }

        /// <summary>
        /// Retrieves the User Pool ID from the existing AWSLoginManager.
        /// </summary>
        private static string GetUserPoolIdFromAWSLoginManager()
        {
            var awsLoginManager = UnityEngine.Object.FindObjectOfType<AWSLoginManager>();
            if (awsLoginManager != null)
            {
                return awsLoginManager.UserPoolId;
            }
            
            // Fallback to default value if AWSLoginManager not found
            return "ap-southeast-1_OvQ3GmgFG";
        }

        /// <summary>
        /// Retrieves the Client ID from the existing AWSLoginManager.
        /// </summary>
        private static string GetClientIdFromAWSLoginManager()
        {
            var awsLoginManager = UnityEngine.Object.FindObjectOfType<AWSLoginManager>();
            if (awsLoginManager != null)
            {
                return awsLoginManager.ClientId;
            }
            
            // Fallback to default value if AWSLoginManager not found
            return "5r2q9cc236gng4437us7mhj82b";
        }

        /// <summary>
        /// Retrieves the Region from the existing AWSLoginManager.
        /// </summary>
        private static RegionEndpoint GetRegionFromAWSLoginManager()
        {
            var awsLoginManager = UnityEngine.Object.FindObjectOfType<AWSLoginManager>();
            if (awsLoginManager != null)
            {
                return awsLoginManager.Region;
            }
            
            // Fallback to default region if AWSLoginManager not found
            return RegionEndpoint.APSoutheast1;
        }
    }
}
