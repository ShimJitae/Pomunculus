using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BehaviorGraphAgent)), RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Entity
{
    [SerializeField] private BehaviorGraphAgent behaviorGraphAgent;
    [SerializeField] private GameObject target;
    [SerializeField] private EnemyDetector detector;


    protected EnemyDataSO enemySO
    {
        get { return so as EnemyDataSO; }
        private set { so = value; }
    }

    private void Awake()
    {
        behaviorGraphAgent = GetComponent<BehaviorGraphAgent>();
        detector = GetComponentInChildren<EnemyDetector>();
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
        moveSpeed = enemySO.GetSpeed();
        isDie = CurrentHp <= 0;
        detector.SetRadius(enemySO.GetDetectRange());
        detector.OnPlayersChanged += RefreshTarget;
    }

    private void OnDisable()
    {
        detector.OnPlayersChanged -= RefreshTarget;
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
        behaviorGraphAgent.SetVariableValue("HasTarget", player != null);
    }

    public void RefreshTarget()
    {
        if (target != null && target.activeInHierarchy && detector.Contains(target))
            return;

        SetTarget(detector.GetNearestPlayer());
    }
}
