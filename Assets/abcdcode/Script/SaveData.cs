using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SaveData : ISaveData
{
    public static SaveData CreateSaveData<T>(T value)
    {
        SaveData d = new SaveData();
        d.SetData(value);
        return d;
    }
    public SaveData()
    {
        Child = new Dictionary<string, SaveData>();
        Version = 0;
        if(GameManager.Instance != null)
        {
            Version = GameManager.Instance.Version;
        }
    }
    public int Version{get;private set;}
    public JToken Data { get; set; }
    public Dictionary<string, SaveData> Child { get; set; }

    public T GetData<T>() => Data.ToObject<T>(Serializer);
    public void SetData<T>(T value) => Data = JToken.FromObject(value, Serializer);

    static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings();
}