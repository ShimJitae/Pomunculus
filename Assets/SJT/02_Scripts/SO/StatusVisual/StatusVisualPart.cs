using UnityEngine;

/// <summary>
/// 상태이상 연출 파츠의 공통 틀이다. StatusEffectData의 연출 목록에 넣어 사용한다.
/// 모든 대상이 같은 에셋을 공유하므로 대상별 실행 상태를 필드에 두지 않고, 필요한 상태는 host(StatusEffectVisualizer)에 맡긴다.
/// 필요한 시점의 함수만 재정의한다.
/// </summary>
public abstract class StatusVisualPart : ScriptableObject
{
    /// <summary>상태가 처음 걸렸을 때 호출된다.</summary>
    public virtual void OnStart(StatusEffectVisualizer host, StatusEffect effect)
    {
    }

    /// <summary>
    /// 대상이 비활성화됐다가 다시 활성화될 때 남아 있던 상태마다 호출된다.
    /// 외곽선처럼 지속되는 연출만 다시 켜고, 걸림 순간의 일회성 연출은 재생하지 않는다.
    /// </summary>
    public virtual void OnRestore(StatusEffectVisualizer host, StatusEffect effect)
    {
    }

    /// <summary>이미 걸린 상태가 다시 걸려 중첩됐을 때 호출된다.</summary>
    public virtual void OnReapply(StatusEffectVisualizer host, StatusEffect effect)
    {
    }

    /// <summary>상태의 발동 주기마다 호출된다.</summary>
    public virtual void OnTick(StatusEffectVisualizer host, StatusEffect effect)
    {
    }

    /// <summary>상태가 만료되어 해제될 때 호출된다.</summary>
    public virtual void OnEnd(StatusEffectVisualizer host, StatusEffect effect)
    {
    }
}
