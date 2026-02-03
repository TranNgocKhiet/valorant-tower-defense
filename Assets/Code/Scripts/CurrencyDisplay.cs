using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI radianiteText;

    private void Start()
    {
        // This runs as soon as the Level Selection scene loads
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        // Retrieve the data that was saved into PlayerPrefs during the Login scene
        int totalRadianite = PlayerPrefs.GetInt("TotalRadianite", 0);

        if (radianiteText != null)
        {
            radianiteText.text = totalRadianite.ToString();
        }

        Debug.Log("Currency Display refreshed from PlayerPrefs: " + totalRadianite);
    }
}