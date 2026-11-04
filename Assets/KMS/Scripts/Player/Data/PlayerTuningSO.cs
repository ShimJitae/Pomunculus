using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTuningSO", menuName = "Scriptable Objects/PlayerTuningSO")]
public class PlayerTuningSO : ScriptableObject
{
    // 초기 설정값

    [Header("이동 및 회전")]
    public float Acceleration = 60f;
    public float RotateSpeed = 900f;
    public float StoppingDistance = 0.1f;

    [Header("공격 (던지기)")]
    public float AttackDuration = 0.5f;       // 던지기 상태 전체 길이
    public float ThrowReleaseTime = 0.2f;     // 손에서 떠나는 시점
    public float AttackCancelTime = 0.3f;     // 이후 회피/재공격/이동으로 캔슬 가능 (releaseTime보다 커야 함)

    [Header("회피")]
    public float DodgeSpeed = 7f;
    public float DodgeDuration = 0.35f;
    public float DodgeInvincibleTime = 0.25f;
    public float DodgeCooldown = 0.5f;

    [Header("피격")]
    public float HitStunDuration = 0.4f;
    public float KnockbackSpeed = 0f;
}
