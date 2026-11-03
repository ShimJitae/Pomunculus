using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    public void Save(string fileName, SaveData data)
    {
        var json = JsonConvert.SerializeObject(data);
        File.WriteAllText(SavePath(fileName),json);
    }

    public SaveData Load(string fileName)
    {
        if(!File.Exists(SavePath(fileName))) return null;
        var json = File.ReadAllText(SavePath(fileName));
        var data = JsonConvert.DeserializeObject<SaveData>(json);
        return data;
    }
    public string SavePath(string fileName)
    {
        return $"{Application.persistentDataPath}/{fileName}";
    }
}