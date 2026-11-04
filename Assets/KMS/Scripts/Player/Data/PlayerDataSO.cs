using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Scriptable Objects/PlayerDataSO")]
public class PlayerDataSO : EntityDataSO
{
    [Header("스테미나")]
    [SerializeField] private int stamina;
    public int Stamina => stamina;
    [SerializeField] private float staminaRegenPerSecond;
    public float StaminaRegenPerSecond => staminaRegenPerSecond;
    [SerializeField] private float staminaRegenDelay;
    public float StaminaRegenDelay => staminaRegenDelay;
    [SerializeField] private int sodgeStaminaCost;
    public int SodgeStaminaCost => sodgeStaminaCost;
    [SerializeField] private int throwStaminaCost;
    public int ThrowStaminaCost => throwStaminaCost;

    [Header("속도")]
    [SerializeField] private float moveSpeed;
    public float MoveSpeed => moveSpeed;

}
