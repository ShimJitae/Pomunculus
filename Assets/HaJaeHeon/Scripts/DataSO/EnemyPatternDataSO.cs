using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyPatternDataSO", menuName = "Scriptable Objects/EnemyPatternDataSO")]
public class EnemyPatternDataSO : ScriptableObject
{
    [Header("식별 정보")]
    [SerializeField] private int patternId;
    [SerializeField] private string monsterName;
    [SerializeField] private int monsterId;
    [SerializeField] private string patternName;

    [Header("행동 수치")]
    [SerializeField, Min(0f)] private float telegraph;
    [SerializeField, Min(0), FormerlySerializedAs("damage")] private int value;   // 피해·중첩 수·회복량 등 패턴별 수치
    [SerializeField, Min(0f)] private float recovery;
    [SerializeField, Min(0f)] private float interval;
    [SerializeField, Min(0)] private int patternWeight = 1;

    [Header("설명")]
    [SerializeField, TextArea(3, 8)] private string note;

    public int PatternId => patternId;
    public string MonsterName => monsterName;
    public int MonsterId => monsterId;
    public string PatternName => patternName;
    public float Telegraph => telegraph;
    public int Value => value;
    public float Recovery => recovery;
    public float Interval => interval;
    public int PatternWeight => patternWeight;
    public string Note => note;

    public string GetPatternName()
    {
        return patternName;
    }

    public int GetPatternId()
    {
        return patternId;
    }

    public int GetMonsterId()
    {
        return monsterId;
    }
}
