using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class WorldData
{
    public int Day{get;set;}
    public float Time{get;set;}
    public int NextId{get;set;} // 다음에 발급할 IdManager id

    // 계절은 Day로 계산하므로 따로 저장/전송하지 않음 (Day 0 = 1일차)
    public const int SeasonLength = 10; // 계절 하나의 일수
    [JsonIgnore] public Season Season => GetSeason(Day);
    public static Season GetSeason(int day) => (Season)(Math.Max(day, 0) / SeasonLength % SeasonCount);
    private const int SeasonCount = 4;

    public Dictionary<string, PlayerSaveData> Players{get;set;} = new(); // key : UID
    public List<WorldObjSaveData> Entities{get;set;} = new();
    // RegionSpawner 소환 기록
    public Dictionary<RegionType, RegionSpawnRecord> RegionGathers{get;set;} = new();
    public Dictionary<RegionType, RegionSpawnRecord> RegionMobs{get;set;} = new();
    public Dictionary<RegionType, RegionSpawnRecord> RegionBosses{get;set;} = new();
    public FinalBossRecord FinalBoss{get;set;} = new(); // 결계 -> 최종보스 진행 기록

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

// 봄에서 시작해 SeasonLength일마다 다음 계절로, 겨울 다음은 다시 봄
public enum Season
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3,
}

// 플레이어는 UID 기준으로 월드 엔티티와 따로 저장하므로, 다시 들어왔을 때 놓을 위치도 같이 둔다
public class PlayerSaveData
{
    public SerialVector3 Position{get;set;}
    public SerialVector3 Rotation{get;set;} // eulerAngles
    public SaveData State{get;set;} // Player가 ISavable.SaveData로 채운 상태
}

// 지역 하나의 채집물/몬스터/보스 소환 기록. 재소환 주기가 지나기 전까지는 이 기록대로만 다시 소환한다
public class RegionSpawnRecord
{
    public int Day{get;set;} // 마지막으로 날짜 갱신을 처리한 날
    public bool Active{get;set;} // 오브젝트가 월드에 나와 있는지
    public List<SpawnPointRecord> Points{get;set;} = new(); // SpawnPointRegistry의 포인트 순서
}
public class SpawnPointRecord
{
    public int PrefabHash{get;set;} = NetworkPrefabCatalog.NoHash; // NetworkPrefabCatalog.GetHashId(이름). 고른 프리팹이 없으면 NoHash
    public int ObjectId{get;set;} = -1; // 나와 있는 오브젝트 ID. 없으면 -1
    public bool Consumed{get;set;} // 이미 채집/처치되어 사라짐. 재소환 주기가 지나기 전까지 비어 있다
    public int ConsumedDay{get;set;} // 사라진 날. Consumed일 때만 의미 있음
    public SaveData State{get;set;} // 디스폰할 때 보관한 상태. 다시 소환할 때 적용
}

// 최종보스 진행 단계. 결계와 최종보스는 일반 소환 규칙(디스폰/재소환)을 따르지 않고 이 기록대로만 관리한다
public enum FinalBossStage
{
    None = 0,     // 아직 결계를 소환하지 않음
    Barrier = 1,  // 결계가 서 있음
    Boss = 2,     // 결계가 풀리고 최종보스가 나와 있음
    Defeated = 3, // 최종보스 처치
}
// 결계/최종보스는 디스폰하지 않으므로 월드 엔티티로 같이 저장되고, 불러오면 같은 ID로 다시 생성된다
public class FinalBossRecord
{
    public FinalBossStage Stage{get;set;}
    public int ObjectId{get;set;} = -1; // 지금 나와 있는 결계 또는 최종보스 ID. 없으면 -1
}

// 입장한 플레이어에게 보내는 현재 월드 상태. 플레이어별 저장 정보(WorldData.Players)는 보내지 않는다
public class WorldSnapshot
{
    public int Day{get;set;}
    public float Time{get;set;}
    public float DayLength{get;set;} // 방장 월드의 하루 길이(초)
    public List<WorldObjSaveData> Entities{get;set;} = new();

