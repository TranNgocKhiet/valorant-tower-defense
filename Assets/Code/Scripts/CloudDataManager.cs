using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudDataManager : MonoBehaviour
{
    public static CloudDataManager Instance { get; private set; }

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // This makes the object persist across scenes
        DontDestroyOnLoad(gameObject);
    }

    private string apiURL = "https://ptf00pe25j.execute-api.ap-southeast-1.amazonaws.com/save";

    // This structure matches what your Python Lambda expects
    [System.Serializable]
    public class PlayerData
    {
        public string PlayerID;
        public int Radianite;
        public int MaxLevel;
    }

    public void SaveGameProgress(int currentRadianite, int reachedLevel)
    {
        // Use "Username" instead of "PlayerIDToken" to keep the database readable
        string playerID = PlayerPrefs.GetString("Username", "UnknownPlayer");

        PlayerData data = new PlayerData
        {
            PlayerID = playerID,
            Radianite = currentRadianite,
            MaxLevel = reachedLevel
        };

        string json = JsonUtility.ToJson(data);
        Debug.Log("Attempting to save JSON: " + json); // Check your console for this!
        StartCoroutine(PostDataCoroutine(json));
    }

    IEnumerator PostDataCoroutine(string jsonData)
    {
        using (UnityWebRequest request = new UnityWebRequest(apiURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Cloud Save Successful: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Cloud Save Failed: " + request.error);
            }
        }
    }

    public void LoadGameProgress()
    {
        // Use the same key used in SaveGameProgress and Login
        string playerID = PlayerPrefs.GetString("Username", "UnknownPlayer");

        // Construct the URL with the query parameter for the GET request
        // Ensure your apiURL ends correctly or use a dedicated string for loading
        string loadURL = apiURL.Replace("/save", "/player-progress") + "?PlayerID=" + playerID;

        Debug.Log("Loading progress from: " + loadURL);
        StartCoroutine(GetDataCoroutine(loadURL));
    }

    IEnumerator GetDataCoroutine(string url)
    {
        // Use the .Get helper to ensure DownloadHandler is attached automatically
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            Debug.Log("Request Created. Sending...");

            // This is the line where it likely hangs
            yield return request.SendWebRequest();

            Debug.Log("Request Returned! Checking result...");

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawJson = request.downloadHandler.text;
                Debug.Log("Raw JSON received: " + rawJson);

                try
                {
                    // 1. Parse directly into PlayerData (No ProxyResponse needed!)
                    PlayerData loadedData = JsonUtility.FromJson<PlayerData>(rawJson);

                    if (loadedData == null || string.IsNullOrEmpty(loadedData.PlayerID))
                    {
                        Debug.LogError("Failed to parse PlayerData. Check if field names match!");
                        yield break;
                    }

                    Debug.Log("Successfully loaded Radianite: " + loadedData.Radianite);

                    // 2. Save to PlayerPrefs
                    PlayerPrefs.SetInt("TotalRadianite", loadedData.Radianite);
                    PlayerPrefs.SetInt("MaxLevel", loadedData.MaxLevel);
                    PlayerPrefs.Save();

                    // 3. Refresh UI
                    CurrencyDisplay display = FindObjectOfType<CurrencyDisplay>();
                    if (display != null) display.UpdateDisplay();

                    Debug.Log("Sync Complete!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON Parsing Error: " + e.Message);
                }
            }
        }
    }

    // Add this class at the bottom of your script to handle the AWS Wrapper
    [System.Serializable]
    public class ProxyResponse
    {
        public int statusCode;
        public string body;
    }
}