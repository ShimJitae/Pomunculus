using UnityEngine;

/// <summary>발동 주기마다 대상에게 현재 중첩에 비례한 화상 피해를 적용한다.</summary>
public sealed class BurnBehavior : IStatusBehavior
{
    /// <summary>첫 주기 전에는 즉시 피해를 적용하지 않는다.</summary>
    public void OnRegistered(StatusEffect effect)
    {
    }

    /// <summary>무적과 받는 피해 배율을 반영한 화상 피해를 대상에게 전달한다.</summary>
    public void OnTick(StatusEffect effect)
    {
        Entity target = effect.Target;
        if (target == null || target.CheckDie() || target.IsInvincible)
        {
            return;
        }

        if (effect.TickDamagePerStack <= 0 || effect.StackCount <= 0)
        {
            return;
        }

        int damage = Mathf.RoundToInt((float)effect.TickDamagePerStack * effect.StackCount);
        if (damage <= 0)
        {
            return;
        }

        // 무적은 여기서 확인하고, 받는 피해 배율은 대상의 TakeDamage가 반영한다.
        target.TakeDamage(damage);
    }

    /// <summary>다음 주기에 갱신된 중첩을 사용하므로 재부여 시 즉시 피해를 주지 않는다.</summary>
    public void OnReapply(StatusEffect effect)
    {
    }

    /// <summary>지속적으로 변경한 능력치가 없어 해제 시 복원할 값이 없다.</summary>
    public void OnRemove(StatusEffect effect)
    {
    }

    /// <summary>화상은 상태이상 수치를 바꾸지 않는다.</summary>
    public void ApplyModifier(StatusEffect effect, ref StatusModifiers modifiers)
    {
    }
}