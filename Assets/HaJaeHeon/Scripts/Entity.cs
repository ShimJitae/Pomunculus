using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(StatusEffectController))]
public class Entity : MonoBehaviour
{
    #region 상태 이상에 따른 수치
    [Min(0f)] public float MoveSpeedRatio { get; set; } = 1f; // 이동속도 비율. 기본값은 1.0f (100%)
    [Min(0f)] public float HealReceivedRatio { get; set; } = 1f; // 회복량 비율. 기본값은 1.0f (100%)
    [Min(0f)] public float DamageTakenRatio { get; set; } = 1f; // 받는 피해량 비율. 기본값은 1.0f (100%)
    public bool CanMove { get; set; } = true; // 이동 가능 여부. 기본값은 true
    public bool CanDodge { get; set; } = true; // 회피 가능 여부. 기본값은 true
    public bool CanAttack { get; set; } = true; // 공격 가능 여부. 기본값은 true
    public bool IsInvincible { get; set; } = false; // 무적 상태 여부. 기본값은 false

    public void ResetStatusModifiers()
    {
        MoveSpeedRatio = 1f;
        HealReceivedRatio = 1f;
        DamageTakenRatio = 1f;
        CanMove = true;
        CanDodge = true;
        CanAttack = true;
        IsInvincible = false;
    }
    #endregion

    protected int entityId;

    protected int currentHp;

    public int CurrentHp
    {
        get
        {
            return currentHp;
        }
        private set
        {
            currentHp = Mathf.Clamp(value, 0, so.GetMaxHp());
        }
    }

    [SerializeField] protected float moveSpeed;

    [SerializeField] protected bool isDie;
    [SerializeField] protected EntityDataSO so;

    protected virtual void OnEnable()
    {
        CurrentHp = so.GetHp();
        isDie = CurrentHp <= 0;
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

        if (CurrentHp <= 0)
        {
            isDie = true;
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
}
