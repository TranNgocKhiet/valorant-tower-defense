using TowerDefense.Streaming;
using UnityEngine;
using UnityEngine.SceneManagement;

// Note: I removed System.Diagnostics and System.Net.Mime because 
// they aren't needed for menu navigation and cause the 'Application' error.

public class MenuNavigator : MonoBehaviour
{
    public string streamUrl = "http://13.250.23.170:3000/active-streams.html";
    [Header("Stream Manager")]
    [SerializeField] private StreamManager streamManager;
    public void RetryLevel()
    {
        // 1. Reset the game speed to normal 
        // (Critical if the player died while paused or game over)
        Time.timeScale = 1f;

        // 2. Get the name of the current scene and reload it
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        Debug.Log("Reloading: " + currentSceneName);
    }

    public void GoToLevelSelect()
    {
        SceneManager.LoadScene("LevelChooseScene");
    }

    public void GoToLevelOne()
    {
        SceneManager.LoadScene("LevelOneScene");
    }

    public void GoToLevelTwo()
    {
        SceneManager.LoadScene("LevelTwoScene");
    }

    public void GoToLevelThree()
    {
        SceneManager.LoadScene("LevelThreeScene");
    }

    public void GoToLevelFour()
    {
        SceneManager.LoadScene("LevelFourScene");
    }

    public void GoToLevelFive()
    {
        SceneManager.LoadScene("LevelFiveScene");
    }

    public void GoToLogin()
    {
        // Terminate streaming session if active before logging out
        var streamManager = TowerDefense.Streaming.StreamManager.GetInstance();
        if (streamManager != null)
        {
            var currentState = streamManager.GetConnectionState();
            if (currentState == TowerDefense.Streaming.Core.ConnectionState.Streaming ||
                currentState == TowerDefense.Streaming.Core.ConnectionState.Connected ||
                currentState == TowerDefense.Streaming.Core.ConnectionState.Reconnecting)
            {
                Debug.Log("MenuNavigator: Stopping streaming session before logout");
                streamManager.StopStreaming();
            }
        }
        
        // Clear authentication tokens
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("LoginScene");
    }

    public void BackToHome()
    {
        SceneManager.LoadScene("HomeScene");
    }

    public void GoToSignup()
    {
        SceneManager.LoadScene("SignupScene");
    }

    public void QuitGame()
    {
        // Terminate streaming session if active before logging out
        var streamManager = TowerDefense.Streaming.StreamManager.GetInstance();
        if (streamManager != null)
        {
            var currentState = streamManager.GetConnectionState();
            if (currentState == TowerDefense.Streaming.Core.ConnectionState.Streaming ||
                currentState == TowerDefense.Streaming.Core.ConnectionState.Connected ||
                currentState == TowerDefense.Streaming.Core.ConnectionState.Reconnecting)
            {
                Debug.Log("MenuNavigator: Stopping streaming session before logout");
                streamManager.StopStreaming();
            }
        }

        UnityEngine.Debug.Log("Quit Button Pressed!");

        // We use UnityEngine.Application to tell Unity EXACTLY which one to use
        UnityEngine.Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    public void GoToStreamPage()
    {
        // This opens the URL in the user's default browser
        Application.OpenURL(streamUrl);
    }
}