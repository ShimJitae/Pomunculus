using System;
using UnityEngine;
using UnityEngine.Serialization;

// 체력과 피해 처리. 상태 전환은 하지 않고 이벤트만 알린다 (플레이어·적 공용).
    public class Health : MonoBehaviour, IDamageable
    {
    [FormerlySerializedAs("maxHp")]
    public int MaxHp = 100;

        public int Hp { get; private set; }

    // true를 돌려주면 피해를 무시한다 (회피 무적, 사망 등). 소유자가 연결
        public Func<bool> IsInvincible;

    public event Action<Vector3> OnDamaged;   // 피해를 받았고 아직 살아 있음 (인자: 피해 출처 위치)
    public event Action OnDied;

    void Awake() => Hp = MaxHp;

        public void TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (IsInvincible != null && IsInvincible()) return;

            Hp = Mathf.Max(0, Hp - damage);

            if (Hp == 0)
            {
            OnDied?.Invoke();
                return;
            }

        OnDamaged?.Invoke(sourcePosition);
        }

    public void ResetHp() => Hp = MaxHp;
}
