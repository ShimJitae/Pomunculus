using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Scriptable Objects/PlayerDataSO")]
public class PlayerDataSO : EntityDataSO
{
    [Header("스태미나")]
    [SerializeField] private int maxStamina = 100;
    public int MaxStamina => maxStamina;
    [SerializeField] private float staminaRegenPerSecond;
    public float StaminaRegenPerSecond => staminaRegenPerSecond;
    [SerializeField] private float staminaRegenDelay;
    public float StaminaRegenDelay => staminaRegenDelay;
    [SerializeField] private int dodgeStaminaCost;
    public int DodgeStaminaCost => dodgeStaminaCost;
    [SerializeField] private int throwStaminaCost;
    public int ThrowStaminaCost => throwStaminaCost;

    //[Header("속도")]
    //[SerializeField] private float moveSpeed = 5f;
    //public float MoveSpeed => moveSpeed;

}
