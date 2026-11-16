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
    public bool Exists(string fileName) => File.Exists(SavePath(fileName));
    public void Delete(string fileName)
    {
        if(Exists(fileName)) File.Delete(SavePath(fileName));
    }
    public string SavePath(string fileName)
    {
        // 멀티 테스트용 인스턴스는 프로필별 폴더에 저장. 기본 프로필은 기존과 같은 persistentDataPath
        return $"{MultiplayerTestProfile.SaveDirectory}/{fileName}";
    }
}