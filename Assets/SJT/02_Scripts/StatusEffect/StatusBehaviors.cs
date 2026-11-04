

/// <summary>화상 상태의 동작을 정의한다. 실제 효과는 아직 구현하지 않았다.</summary>
public sealed class BurnBehavior : IStatusBehavior
{

    public void OnRegistered(StatusEffect effect)
    {
    }

    public void OnTick(StatusEffect effect)
    {
    }

    public void OnReapply(StatusEffect effect)
    {
    }

    public void OnRemove(StatusEffect effect)
    {
    }
}
// }

// /// <summary>냉기 상태의 동작을 정의한다. 실제 효과는 아직 구현하지 않았다.</summary>
// public sealed class ChillBehavior : IStatusBehavior
// {
//     /// <summary>냉기가 처음 부여됐을 때 호출된다.</summary>
//     public void OnApply(StatusEffect effect)
//     {
//     }

//     /// <summary>냉기의 발동 간격마다 호출된다.</summary>
//     public void OnTick(StatusEffect effect)
//     {
//     }

//     /// <summary>냉기 중첩이 변경됐을 때 호출된다.</summary>
//     public void OnStackChanged(StatusEffect effect)
//     {
//     }

//     /// <summary>냉기가 제거될 때 호출된다.</summary>
//     public void OnRemove(StatusEffect effect)
//     {
//     }
// }

// /// <summary>독 상태의 동작을 정의한다. 실제 효과는 아직 구현하지 않았다.</summary>
// public sealed class PoisonBehavior : IStatusBehavior
// {
//     /// <summary>독이 처음 부여됐을 때 호출된다.</summary>
//     public void OnApply(StatusEffect effect)
//     {
//     }

//     /// <summary>독의 발동 간격마다 호출된다.</summary>
//     public void OnTick(StatusEffect effect)
//     {
//     }

//     /// <summary>독 중첩이 변경됐을 때 호출된다.</summary>
//     public void OnStackChanged(StatusEffect effect)
//     {
//     }

//     /// <summary>독이 제거될 때 호출된다.</summary>
//     public void OnRemove(StatusEffect effect)
//     {
//     }
// }

// /// <summary>기절 상태의 동작을 정의한다. 실제 효과는 아직 구현하지 않았다.</summary>
// public sealed class StunBehavior : IStatusBehavior
// {
//     /// <summary>기절이 처음 부여됐을 때 호출된다.</summary>
//     public void OnApply(StatusEffect effect)
//     {
//     }

//     /// <summary>기절의 발동 간격마다 호출된다.</summary>
//     public void OnTick(StatusEffect effect)
//     {
//     }

//     /// <summary>기절 중첩이 변경됐을 때 호출된다.</summary>
//     public void OnStackChanged(StatusEffect effect)
//     {
//     }

//     /// <summary>기절이 제거될 때 호출된다.</summary>
//     public void OnRemove(StatusEffect effect)
//     {
//     }
// }