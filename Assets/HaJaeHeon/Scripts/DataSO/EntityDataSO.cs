using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityDataSO", menuName = "Scriptable Objects/EntityDataSO")]
public class EntityDataSO : ScriptableObject
{
    [SerializeField] protected int hp;
    [SerializeField] protected int maxHp;

    //[SerializeField] protected List<DroptableDataSO> dropList = new();

    public int GetHp()
    {
        return hp;
    }

    public int GetMaxHp()
    {
        return maxHp;
    }
}
