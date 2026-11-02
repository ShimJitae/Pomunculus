using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : EntityDataSO
{
    [SerializeField] private float speed;
    [SerializeField] private float attackDamage;

    public EnemyDataSO(int hp, List<DroptableDataSO> dropList) : base(hp, dropList)
    {
    }
}
