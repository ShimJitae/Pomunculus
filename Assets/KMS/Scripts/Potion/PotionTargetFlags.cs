[System.Flags]
public enum PotionTargetFlags
{
    None = 0,
    Ally = 1, // 아군 (나를 제외한 다른 플레이어)
    Enemy = 2, // 적 (몬스터)
    Self = 4, // 나
    Gatherable = 8, // 채집물 (플레이어·몬스터가 아닌 Entity)
    AllExceptSelf = Ally | Enemy | Gatherable, // 나를 제외한 모두
    All = Ally | Enemy | Self | Gatherable // 전부
}
