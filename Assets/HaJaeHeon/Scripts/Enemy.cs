using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class Enemy : Entity
{
    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;

    protected EnemyDataSO enemySO
    {
        get { return so as EnemyDataSO; }
    }




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
