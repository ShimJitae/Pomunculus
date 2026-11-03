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
public class MonsterDataSO : ScriptableObject
{

    [SerializeField] private int monster_id;
    [SerializeField] private string monster_name;
    [SerializeField] private Monster_Kind kind;
    [SerializeField] private int hp;
    [SerializeField] private int atk;
    [SerializeField] private float speed;
    [SerializeField] private int zone_id;
    [SerializeField] private string zone_name;
    [SerializeField] private int drop_id;
    [SerializeField] private string drop_name;
    [SerializeField] private string behaviour;

    public int GetMonsterId()
    {
        return monster_id;
    }
}