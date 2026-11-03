public interface IState<TEnum, TOwner> where TEnum : System.Enum
{
    TEnum StateType { get; }      // 이 상태의 이름표

    void Enter(TOwner owner);     // 상태 진입 시 1회
    void Update(TOwner owner);    // 상태 유지 중 매 프레임
    void Exit(TOwner owner);      // 상태 종료 시 1회
}
