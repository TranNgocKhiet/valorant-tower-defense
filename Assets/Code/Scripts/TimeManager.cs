using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TimeController : MonoBehaviour
{
    public GameObject pauseMenuPanel; // Assign your Pause UI Panel here
    private bool isPaused = false;
    private float lastTimeScale = 1f;

    void Update()
    {
        // Toggle pause with the Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // --- TIME SPEED FUNCTIONS ---
    public void SetSpeedNormal() => SetTime(1f);
    public void SetSpeedX2() => SetTime(2f);
    public void SetSpeedX3() => SetTime(3f);

    private void SetTime(float scale)
    {
        if (!isPaused)
        {
            Time.timeScale = scale;
            lastTimeScale = scale; // Remember speed for when we resume
        }
    }

    public TextMeshProUGUI speedButtonText; // Drag your button's text here
    private int currentSpeedMode = 1; // 1 = 1x, 2 = 2x, 3 = 3x

    public void CycleGameSpeed()
    {
        currentSpeedMode++;

        if (currentSpeedMode > 3)
        {
            currentSpeedMode = 1;
        }

        switch (currentSpeedMode)
        {
            case 1:
                Time.timeScale = 1f;
                speedButtonText.text = ">"; // Normal
                break;
            case 2:
                Time.timeScale = 2f;
                speedButtonText.text = ">>"; // Fast
                break;
            case 3:
                Time.timeScale = 3f;
                speedButtonText.text = ">>>"; // Super Fast
                break;
        }

        Debug.Log("Current Speed: " + Time.timeScale + "x");
    }

    // --- PAUSE LOGIC ---
    public void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Freeze everything
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = lastTimeScale; // Return to whatever speed it was
    }

    public void BackToLevelSelect()
    {
        Time.timeScale = 1f; // IMPORTANT: Always reset time before changing scenes
        SceneManager.LoadScene("LevelChooseScene");
    }
}