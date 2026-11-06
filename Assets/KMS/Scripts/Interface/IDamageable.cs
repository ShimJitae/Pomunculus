using UnityEngine;

// 데미지를 받을 수 있는 대상 (플레이어, 적 공통)
public interface IDamageable
{
    public void TakeDamage(int damage, Vector3 sourcePosition);
}