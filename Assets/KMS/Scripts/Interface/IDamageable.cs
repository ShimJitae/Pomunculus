using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 대상 (플레이어, 적 공통)
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage, Vector3 sourcePosition);
}