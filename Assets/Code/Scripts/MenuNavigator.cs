using UnityEngine;
using UnityEngine.SceneManagement;

// Note: I removed System.Diagnostics and System.Net.Mime because 
// they aren't needed for menu navigation and cause the 'Application' error.

public class MenuNavigator : MonoBehaviour
{
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

    public void GoToLogin()
    {
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
        UnityEngine.Debug.Log("Quit Button Pressed!");

        // We use UnityEngine.Application to tell Unity EXACTLY which one to use
        UnityEngine.Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}