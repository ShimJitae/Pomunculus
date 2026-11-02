using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public interface ISaveData
{
    public T GetData<T>();
    public void SetData<T>(T value);
    public JToken Data { get; }
    public Dictionary<string, SaveData> Child { get;}
    public int Version{get;}
}