using Unity.Behavior;
using UnityEngine;

//[RequireComponent(typeof(BehaviorGraphAgent))]
public class Enemy : Entity
{
    //[SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;

    protected EnemyDataSO enemySO
    {
        get { return so as EnemyDataSO; }
        private set { so = value; }
    }

    private void Awake()
    {
        //behaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
    }


    protected override void OnEnable()
    {
        if(enemySO == null)
        {
            Debug.Log("SO 데이터가 연결되어있지 않습니다.");
            gameObject.SetActive(false);
            return;
        }

        CurrentHp = enemySO.GetHp();
        moveSpeed = enemySO.GetSpeed();
        isDie = CurrentHp <= 0;
    }

    //public override void TakeDamage(int damage)
    //{
    //    base.TakeDamage(damage);
    //}

    //public override bool CheckDie()
    //{
    //    return base.CheckDie();
    //}

    //protected override int GetId()
    //{
    //    return enemySO.GetEnemyId();
    //}

    protected override void OnDie()
    {
        DropItemManager.Instance.SpawnDropItem(GetId(), transform.position);

        Destroy(gameObject, 1f);
    }
}
