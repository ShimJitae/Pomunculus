using UnityEngine;

namespace ClickMoveExample
{
    /// <summary>목표 지점에 포물선으로 떨어지는 투사체를 던진다</summary>
    public class PlayerThrower : MonoBehaviour
    {
        public ThrownProjectile projectilePrefab;
        public Transform throwOrigin;             // 손 위치. 비우면 머리 높이에서 던짐
        public float maxThrowRange = 10f;
        public float throwHorizontalSpeed = 12f;  // 수평 비행 속도 (멀수록 오래 날아감)
        public float minFlightTime = 0.35f;       // 가까이 던질 때 최소 체공 시간 (포물선 높이)
        public int attackDamage = 10;
        public LayerMask enemyLayer;

        public Vector3 ClampToThrowRange(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.magnitude <= maxThrowRange) return target;

            Vector3 clamped = transform.position + flat.normalized * maxThrowRange;
            clamped.y = target.y;
            return clamped;
        }

        /// <summary>목표 지점에 정확히 떨어지는 포물선 속도로 투사체 발사</summary>
        public void ThrowProjectile(Vector3 target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerClickFSM] projectilePrefab이 지정되지 않았습니다.");
                return;
            }

            Vector3 origin = throwOrigin != null
                ? throwOrigin.position
                : transform.position + Vector3.up * 1.5f;

            Vector3 toTarget = target - origin;
            float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
            float flightTime = Mathf.Max(minFlightTime, horizontalDist / throwHorizontalSpeed);

            // 등가속도 운동: target = origin + v*t + 0.5*g*t²  →  v = (target - origin)/t - 0.5*g*t
            Vector3 velocity = toTarget / flightTime - 0.5f * flightTime * Physics.gravity;

            ThrownProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(velocity, attackDamage, enemyLayer, gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, maxThrowRange);   // 최대 사거리
        }
    }
}
