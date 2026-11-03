using Unity.Behavior;
using UnityEngine;
using UnityEngine.LightTransport;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class Enemy : Entity
{
    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;

    protected EnemyDataSO monsterSO
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

    protected override int GetId()
    {
        return monsterSO.GetMonsterId();
    }

    protected override void OnDie()
    {
        DropItemManager.Instance.SpawnDropItem(GetId());
    }
}
