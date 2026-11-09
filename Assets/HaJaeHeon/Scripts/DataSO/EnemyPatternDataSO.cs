using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum AttackAreaShape
{
    None,       // 범위 없음(버프, 소환, 이동 등)
    Circle,     //원
    Sector,     // 부채꼴
    Rectangle,  // 직사각형 (돌진, 브레스 등등)
}

public enum AttackAreaAnchor
{
    Self,   // 적 몬스터 기준
    Target, // 스킬 캐스팅 시 타겟 위치 기준
}

public enum EnemyAttackTrigger
{
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
}

[Serializable]
public struct AttackAreaData
{
    public AttackAreaShape shape;
    public AttackAreaAnchor anchor;
    [Min(0f)] public float radius;
    [Range(0f, 360f)] public float angle;
    [Min(0f)] public float width;
    [Min(0f)] public float length;
    public Vector3 offset;
}

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
    [SerializeField, Min(0)] private int patternWeight;
    [SerializeField] private EnemyAttackTrigger animatorTrigger;

    [Header("사용 사거리 (0이면 몬스터 기본 사거리)")]
    [SerializeField, Min(0f)] private float useRange;

    [Header("공격 범위 (실제로 맞는 영역)")]
    [SerializeField] private AttackAreaData attackArea;
    
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
    public float UseRange => useRange;
    public AttackAreaData AttackArea => attackArea;
    public string AnimatorTrigger => animatorTrigger.ToString();
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
