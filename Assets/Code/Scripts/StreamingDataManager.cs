using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TowerDefense.Streaming.Models;

/// <summary>
/// Manages streaming session data in DynamoDB.
/// Handles stream domain generation, active stream storage, and cleanup.
/// </summary>
public class StreamingDataManager : MonoBehaviour
{
    public static StreamingDataManager Instance { get; private set; }

    private string apiURL = "https://ptf00pe25j.execute-api.ap-southeast-1.amazonaws.com";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GenerateStreamDomain(string username)
    {
        string timestamp = DateTime.UtcNow.Ticks.ToString();
        string domain = $"stream-{username}-{timestamp.Substring(timestamp.Length - 8)}";
        return domain.ToLower();
    }

    public void SaveStreamInfo(string streamDomain, string sessionId, int currentLevel)
    {
        string playerID = PlayerPrefs.GetString("Username", "UnknownPlayer");

        StreamInfo streamInfo = new StreamInfo
        {
            StreamDomain = streamDomain,
            PlayerID = playerID,
            StreamStartTime = DateTime.UtcNow.ToString("o"),
            CurrentLevel = currentLevel,
            Status = "active",
            SessionId = sessionId
        };

        string json = JsonUtility.ToJson(streamInfo);
        Debug.Log("Saving stream info: " + json);
        StartCoroutine(SaveStreamCoroutine(json));
    }

    public void DeleteStreamInfo(string streamDomain)
    {
        string playerID = PlayerPrefs.GetString("Username", "UnknownPlayer");
        Debug.Log($"Deleting stream info for domain: {streamDomain}");
        StartCoroutine(DeleteStreamCoroutine(streamDomain, playerID));
    }

    public void GetActiveStreams(Action<StreamInfo[]> callback)
    {
        StartCoroutine(GetActiveStreamsCoroutine(callback));
    }

    private IEnumerator SaveStreamCoroutine(string jsonData)
    {
        string url = apiURL + "/stream-info";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Stream info saved successfully: " + request.downloadHandler.text);
            else
                Debug.LogError("Failed to save stream info: " + request.error);
        }
    }

    private IEnumerator DeleteStreamCoroutine(string streamDomain, string playerID)
    {
        string url = $"{apiURL}/stream-info?StreamDomain={streamDomain}&PlayerID={playerID}";

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Stream info deleted successfully");
            else
                Debug.LogError("Failed to delete stream info: " + request.error);
        }
    }

    private IEnumerator GetActiveStreamsCoroutine(Action<StreamInfo[]> callback)
    {
        string url = apiURL + "/active-streams";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawJson = request.downloadHandler.text;
                Debug.Log("Active streams received: " + rawJson);

                try
                {
                    StreamInfoList streamList = JsonUtility.FromJson<StreamInfoList>(rawJson);
                    callback?.Invoke(streamList.streams);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse active streams: " + e.Message);
                    callback?.Invoke(new StreamInfo[0]);
                }
            }
            else
            {
                Debug.LogError("Failed to get active streams: " + request.error);
                callback?.Invoke(new StreamInfo[0]);
            }
        }
    }

    [Serializable]
    private class StreamInfoList
    {
        public StreamInfo[] streams;
    }
}
