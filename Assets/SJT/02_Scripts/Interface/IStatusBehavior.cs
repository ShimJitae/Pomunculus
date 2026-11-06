public interface IStatusBehavior
{
    /// <summary>이벤트: 처음 걸릴 때 한 번 호출된다.</summary>
    void OnRegistered(StatusEffect effect);

    /// <summary>이벤트: 발동 주기마다 호출된다.</summary>
    void OnTick(StatusEffect effect);

    /// <summary>이벤트: 이미 걸린 상태가 다시 걸릴 때 호출된다.</summary>
    void OnReapply(StatusEffect effect);

    /// <summary>이벤트: 상태가 해제될 때 호출된다.</summary>
    void OnRemove(StatusEffect effect);

    /// <summary>
    /// 계산: 현재 중첩 기준으로 modifiers에 자기 몫을 보탠다. 대상 Entity에 직접 쓰지 않는다.
    /// 컨트롤러가 기본값에서 시작해 활성 상태 전체에 호출하므로 여러 번 호출될 수 있다.
    /// 배율은 곱하고(*=), 제한은 막기만 하며(= false), 무적은 켜기만 한다(= true). 값을 되돌리지 않는다.
    /// </summary>
    void ApplyModifier(StatusEffect effect, ref StatusModifiers modifiers);
}
