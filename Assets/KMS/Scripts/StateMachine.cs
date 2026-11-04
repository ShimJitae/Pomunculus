using System.Collections.Generic;
using UnityEngine;

public class StateMachine<TEnum, TOwner> where TEnum : System.Enum
{
    readonly Dictionary<TEnum, IState<TEnum, TOwner>> states = new Dictionary<TEnum, IState<TEnum, TOwner>>();
    readonly TOwner owner;

    public IState<TEnum, TOwner> CurrentState { get; private set; }
    public bool HasState => CurrentState != null;

    // 현재 상태 enum
    public TEnum CurrentStateType => CurrentState != null ? CurrentState.StateType : default;
    public TEnum PreviousStateType { get; private set; }

    public StateMachine(TOwner owner) => this.owner = owner;

    
    public void AddState(IState<TEnum, TOwner> state)
    {
        if (states.ContainsKey(state.StateType))
        {
            Debug.LogWarning($"[FSM] {state.StateType} 상태가 이미 등록되어 있습니다.");
            return;
        }
        states.Add(state.StateType, state);
    }


    // 같은 상태로 다시 들어가는 것 허용 (연속 공격, 연속 피격)
    public bool ChangeState(TEnum next, bool allowReenter = false)
    {
        if (!states.TryGetValue(next, out var nextState))
        {
            Debug.LogError($"[FSM] {typeof(TEnum).Name}.{next} 상태가 등록되지 않았습니다. AddState를 확인하세요.");
            return false;
        }

        if (nextState == CurrentState && !allowReenter) return false;

        if (CurrentState != null)
        {
            CurrentState.Exit(owner);
            PreviousStateType = CurrentState.StateType;
        }

        CurrentState = nextState;
        CurrentState.Enter(owner);
        return true;
    }

    public void Update() => CurrentState?.Update(owner);

    public void ValidateAllRegistered()
    {
        foreach (TEnum value in System.Enum.GetValues(typeof(TEnum)))
        {
            if (!states.ContainsKey(value))
                Debug.LogError($"[FSM] {typeof(TEnum).Name}.{value} 상태가 등록되지 않았습니다.");
        }
    }
}