using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Scriptable Objects/MonsterDatabase")]
public class EnemyDatabase : ScriptableObject
{
    [SerializeField] private List<EnemyDataSO> data = new();

    private Dictionary<int, EnemyDataSO> monster = new();

    private void OnEnable()
    {
        InitDatabase();
    }

    private void InitDatabase()
    {
        monster.Clear();

        foreach (EnemyDataSO m in data)
        {
            monster.Add(m.GetMonsterId(), m);
        }
    }

    public EnemyDataSO TryGetDataSO(int monsterId)
    {
        if (monster.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so;
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return null;
    }

    public int TryGetDropItemID(int monsterId)
    {
        if (monster.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so.GetDropId();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return -1;
    }

    public string TryGetDropItemName(int monsterId)
    {
        if (monster.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so.GetDropName();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return null;
    }

    [ContextMenu("DB확인")]
    public void ShowDB()
    {
        Debug.Log($"원본 목록: {data.Count}개 / 조회용 사전: {monster.Count}개");
        foreach (var entry in monster)
        {
            Debug.Log($"ID : {entry.Key} / SO이름 : {entry.Value.name} / Value : {entry.Value}");
        }
    }
}
