using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class WorldData
{
    public int Day{get;set;}
    public float Time{get;set;}
    public int NextId{get;set;} // 다음에 발급할 IdManager id

    public Dictionary<string, PlayerSaveData> Players{get;set;} = new(); // key : UID
    public List<WorldObjSaveData> Entities{get;set;} = new();
    // RegionSpawner 소환 기록
    public Dictionary<RegionType, RegionSpawnRecord> RegionGathers{get;set;} = new();
    public Dictionary<RegionType, RegionSpawnRecord> RegionMobs{get;set;} = new();

    public void SavePlayer(string uid, IWorldObj player)
    {
        var state = new SaveData();
        player.SaveData(state);
        var t = player.gameObject.transform;
        Players[uid] = new PlayerSaveData { Position = t.position, Rotation = t.eulerAngles, State = state };
    }
    public bool LoadPlayer(string uid, IWorldObj player)
    {
        if(!Players.TryGetValue(uid, out var d) || d?.State == null) return false; // 처음 입장한 플레이어 or 위치 저장 전 세이브
        // NavMeshAgent가 켜져 있으면 transform을 직접 옮겨도 에이전트가 되돌리므로 Warp로 이동
        var go = player.gameObject;
        if(go.TryGetComponent(out NavMeshAgent agent) && agent.enabled) agent.Warp(d.Position);
        else go.transform.position = d.Position;
        go.transform.eulerAngles = d.Rotation;
        player.LoadData(d.State);
        return true;
    }

    public void SaveEntities(IEnumerable<IWorldObj> objs)
    {
        Entities.Clear();
        foreach(var obj in objs) 
        {
            try{
            if(obj.gameObject.IsDestroyed()) continue;
            }catch(Exception e)
            {
                continue;
            }
            Entities.Add(WorldObjSaveData.Create(obj));
        }
    }
}

// 플레이어는 UID 기준으로 월드 엔티티와 따로 저장하므로, 다시 들어왔을 때 놓을 위치도 같이 둔다
public class PlayerSaveData
{
    public SerialVector3 Position{get;set;}
    public SerialVector3 Rotation{get;set;} // eulerAngles
    public SaveData State{get;set;} // Player가 ISavable.SaveData로 채운 상태
}

// 지역 하나의 채집물 또는 몬스터 소환 기록. 날짜가 바뀌기 전까지는 이 기록대로만 다시 소환한다
public class RegionSpawnRecord
{
    public int Day{get;set;} // 소환한 날
    public bool Active{get;set;} // 오브젝트가 월드에 나와 있는지
    public List<SpawnPointRecord> Points{get;set;} = new(); // SpawnPointRegistry의 포인트 순서
}
public class SpawnPointRecord
{
    public string PrefabId{get;set;}
    public int ObjectId{get;set;} = -1; // 나와 있는 오브젝트 ID. 없으면 -1
    public bool Consumed{get;set;} // 오늘 이미 채집/처치되어 사라짐
    public SaveData State{get;set;} // 디스폰할 때 보관한 상태. 다시 소환할 때 적용
}

// 입장한 플레이어에게 보내는 현재 월드 상태. 플레이어별 저장 정보(WorldData.Players)는 보내지 않는다
public class WorldSnapshot
{
    public int Day{get;set;}
    public float Time{get;set;}
    public List<WorldObjSaveData> Entities{get;set;} = new();

    // 네트워크 전송용 바이너리
    public void Write(PacketData p)
    {
        p.Write(Day);
        p.Write(Time);
        p.Write(Entities.Count);
        foreach(var e in Entities) e.Write(p);
    }
    public static WorldSnapshot Read(PacketData p)
    {
        var s = new WorldSnapshot { Day = p, Time = p };
        int count = p;
        for(int i = 0; i < count; i++) s.Entities.Add(WorldObjSaveData.Read(p));
        return s;
    }
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

    // 네트워크 전송용 바이너리. 다시 생성하는 데 필요한 값은 고정 크기로 쓰고,
    // 상태(SaveData)는 Entity마다 형태가 달라서 JSON 문자열로 둔다
    public void Write(PacketData p)
    {
        p.Write(ID);
        p.Write(PrefabId);
        p.Write(Position.ToVector3());
        p.Write(Rotation.ToVector3());
        p.WriteSaveData(State);
    }
    public static WorldObjSaveData Read(PacketData p) => new()
    {
        ID = p,
        PrefabId = p,
        Position = p.ReadVector3(),
        Rotation = p.ReadVector3(),
        State = p.ReadSaveData()
    };
}

public static class SaveDataPacketExtensions
{
    // null이면 bool 하나만 씀
    public static void WriteSaveData(this PacketData p, SaveData d)
    {
        p.Write(d != null);
        if(d != null) p.Write(JsonConvert.SerializeObject(d));
    }
    public static SaveData ReadSaveData(this PacketData p)
    {
        return p.ReadBool() ? JsonConvert.DeserializeObject<SaveData>(p.ReadString()) : null;
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
