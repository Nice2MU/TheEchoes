using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private string savePath;
    private List<SavaObject> SavaObject = new List<SavaObject>();

    private void Awake()
    {
        savePath = Application.persistentDataPath + "/global_save.dat";
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SavaObject.Clear();
        SavaObject.AddRange(FindObjectsOfType<SavaObject>());
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

    public void SaveScene()
    {
        Dictionary<string, ObjectData> saveData = new Dictionary<string, ObjectData>();
        foreach (var obj in SavaObject)
        {
            saveData[obj.UniqueID] = obj.GetData();
        }

        using (FileStream stream = new FileStream(savePath, FileMode.Create))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, saveData);
        }
    }

    public void LoadScene()
    {
        if (!File.Exists(savePath))
        {
            return;
        }

        using (FileStream stream = new FileStream(savePath, FileMode.Open))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            var saveData = formatter.Deserialize(stream) as Dictionary<string, ObjectData>;

            foreach (var obj in SavaObject)
            {
                if (saveData.TryGetValue(obj.UniqueID, out var data))
                {
                    obj.LoadData(data);
                }
            }
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        else
        {

        }
    }
}