using UnityEngine;
using UnityEngine.Serialization;

// 목표 지점에 포물선으로 떨어지는 투사체를 던진다
public class PlayerThrower : MonoBehaviour
{
    [FormerlySerializedAs("projectilePrefab")]
    public ThrownProjectile ProjectilePrefab;
    [FormerlySerializedAs("throwOrigin")]
    public Transform ThrowOrigin;             // 손 위치. 비우면 머리 높이에서 던짐
    [FormerlySerializedAs("maxThrowRange")]
    public float MaxThrowRange = 10f;
    [FormerlySerializedAs("throwHorizontalSpeed")]
    public float ThrowHorizontalSpeed = 12f;  // 수평 비행 속도 (멀수록 오래 날아감)
    [FormerlySerializedAs("minFlightTime")]
    public float MinFlightTime = 0.35f;       // 가까이 던질 때 최소 체공 시간 (포물선 높이)
    [FormerlySerializedAs("attackDamage")]
    public int AttackDamage = 10;
    [FormerlySerializedAs("enemyLayer")]
    public LayerMask EnemyLayer;

    public Vector3 ClampToThrowRange(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.magnitude <= MaxThrowRange) return target;

        Vector3 clamped = transform.position + flat.normalized * MaxThrowRange;
        clamped.y = target.y;
        return clamped;
    }

    // 목표 지점에 정확히 떨어지는 포물선 속도로 투사체 발사
    public void ThrowProjectile(Vector3 target)
    {
        if (ProjectilePrefab == null)
        {
            Debug.LogWarning("[PlayerFSM] projectilePrefab이 지정되지 않았습니다.");
            return;
        }

        Vector3 origin = ThrowOrigin != null
            ? ThrowOrigin.position
            : transform.position + Vector3.up * 1.5f;

        Vector3 toTarget = target - origin;
        float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float flightTime = Mathf.Max(MinFlightTime, horizontalDist / ThrowHorizontalSpeed);

        // 등가속도 운동: target = origin + v*t + 0.5*g*t²  →  v = (target - origin)/t - 0.5*g*t
        Vector3 velocity = toTarget / flightTime - 0.5f * flightTime * Physics.gravity;

        ThrownProjectile projectile = Instantiate(ProjectilePrefab, origin, Quaternion.identity);
        projectile.Launch(velocity, AttackDamage, EnemyLayer, gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, MaxThrowRange);   // 최대 사거리
    }
}
