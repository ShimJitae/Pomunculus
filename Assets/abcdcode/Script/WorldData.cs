using System.Collections.Generic;
using UnityEngine;

public class WorldData
{
    public int Day{get;set;}
    public float Time{get;set;}
    public int NextId{get;set;} // 다음에 발급할 IdManager id

    public Dictionary<string, SaveData> Players{get;set;} = new(); // key : UID, value : Player가 ISavable.SaveData로 채운 상태
    public List<WorldObjSaveData> Entities{get;set;} = new();

    public void SavePlayer(string uid, ISavable player)
    {
        var d = new SaveData();
        player.SaveData(d);
        Players[uid] = d;
    }
    public bool LoadPlayer(string uid, ISavable player)
    {
        if(!Players.TryGetValue(uid, out var d)) return false; // 처음 입장한 플레이어
        player.LoadData(d);
        return true;
    }

    public void SaveEntities(IEnumerable<IWorldObj> objs)
    {
        Entities.Clear();
        foreach(var obj in objs) Entities.Add(WorldObjSaveData.Create(obj));
    }
}

// 입장한 플레이어에게 보내는 데이터. 다른 플레이어의 저장 정보는 보내지 않는다
public class WorldSnapshot
{
    public int Day{get;set;}
    public float Time{get;set;}
    public List<WorldObjSaveData> Entities{get;set;} = new();
    public SaveData Player{get;set;} // 입장한 플레이어 본인의 저장 데이터, 없으면 null
}

// 엔티티 상태 자체는 ISavable에 맡기고, 여기엔 다시 생성하는 데 필요한 정보만 둔다
public class WorldObjSaveData
{
    public int ID{get;set;} // IdManager 발급 id
    public string PrefabId{get;set;} // NetworkPrefabCatalog 이름
    public SerialVector3 Position{get;set;}
    public SerialVector3 Rotation{get;set;} // eulerAngles
    public SaveData State{get;set;} // Entity가 ISavable.SaveData로 채운 상태

    public static WorldObjSaveData Create(IWorldObj obj)
    {
        var state = new SaveData();
        obj.SaveData(state);
        var t = obj.gameObject.transform;
        return new()
        {
            ID = obj.ID,
            PrefabId = obj.PrefabId,
            Position = t.position,
            Rotation = t.eulerAngles,
            State = state
        };
    }
}

// Vector3를 Newtonsoft로 직접 직렬화하면 normalized 등 프로퍼티 순환참조 문제가 생겨서 별도 구조체로 저장
public struct SerialVector3
{
    public float x{get;set;}
    public float y{get;set;}
    public float z{get;set;}

    public SerialVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    public Vector3 ToVector3() => new Vector3(x, y, z);

    public static implicit operator SerialVector3(Vector3 v) => new SerialVector3(v);
    public static implicit operator Vector3(SerialVector3 v) => v.ToVector3();
}
