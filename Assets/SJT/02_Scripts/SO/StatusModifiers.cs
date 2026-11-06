using System;
using UnityEngine;

/// <summary>
/// 활성 상태이상 전체가 대상에게 적용하는 수치 묶음이다.
/// StatusEffectController가 걸림·중첩·만료·복원 때 다시 계산하고, 값이 바뀌면 OnModifiersChanged로 알린다.
/// </summary>
public struct StatusModifiers : IEquatable<StatusModifiers>
{
    public float MoveSpeedRatio;    // 이동속도 비율. 기본값 1.0 (100%)
    public float HealReceivedRatio; // 회복량 비율. 기본값 1.0 (100%)
    public float DamageTakenRatio;  // 받는 피해량 비율. 기본값 1.0 (100%)
    public bool CanMove;            // 이동 가능 여부. 기본값 true
    public bool CanDodge;           // 회피 가능 여부. 기본값 true
    public bool CanAttack;          // 공격 가능 여부. 기본값 true
    public bool IsInvincible;       // 무적 여부. 기본값 false

    /// <summary>상태이상이 하나도 없을 때의 값이다.</summary>
    public static StatusModifiers Default => new StatusModifiers
    {
        MoveSpeedRatio = 1f,
        HealReceivedRatio = 1f,
        DamageTakenRatio = 1f,
        CanMove = true,
        CanDodge = true,
        CanAttack = true,
        IsInvincible = false,
    };

    public bool Equals(StatusModifiers other)
    {
        return Mathf.Approximately(MoveSpeedRatio, other.MoveSpeedRatio)
            && Mathf.Approximately(HealReceivedRatio, other.HealReceivedRatio)
            && Mathf.Approximately(DamageTakenRatio, other.DamageTakenRatio)
            && CanMove == other.CanMove
            && CanDodge == other.CanDodge
            && CanAttack == other.CanAttack
            && IsInvincible == other.IsInvincible;
    }

    public override bool Equals(object obj) => obj is StatusModifiers other && Equals(other);

    // Approximately 비교와 어긋나지 않도록 float 필드는 해시에서 제외한다.
    public override int GetHashCode() => HashCode.Combine(CanMove, CanDodge, CanAttack, IsInvincible);

    public static bool operator ==(StatusModifiers a, StatusModifiers b) => a.Equals(b);
    public static bool operator !=(StatusModifiers a, StatusModifiers b) => !a.Equals(b);
}
