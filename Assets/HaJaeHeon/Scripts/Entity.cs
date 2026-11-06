using UnityEngine;

[RequireComponent(typeof(StatusEffectController))]
public class Entity : MonoBehaviour, IWorldObj, IDamageable
{
    public string PrefabId { get; set; }
    public int ID { get; set; }

    #region 상태 이상에 따른 수치
    // StatusEffectController.OnModifiersChanged로만 갱신된다.
    public float MoveSpeedRatio { get; private set; } = 1f; // 이동속도 비율. 기본값은 1.0f (100%)
    public float HealReceivedRatio { get; private set; } = 1f; // 회복량 비율. 기본값은 1.0f (100%)
    public float DamageTakenRatio { get; private set; } = 1f; // 받는 피해량 비율. 기본값은 1.0f (100%)
    public bool CanMove { get; private set; } = true; // 이동 가능 여부. 기본값은 true
    public bool CanDodge { get; private set; } = true; // 회피 가능 여부. 기본값은 true
    public bool CanAttack { get; private set; } = true; // 공격 가능 여부. 기본값은 true
    public bool IsInvincible { get; private set; } = false; // 무적 상태 여부. 기본값은 false

    private StatusEffectController statusEffectController;

    private void HandleModifiersChanged(StatusModifiers m)
    {
        MoveSpeedRatio = m.MoveSpeedRatio;
        HealReceivedRatio = m.HealReceivedRatio;
        DamageTakenRatio = m.DamageTakenRatio;
        CanMove = m.CanMove;
        CanDodge = m.CanDodge;
        CanAttack = m.CanAttack;
        IsInvincible = m.IsInvincible;
        OnStatusModifiersChanged(m);
    }

    /// <summary>상태이상 수치가 바뀐 직후 호출된다. 자식이 속도·애니메이션 등을 갱신할 지점이다.</summary>
    protected virtual void OnStatusModifiersChanged(StatusModifiers m) { }
    #endregion

    protected int entityId;

    protected int currentHp;

    public int CurrentHp
    {
        get
        {
            return currentHp;
        }
        protected set
        {
            currentHp = Mathf.Clamp(value, 0, so.GetMaxHp());
        }
    }

    [SerializeField] protected float moveSpeed;

    [SerializeField] protected bool isDie;
    [SerializeField] protected EntityDataSO so;

    protected virtual void Awake()
    {
        statusEffectController = GetComponent<StatusEffectController>();
    }

    protected virtual void OnEnable()
    {
        CurrentHp = so.GetHp();
        isDie = CurrentHp <= 0;

        statusEffectController.OnModifiersChanged += HandleModifiersChanged;
        HandleModifiersChanged(statusEffectController.Modifiers); // 현재 값으로 한 번 맞춘다
    }

    protected virtual void OnDisable()
    {
        statusEffectController.OnModifiersChanged -= HandleModifiersChanged;
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDie)
        {
            Debug.Log("이미 죽은 상태입니다.");
            return;
        }

        if (damage <= 0)
        {
            Debug.Log($"{damage}가 0보다 작거나 같습니다.");
            return;
        }

        Debug.Log($"현재 HP : {CurrentHp} / 데미지 >> {damage}");

        CurrentHp -= damage;
        //기존
        //if (CurrentHp <= 0)
        //{
        //    isDie = true;
        //    OnDie();
        //}
        //임시 테스트용 habin
        if (CurrentHp <= 0)
        {
            isDie = true;

            if (TryGetComponent<ItemDropper>(out ItemDropper dropper))
                dropper.DropItems();

            OnDie();
        }

        Debug.Log($"데미지 계산 후 HP : {CurrentHp}");
    }

    public virtual bool CheckDie()
    {
        return isDie;
    }

    [ContextMenu("사망 시키기")]
    public virtual void KillEntity()
    {
        int damage = so.GetMaxHp();
        TakeDamage(damage);
    }

    [ContextMenu("10씩 데미지 입히기")]
    public virtual void Take10Damage()
    {
        TakeDamage(10);
    }

    protected virtual void OnDie()
    {
        Debug.Log("Entity 사망");
    }

    protected virtual int GetId()
    {
        return entityId;
    }

    public virtual void InitWorldObj(int id, string prefabId)
    {
    }

    public virtual void SaveData(SaveData _data)
    {
        var sd = new SaveData();
        statusEffectController.SaveData(sd);
        _data.Child["sec"] = sd;
    }

    public virtual void LoadData(SaveData _data)
    {
        var sd = _data.Child["sec"];
        statusEffectController.LoadData(sd);
    }

    public virtual void TakeDamage(int damage, Vector3 sourcePosition)
    {
        Debug.Log($"{damage}만큼 피격!");
        TakeDamage(damage);
    }
}
