using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternDatabase", menuName = "Scriptable Objects/PatternDatabase")]
public class PatternDatabase : ScriptableObject
{
    [SerializeField] private List<EnemyPatternDataSO> data = new();

    private Dictionary<int, EnemyPatternDataSO> pattern = new();
    private Dictionary<int, List<EnemyPatternDataSO>> combinePattern = new();

    private void OnEnable()
    {
        InitDatabase();
        InitCombinePattern();
    }

    private void InitDatabase()
    {
        pattern.Clear();

        for (int i = 0; i < data.Count; i++)
        {
            EnemyPatternDataSO so = data[i];

            if (so == null)
                continue;

            int patternId = so.GetPatternId();

            if (patternId <= 0 || !pattern.TryAdd(patternId, so))
                continue;
        }
    }

    private void InitCombinePattern()
    {
        combinePattern.Clear();

        List<int> checkList = new List<int>();
        List<EnemyPatternDataSO> patternSOList = new List<EnemyPatternDataSO>();
        List<EnemyPatternDataSO> storeList = new List<EnemyPatternDataSO>();

        for(int i = 0;i < data.Count;i++)
        {
            storeList.Clear();

            EnemyPatternDataSO so = data[i];

            if (so == null)
                continue;

            int monsterId = so.GetMonsterId();

            if (!checkList.Contains(monsterId))
            {
                checkList.Add(monsterId);

                List<EnemyPatternDataSO> monsterPatterns = new List<EnemyPatternDataSO>();

                for (int j = i; j < data.Count; j++)
                {
                    if (data[j] != null && data[j].GetMonsterId() == monsterId)
                        monsterPatterns.Add(data[j]);
                }

                combinePattern.TryAdd(monsterId, monsterPatterns);
            }
        }
    }

    public EnemyPatternDataSO TryGetDataSO(int monsterId)
    {
        if (pattern.TryGetValue(monsterId, out EnemyPatternDataSO so))
        {
            return so;
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return null;
    }

    public int TryGetPatternID(int patternId)
    {
        if (pattern.TryGetValue(patternId, out EnemyPatternDataSO so))
        {
            return so.GetPatternId();
        }

        Debug.Log($"{patternId}에 해당하는 데이터 없음");
        return -1;
    }

    public string TryGetPatternName(int patternId)
    {
        if (pattern.TryGetValue(patternId, out EnemyPatternDataSO so))
        {
            return so.GetPatternName();
        }

        Debug.Log($"{patternId}에 해당하는 데이터 없음");
        return null;
    }

    public void ShowDB()
    {
        Debug.Log($"원본 목록: {data.Count}개 / 조회용 사전: {pattern.Count}개");
        foreach (var entry in pattern)
        {
            Debug.Log($"ID : {entry.Key} / SO이름 : {entry.Value.name}");
        }

        Debug.LogWarning("=================================================");

        Debug.Log($"원본 목록: {data.Count}개 / 조회용 사전: {combinePattern.Count}개");
        foreach (var entry in combinePattern)
        {
            Debug.Log($"ID : {entry.Key} / 패턴 : {string.Join(", ", entry.Value.ConvertAll(patternData => patternData.PatternName))}");
        }
    }
}
