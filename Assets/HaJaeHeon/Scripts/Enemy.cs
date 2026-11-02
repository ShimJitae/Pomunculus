using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class Enemy : Entity
{
    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;

    [SerializeField] private EnemyDataSO so;




    protected override void OnEnable()
    {

        base.OnEnable();
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }

    public override bool CheckDie()
    {
        return base.CheckDie();
    }
}
