[System.Flags]
public enum PotionTargetFlags
{
    None = 0,
    Ally = 1,
    Enemy = 2,
    All = Ally | Enemy
}
