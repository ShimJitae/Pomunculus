using System;
using UnityEngine;

namespace ClickMoveExample
{
    /// <summary>
    /// 체력과 피해 처리. 상태 전환은 하지 않고 이벤트만 알린다 (플레이어·적 공용).
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public int maxHp = 100;

        public int Hp { get; private set; }

        /// <summary>true를 돌려주면 피해를 무시한다 (회피 무적, 사망 등). 소유자가 연결</summary>
        public Func<bool> IsInvincible;

        public event Action<Vector3> Damaged;   // 피해를 받았고 아직 살아 있음 (인자: 피해 출처 위치)
        public event Action Died;

        void Awake() => Hp = maxHp;

        public void TakeDamage(int damage, Vector3 sourcePosition)
        {
            if (IsInvincible != null && IsInvincible()) return;

            Hp = Mathf.Max(0, Hp - damage);

            if (Hp == 0)
            {
                Died?.Invoke();
                return;
            }

            Damaged?.Invoke(sourcePosition);
        }

        public void ResetHp() => Hp = maxHp;
    }
}
