using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Scriptable Objects/MonsterDatabase")]
public class MonsterDatabase : ScriptableObject
{
    [SerializeField] private List<MonsterDataSO> data = new();

    private Dictionary<int, MonsterDataSO> monster = new();

    private void Awake()
    {
        data.Clear();

        InitDatabase();
    }

    private void InitDatabase()
    {
        foreach (MonsterDataSO m in data)
        {
            monster.Add(m.GetMonsterId(), m);
        }
    }

    public MonsterDataSO GetDataSO(int monsterId)
    {
        monster.TryGetValue(monsterId, out MonsterDataSO so);

        return so;
    }
}
