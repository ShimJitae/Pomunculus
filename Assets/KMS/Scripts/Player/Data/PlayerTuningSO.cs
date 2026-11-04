using UnityEngine;

// 조작감 튜닝 값(이동·공격·회피·피격 타이밍). 실행 중에는 읽기만 한다.
[CreateAssetMenu(fileName = "PlayerTuningSO", menuName = "Scriptable Objects/PlayerTuningSO")]
public class PlayerTuningSO : ScriptableObject
{
    [Header("이동 및 회전")]
    [SerializeField, Min(0f)] private float acceleration = 60f;           // NavMeshAgent 가속도
    public float Acceleration => acceleration;
    [SerializeField, Min(0f)] private float rotateSpeed = 900f;           // 초당 회전 각도
    public float RotateSpeed => rotateSpeed;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.1f;      // 목적지 도착으로 판단하는 거리
    public float StoppingDistance => stoppingDistance;

    [Header("공격 (던지기)")]
    [SerializeField, Min(0f)] private float attackDuration = 0.5f;        // 던지기 상태 전체 길이
    public float AttackDuration => attackDuration;
    [SerializeField, Min(0f)] private float throwReleaseTime = 0.2f;      // 손에서 떠나는 시점
    public float ThrowReleaseTime => throwReleaseTime;
    [SerializeField, Min(0f)] private float attackCancelTime = 0.3f;      // 이후 회피/재공격/이동으로 캔슬 가능 (releaseTime보다 커야 함)
    public float AttackCancelTime => attackCancelTime;

    [Header("회피")]
    [SerializeField, Min(0f)] private float dodgeSpeed = 7f;              // 회피 이동 속도
    public float DodgeSpeed => dodgeSpeed;
    [SerializeField, Min(0f)] private float dodgeDuration = 0.35f;        // 회피 상태 전체 길이
    public float DodgeDuration => dodgeDuration;
    [SerializeField, Min(0f)] private float dodgeInvincibleTime = 0.25f;  // 회피 시작부터 무적이 유지되는 시간
    public float DodgeInvincibleTime => dodgeInvincibleTime;
    [SerializeField, Min(0f)] private float dodgeCooldown = 0.5f;         // 다음 회피까지 대기 시간
    public float DodgeCooldown => dodgeCooldown;

    [Header("피격")]
    [SerializeField, Min(0f)] private float hitStunDuration = 0.4f;       // 피격 경직 시간
    public float HitStunDuration => hitStunDuration;
    [SerializeField, Min(0f)] private float knockbackSpeed = 0f;          // 피격 시 밀려나는 속도 (0이면 넉백 없음)
    public float KnockbackSpeed => knockbackSpeed;
}
