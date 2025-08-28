using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameEvent
{
    public string game;
    public string mode;
    public string player;
    public string run;
    public string event_name;
    public Dictionary<string, object> data;
    public string timestamp;

    public GameEvent(
        string game,
        string mode,
        string player,
        string run,
        string event_name,
        Dictionary<string, object> data,
        string timestamp
    )
    {
        this.game = game;
        this.mode = mode;
        this.player = player;
        this.run = run;
        this.event_name = event_name;
        this.data = data;
        this.timestamp = timestamp;
    }

    // Custom JSON serialization method that can handle Dictionary
    public string ToJson()
    {
        var jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");

        jsonBuilder.Append($"\"game\":\"{game}\",");
        jsonBuilder.Append($"\"mode\":\"{mode}\",");
        jsonBuilder.Append($"\"player\":\"{player}\",");
        jsonBuilder.Append($"\"run\":\"{run}\",");
        jsonBuilder.Append($"\"event_name\":\"{event_name}\",");

        // Handle the data dictionary
        if (data != null && data.Count > 0)
        {
            jsonBuilder.Append("\"data\":{");
            bool first = true;
            foreach (var kvp in data)
            {
                if (!first)
                    jsonBuilder.Append(",");
                first = false;

                string key = kvp.Key;
                object value = kvp.Value;

                jsonBuilder.Append($"\"{key}\":");
                if (value is string)
                {
                    jsonBuilder.Append($"\"{value}\"");
                }
                else if (value is bool)
                {
                    jsonBuilder.Append(value.ToString().ToLower());
                }
                else if (value is int || value is float || value is double)
                {
                    jsonBuilder.Append(value.ToString());
                }
                else
                {
                    // For other types, convert to string
                    jsonBuilder.Append($"\"{value}\"");
                }
            }
            jsonBuilder.Append("}");
        }
        else
        {
            jsonBuilder.Append("\"data\":{}");
        }

        jsonBuilder.Append($",\"timestamp\":\"{timestamp}\"");
        jsonBuilder.Append("}");

        return jsonBuilder.ToString();
    }
}

[System.Serializable]
public class PlayerScore
{
    public string player_name;
    public int score;
    public string timestamp;
}

[System.Serializable]
public class PlayerScoreArray
{
    public PlayerScore[] items;
}

public class AnalyticsManager : Singleton<AnalyticsManager>
{
    [Header("Analytics Configuration")]
    [SerializeField]
    private string gameName = "eldritch_shores";

    [SerializeField]
    private bool persistRunId = false;

    private string playerId = "";

    private string currentRunId = "";

    private string currentMode = "";

    private const string API_URL = "https://game-analytics-server-79aae6a5062a.herokuapp.com/api";

    public static int MAX_HIGH_SCORES = 10;

    private const string ANALYTICS_DATA_FILENAME = "analytics_data.json";
    private string PlayerDataPath =>
        Path.Combine(Application.persistentDataPath, ANALYTICS_DATA_FILENAME);

    [System.Serializable]
    private class PlayerData
    {
        public string playerId;
        public string lastRunId;
    }

    protected override void Awake()
    {
        base.Awake();
        LoadOrGeneratePlayerId();
        SendEvent("game_opened");
    }

    private void LoadOrGeneratePlayerId()
    {
        if (File.Exists(PlayerDataPath))
        {
            try
            {
                string jsonData = File.ReadAllText(PlayerDataPath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(jsonData);
                playerId = data.playerId;
                currentRunId = persistRunId ? data.lastRunId : "";
                Debug.Log($"Loaded existing player ID: {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"Failed to load player data: {e.Message}. Generating new player ID."
                );
                GenerateNewPlayerId();
            }
        }
        else
        {
            GenerateNewPlayerId();
        }
    }

    private void GenerateNewPlayerId()
    {
        playerId = Guid.NewGuid().ToString();
        SavePlayerData();
        Debug.Log($"Generated new player ID: {playerId}");
    }

    private void SavePlayerData()
    {
        try
        {
            PlayerData data = new PlayerData { playerId = playerId, lastRunId = currentRunId };

            string jsonData = JsonUtility.ToJson(data, true);
            File.WriteAllText(PlayerDataPath, jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data: {e.Message}");
        }
    }

    public void StartNewRun(string mode)
    {
        currentMode = mode;
        currentRunId = Guid.NewGuid().ToString();
        SavePlayerData(); // Update the file with the new run ID
        Debug.Log($"Generated new run ID: {currentRunId}");
    }

    public void EndRun()
    {
        currentMode = "";
        currentRunId = "";
        SavePlayerData();
    }

    public void SendHighScoreEvent(
        int score,
        string playerName,
        Dictionary<string, object> eventData = null
    )
    {
        Dictionary<string, object> combinedEventData = new Dictionary<string, object>
        {
            { "score", score },
            { "player_name", playerName },
        };

        if (eventData != null)
        {
            foreach (var kvp in eventData)
            {
                combinedEventData.Add(kvp.Key, kvp.Value);
            }
        }

        SendEvent("high_score", combinedEventData);
    }

    public void SendEvent(string eventType, Dictionary<string, object> eventData = null)
    {
        var gameEvent = new GameEvent(
            gameName,
            currentMode,
            playerId,
            currentRunId,
            eventType,
            eventData,
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        );
        string jsonData = gameEvent.ToJson();

        Debug.Log($"Sending event: {gameEvent.event_name} with data: {jsonData}");

#if !DEBUG
        StartCoroutine(SendEventCoroutine(jsonData));
#endif
    }

    private IEnumerator SendEventCoroutine(string jsonData)
    {
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/events", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Event sent successfully!");
            }
            else
            {
                Debug.LogError($"Failed to send event:");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }

    public void FetchHighScores(
        string mode,
        System.Action<List<PlayerScore>> onSuccess,
        System.Action<string> onError = null
    )
    {
        StartCoroutine(FetchHighScoresCoroutine(mode, onSuccess, onError));
    }

    private List<PlayerScore> ParseHighScoresFromJson(string jsonResponse)
    {
        string wrappedJson = "{\"items\":" + jsonResponse + "}";
        PlayerScoreArray scoreArray = JsonUtility.FromJson<PlayerScoreArray>(wrappedJson);
        return new List<PlayerScore>(scoreArray.items);
    }

    private IEnumerator FetchHighScoresCoroutine(
        string mode,
        System.Action<List<PlayerScore>> onSuccess,
        System.Action<string> onError
    )
    {
        string url = $"{API_URL}/scores/{gameName}/{mode}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    List<PlayerScore> highScores = ParseHighScoresFromJson(jsonResponse);
                    onSuccess?.Invoke(highScores.Take(MAX_HIGH_SCORES).ToList());
                }
                catch (System.Exception e)
                {
                    string errorMsg = $"Failed to parse high scores response: {e.Message}";
                    Debug.LogError(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
            else
            {
                string errorMsg = $"Failed to fetch high scores: {request.error}";
                Debug.LogError(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }
}