    // 네트워크 전송용 바이너리
    // 엔티티 목록은 통째로 한 번에 압축. 엔티티마다 반복되는 JSON 키가 같이 압축되어 개별 압축보다 효율이 좋음
    public void Write(PacketData p)
    {
        p.Write(Day);
        p.Write(Time);
        p.Write(DayLength);
        var inner = new PacketData();
        inner.Write(Entities.Count);
        foreach(var e in Entities) e.Write(inner, compressState: false); // 바깥에서 한 번에 압축하므로 개별 압축 안 함
        inner.Save();
        int rawLength = inner.GetData().Length;
        int before = p.WrittenLength;
        p.WriteBlock(inner.GetData(), compress: true);
        int blockLength = p.WrittenLength - before; // 블록 헤더(플래그 1 + 길이 4) 포함
        Debug.Log($"[WorldSnapshot] 엔티티 {Entities.Count}개 | 압축 전 {rawLength:N0}B | 압축 후 {blockLength:N0}B " +
                  $"({(rawLength > 0 ? blockLength * 100f / rawLength : 0f):F1}%) | 스냅샷 전체 {p.WrittenLength:N0}B");
    }
    public static WorldSnapshot Read(PacketData p)
    {
        var s = new WorldSnapshot { Day = p, Time = p, DayLength = p };
        var inner = new PacketData(p.ReadBlock());
        int count = inner;
        for(int i = 0; i < count; i++) s.Entities.Add(WorldObjSaveData.Read(inner));
        return s;
    }
}

// 엔티티 상태 자체는 ISavable에 맡기고, 여기엔 다시 생성하는 데 필요한 정보만 둔다
public class WorldObjSaveData
{
    public int ID{get;set;} // IdManager 발급 id
    public int PrefabHash{get;set;} // NetworkPrefabCatalog.GetHashId(PrefabId). 세이브/통신 용량을 줄이기 위해 문자열 대신 저장
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
            PrefabHash = NetworkPrefabCatalog.GetHashId(obj.PrefabId),
            Position = t.position,
            Rotation = t.eulerAngles,
            State = state
        };
    }

    // 네트워크 전송용 바이너리. 다시 생성하는 데 필요한 값은 고정 크기로 쓰고,
    // 상태(SaveData)는 Entity마다 형태가 달라서 JSON으로 두고, 크면 Deflate로 압축
    // compressState : 바깥에서 묶어서 압축하는 경우(스냅샷) false
    public void Write(PacketData p, bool compressState = true)
    {
        p.Write(ID);
        p.Write(PrefabHash);
        p.Write(Position.ToVector3());
        p.Write(Rotation.ToVector3());
        p.WriteSaveData(State, compressState);
    }
    public static WorldObjSaveData Read(PacketData p) => new()
    {
        ID = p,
        PrefabHash = p,
        Position = p.ReadVector3(),
        Rotation = p.ReadVector3(),
        State = p.ReadSaveData()
    };
}

public static class SaveDataPacketExtensions
{
    // 형식 : [byte 플래그][int 길이][바이트]
    //   플래그 0 : null (길이/바이트 없음)
    //   플래그 1 : 원본 그대로
    //   플래그 2 : Deflate 압축
    // 작은 데이터는 압축 헤더 때문에 오히려 커지므로 CompressThreshold 이상일 때만 압축하고,
    // 압축해도 줄어들지 않으면 원본으로 보냄. 읽는 쪽은 플래그만 보고 처리하므로 압축 여부와 상관없이 같은 Read를 씀
    public const int CompressThreshold = 256;
    private const byte FlagNull = 0;
    private const byte FlagRaw = 1;
    private const byte FlagDeflate = 2;

    public static void WriteSaveData(this PacketData p, SaveData d, bool compress = true)
    {
        if(d == null)
        {
            p.Write(FlagNull);
            return;
        }
        p.WriteBlock(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(d)), compress);
    }
    public static SaveData ReadSaveData(this PacketData p)
    {
        byte[] bytes = p.ReadBlock();
        return bytes == null ? null : JsonConvert.DeserializeObject<SaveData>(Encoding.UTF8.GetString(bytes));
    }

    public static void WriteBlock(this PacketData p, byte[] raw, bool compress)
    {
        if(raw == null)
        {
            p.Write(FlagNull);
            return;
        }
        if(compress && raw.Length >= CompressThreshold)
        {
            byte[] compressed = Deflate(raw);
            if(compressed.Length < raw.Length)
            {
                p.Write(FlagDeflate);
                p.Write(compressed.Length);
                p.Write(compressed);
                return;
            }
        }
        p.Write(FlagRaw);
        p.Write(raw.Length);
        p.Write(raw);
    }
    public static byte[] ReadBlock(this PacketData p)
    {
        byte flag = p.ReadByte();
        if(flag == FlagNull) return null;
        int length = p.ReadInt();
        int start = p.offset;
        p.offset += length;
        if(flag == FlagDeflate) return Inflate(p.data, start, length);
        var raw = new byte[length];
        Buffer.BlockCopy(p.data, start, raw, 0, length);
        return raw;
    }

    private static byte[] Deflate(byte[] raw)
    {
        using var output = new MemoryStream();
        using(var deflate = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(raw, 0, raw.Length);
        return output.ToArray();
    }
    private static byte[] Inflate(byte[] data, int offset, int count)
    {
        using var input = new MemoryStream(data, offset, count);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
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
