using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Streaming;
using TowerDefense.Streaming.Core;

/// <summary>
/// Manages logout functionality including stream termination and cleanup.
/// </summary>
public class LogoutManager : MonoBehaviour
{
    /// <summary>
    /// Performs a complete logout:
    /// 1. Terminates active streaming session
    /// 2. Clears authentication tokens
    /// 3. Clears player preferences (optional)
    /// 4. Returns to login scene
    /// </summary>
    public void Logout()
    {
        Debug.Log("LogoutManager: Starting logout process");
        
        // Step 1: Terminate streaming session if active
        TerminateStreamingSession();
        
        // Step 2: Clear authentication tokens
        ClearAuthenticationData();
        
        // Step 3: Navigate to login scene
        SceneManager.LoadScene("LoginScene");
        
        Debug.Log("LogoutManager: Logout complete");
    }
    
    /// <summary>
    /// Terminates the streaming session if one is active.
    /// </summary>
    private void TerminateStreamingSession()
    {
        var streamManager = StreamManager.GetInstance();
        if (streamManager == null)
        {
            Debug.Log("LogoutManager: No StreamManager instance found");
            return;
        }
        
        var currentState = streamManager.GetConnectionState();
        Debug.Log($"LogoutManager: Current streaming state: {currentState}");
        
        // Check if streaming is active
        if (currentState == ConnectionState.Streaming ||
            currentState == ConnectionState.Connected ||
            currentState == ConnectionState.Reconnecting)
        {
            Debug.Log("LogoutManager: Stopping active streaming session");
            streamManager.StopStreaming();
            
            // Give it a moment to clean up
            // In a production app, you might want to wait for confirmation
            System.Threading.Thread.Sleep(500);
        }
        else
        {
            Debug.Log($"LogoutManager: No active streaming session (state: {currentState})");
        }
    }
    
    /// <summary>
    /// Clears authentication tokens and related data.
    /// </summary>
    private void ClearAuthenticationData()
    {
        Debug.Log("LogoutManager: Clearing authentication data");
        
        // Clear refresh token (used for AWS Cognito)
        if (PlayerPrefs.HasKey("RefreshToken"))
        {
            PlayerPrefs.DeleteKey("RefreshToken");
            Debug.Log("LogoutManager: Cleared RefreshToken");
        }
        
        // Optionally clear username (uncomment if you want to clear it)
        // if (PlayerPrefs.HasKey("Username"))
        // {
        //     PlayerPrefs.DeleteKey("Username");
        //     Debug.Log("LogoutManager: Cleared Username");
        // }
        
        // Save changes
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Quick logout without clearing player progress.
    /// Only clears authentication and stops streaming.
    /// </summary>
    public void QuickLogout()
    {
        Debug.Log("LogoutManager: Quick logout (preserving player progress)");
        
        TerminateStreamingSession();
        
        // Only clear authentication tokens, keep player progress
        if (PlayerPrefs.HasKey("RefreshToken"))
        {
            PlayerPrefs.DeleteKey("RefreshToken");
        }
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("LoginScene");
    }
    
    /// <summary>
    /// Full logout that also clears player progress.
    /// Use with caution!
    /// </summary>
    public void FullLogout()
    {
        Debug.Log("LogoutManager: Full logout (clearing all data)");
        
        TerminateStreamingSession();
        
        // Clear all PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("LoginScene");
    }
}
