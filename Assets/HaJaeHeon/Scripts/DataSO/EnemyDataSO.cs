using Unity.Behavior;
using UnityEngine;

public enum Monster_Kind
{
    Normal,
    Boss,
    Special,
}

public enum Monster_Zone
{
    설산 = 1,
    독늪 = 2,
    용암지대 = 3,
}

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : EntityDataSO
{
    //[SerializeField] private int hp;

    [Header("시트 정보")]
    [SerializeField] private int monsterId;
    [SerializeField] private string monsterName;
    [SerializeField] private Monster_Kind kind;
    [SerializeField] private int atk;
    [SerializeField] private float speed;
    [SerializeField] private int zoneId;
    [SerializeField] private string zoneName;
    [SerializeField] private int dropId;
    [SerializeField] private string dropName;
    [SerializeField] private string behaviour;

    [Header("추가로 등록할 내용")]
    [SerializeField] private float detectRange;
    [SerializeField] private float attackRange;
    [SerializeField] private BehaviorGraph behaviourGraph;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyPatternDataSO normalAttackPattern;
    

    public int GetEnemyId()
    {
        return monsterId;
    }

    public int GetDropId()
    {
        return dropId;
    }

    public string GetDropName()
    {
        return dropName;
    }

    public GameObject GetEnemyPrefab()
    {
        return enemyPrefab;
    }

    public BehaviorGraph GetBehaviorGraph()
    {
        return behaviourGraph;
    }

    public float GetDetectRange()
    {
        return detectRange;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public int GetAtk()
    {
        return atk;
    }

    public EnemyPatternDataSO GetNormalAttackPattern()
    {
        return normalAttackPattern;
    }
}