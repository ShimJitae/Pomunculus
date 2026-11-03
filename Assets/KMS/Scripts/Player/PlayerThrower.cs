using UnityEngine;

// 목표 지점에 포물선으로 떨어지는 투사체를 던진다
public class PlayerThrower : MonoBehaviour
{
    // FormerlySerializedAs: 프리팹·씬에 대문자 이름으로 저장된 기존 값을 그대로 불러오기 위함
    [SerializeField] private ThrownProjectile projectilePrefab;
    [SerializeField] private Transform throwOrigin;                  // 손 위치. 비우면 머리 높이에서 던짐
    [SerializeField] private float maxThrowRange = 10f;
    [SerializeField] private float throwHorizontalSpeed = 12f;       // 기준 수평 속도. 궤적 모양을 정함 (배율 1일 때의 실제 속도)
    [SerializeField] private float minFlightTime = 0.35f;            // 가까이 던질 때 최소 체공 시간. 발밑에 던질 때 떨어지는 느낌
    [SerializeField, Min(0.01f)] private float flightSpeedMultiplier = 1f;       // 궤적은 그대로, 날아가는 빠르기만 배율로 조절
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;

    // 목표 지점이 최대 사거리 밖이면 같은 방향의 사거리 끝으로 당긴다 (높이는 유지)
    public Vector3 ClampToThrowRange(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.magnitude <= maxThrowRange) return target;

        Vector3 clamped = transform.position + flat.normalized * maxThrowRange;
        clamped.y = target.y;
        return clamped;
    }

    // 발사 위치. throwOrigin이 없으면 머리 높이
    public Vector3 GetThrowOrigin()
    {
        return throwOrigin != null
            ? throwOrigin.position
            : transform.position + Vector3.up * 1.5f;
    }

    /// <summary>
    /// 목표 지점에 정확히 떨어지는 초기 속도, 체공 시간, 투사체에 적용할 중력을 계산한다.
    /// 궤적은 실제 중력 기준으로 정하고, flightSpeedMultiplier만큼 같은 궤적을 더 빠르게(느리게) 지나가도록
    /// 속도는 m배, 중력은 m²배 한다. 배율이 1이면 실제 중력 그대로다.
    /// </summary>
    public Vector3 CalculateLaunchVelocity(Vector3 origin, Vector3 target, out float flightTime, out Vector3 gravity)
    {
        Vector3 toTarget = target - origin;
        float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float baseFlightTime = Mathf.Max(minFlightTime, horizontalDist / throwHorizontalSpeed);

        // 등가속도 운동: target = origin + v*t + 0.5*g*t²  →  v = (target - origin)/t - 0.5*g*t
        Vector3 baseVelocity = toTarget / baseFlightTime - 0.5f * baseFlightTime * Physics.gravity;

        float multiplier = flightSpeedMultiplier;
        flightTime = baseFlightTime / multiplier;
        gravity = Physics.gravity * (multiplier * multiplier);
        return baseVelocity * multiplier;
    }

    // 목표 지점에 정확히 떨어지는 포물선 속도로 투사체 발사
    public void ThrowProjectile(Vector3 target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerThrower] projectilePrefab이 지정되지 않았습니다.");
            return;
        }

        Vector3 origin = GetThrowOrigin();
        Vector3 velocity = CalculateLaunchVelocity(origin, target, out _, out Vector3 gravity);

        ThrownProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(velocity, gravity, attackDamage, enemyLayer, gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, maxThrowRange);   // 최대 사거리
    }
}
