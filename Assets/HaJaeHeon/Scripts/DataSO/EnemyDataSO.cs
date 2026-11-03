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

[CreateAssetMenu(fileName = "MonsterDataSO", menuName = "Scriptable Objects/MonsterDataSO")]
public class EnemyDataSO : EntityDataSO
{
    //[SerializeField] private int hp;

    [Header("시트 정보")]
    [SerializeField] private int monster_id;
    [SerializeField] private string monster_name;
    [SerializeField] private Monster_Kind kind;
    [SerializeField] private int atk;
    [SerializeField] private float speed;
    [SerializeField] private int zone_id;
    [SerializeField] private string zone_name;
    [SerializeField] private int drop_id;
    [SerializeField] private string drop_name;
    [SerializeField] private string behaviour;

    [Header("추가로 등록할 내용")]
    [SerializeField] private float detectRange;
    [SerializeField] private BehaviorGraph behaviourGraph;
    [SerializeField] private GameObject enemyPrefab;

    public int GetEnemyId()
    {
        return monster_id;
    }

    public int GetDropId()
    {
        return drop_id;
    }

    public string GetDropName()
    {
        return drop_name;
    }

    public GameObject GetEnemyPrefab()
    {
        return enemyPrefab;
    }
}