using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternDatabase", menuName = "Scriptable Objects/PatternDatabase")]
public class PatternDatabase : ScriptableObject
{
    [SerializeField] private List<EnemyPatternDataSO> data = new();

    private Dictionary<int, EnemyPatternDataSO> pattern = new();
    private Dictionary<int, List<EnemyPatternDataSO>> combinePattern = new();
    private static readonly List<EnemyPatternDataSO> emptyPatternList = new();

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

        for(int i = 0;i < data.Count;i++)
        {
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

    public EnemyPatternDataSO TryGetDataSO(int patternId)
    {
        if (pattern.TryGetValue(patternId, out EnemyPatternDataSO patternData))
            return patternData;

        Debug.Log($"{patternId}에 해당하는 패턴 없음");
        return null;
    }

    public IReadOnlyList<EnemyPatternDataSO> GetPatternsByEnemyId(int monsterId)
    {
        if (combinePattern.TryGetValue(monsterId, out List<EnemyPatternDataSO> monsterPatterns))
            return monsterPatterns;

        Debug.Log($"{monsterId}에 해당하는 패턴 없음");
        return emptyPatternList;
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
