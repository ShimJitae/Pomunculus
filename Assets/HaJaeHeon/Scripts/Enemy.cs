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
        private set { so = value; }
    }

    private void Awake()
    {
        behaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
    }


    protected override void OnEnable()
    {
        if(enemySO == null)
        {
            Debug.Log("SO 데이터가 연결되어있지 않습니다.");
            gameObject.SetActive(false);
            return;
        }

        behaviorGraphAgent.Graph = enemySO.GetBehaviorGraph();
        CurrentHp = enemySO.GetHp();
        isDie = CurrentHp <= 0;
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
        return enemySO.GetEnemyId();
    }

    protected override void OnDie()
    {
        behaviorGraphAgent.SetVariableValue<bool>("IsDie", true);

        DropItemManager.Instance.SpawnDropItem(GetId(), transform.position);

        Destroy(gameObject, 1f);
    }

    public void SetTarget(GameObject player)
    {
        target = player;
        behaviorGraphAgent.SetVariableValue<GameObject>("Target", player);
    }
}
