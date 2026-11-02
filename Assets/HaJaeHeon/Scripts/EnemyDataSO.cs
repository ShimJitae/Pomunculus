using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "Scriptable Objects/EnemyDataSO")]
public class EnemyDataSO : EntityDataSO
{
    [SerializeField] private float speed;
    [SerializeField] private float detectRange;
    [SerializeField] private float attackDamage;
    [SerializeField] private BehaviorGraph behaviourGraph;

    public EnemyDataSO(int hp, List<DroptableDataSO> dropList) : base(hp, dropList)
    {
    }
}
