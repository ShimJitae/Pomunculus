using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityDataSO", menuName = "Scriptable Objects/EntityDataSO")]
public class EntityDataSO : ScriptableObject
{
    [SerializeField] private int hp;
    [SerializeField] private List<DroptableDataSO> dropList = new();

    public EntityDataSO(int hp, List<DroptableDataSO> dropList)
    {
        this.hp = hp;
        this.dropList = dropList;
    }
}
