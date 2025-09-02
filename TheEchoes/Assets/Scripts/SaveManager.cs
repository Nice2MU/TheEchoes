using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private List<ObjectSave> ObjectSave = new List<ObjectSave>();
    private const string SaveKeyPrefix = "WebGL_GlobalSave";
    private const int ChunkSize = 100000;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ObjectSave.Clear();
        ObjectSave.AddRange(FindObjectsByType<ObjectSave>(FindObjectsSortMode.None));
        LoadScene();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaveScene();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSave();
        }
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public List<string> keys = new List<string>();
        public List<ObjectData> values = new List<ObjectData>();
    }

    public void SaveScene()
    {
        SaveWrapper wrapper = new SaveWrapper();

        foreach (var obj in ObjectSave)
        {
            wrapper.keys.Add(obj.UniqueID);
            wrapper.values.Add(obj.GetData());
        }

        string fullJson = JsonUtility.ToJson(wrapper);
        SaveLargeData(SaveKeyPrefix, fullJson);
    }

    public void LoadScene()
    {
        string fullJson = LoadLargeData(SaveKeyPrefix);
        if (string.IsNullOrEmpty(fullJson)) return;

        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(fullJson);

        for (int i = 0; i < wrapper.keys.Count; i++)
        {
            string id = wrapper.keys[i];
            ObjectData data = wrapper.values[i];

            foreach (var obj in ObjectSave)
            {
                if (obj.UniqueID == id)
                {
                    obj.LoadData(data);
                    break;
                }
            }
        }
    }

    public void DeleteSave()
    {
        if (PlayerPrefs.HasKey(SaveKeyPrefix + "_Chunks"))
        {
            int chunkCount = PlayerPrefs.GetInt(SaveKeyPrefix + "_Chunks");
            for (int i = 0; i < chunkCount; i++)
            {
                PlayerPrefs.DeleteKey(SaveKeyPrefix + "_Chunk_" + i);
            }
            PlayerPrefs.DeleteKey(SaveKeyPrefix + "_Chunks");
        }

        PlayerPrefs.Save();
    }

    private void SaveLargeData(string baseKey, string fullJson)
    {
        int totalChunks = Mathf.CeilToInt((float)fullJson.Length / ChunkSize);
        PlayerPrefs.SetInt(baseKey + "_Chunks", totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            string chunk = fullJson.Substring(i * ChunkSize, Mathf.Min(ChunkSize, fullJson.Length - i * ChunkSize));
            PlayerPrefs.SetString(baseKey + "_Chunk_" + i, chunk);
        }

        PlayerPrefs.Save();
    }

    private string LoadLargeData(string baseKey)
    {
        if (!PlayerPrefs.HasKey(baseKey + "_Chunks")) return null;

        int totalChunks = PlayerPrefs.GetInt(baseKey + "_Chunks");
        string fullJson = "";

        for (int i = 0; i < totalChunks; i++)
        {
            fullJson += PlayerPrefs.GetString(baseKey + "_Chunk_" + i);
        }

        return fullJson;
    }
}