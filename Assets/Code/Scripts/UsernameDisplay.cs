using UnityEngine;
using TMPro;

public class UsernameDisplay : MonoBehaviour
{
    public TextMeshProUGUI welcomeText;

    void Start()
    {
        // Retrieve the name we saved in the Login scene
        string username = PlayerPrefs.GetString("Username", "Agent");

        // Display it (e.g., "WELCOME, PLAYER01")
        if (welcomeText != null)
        {
            welcomeText.text = "Greeting, agent " + username;
        }
    }
}