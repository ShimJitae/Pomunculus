using Pomunculus.Items;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 포션으로 생성된 플레이어 분신. 소환한 플레이어의 이동·회전·애니메이션·던지기를 그대로 따라 한다.
/// 소환자의 위치·애니메이션·던지기는 이미 모든 클라이언트에 동기화되므로, 각 클라이언트가 로컬로 따라 하면 모두에게 같게 보인다.
/// 분신은 맞지 않고 막지도 않도록 콜라이더를 모두 끈다.
/// </summary>
public class PlayerClone : MonoBehaviour, IPotionSpawned
{
    private const float NavMeshSnapDistance = 2f;   // 생성·이동 후 NavMesh를 찾을 최대 거리
    private const float MaxFollowStep = 3f;         // 한 프레임에 이보다 많이 움직이면 순간이동으로 보고 따라가지 않는다

    private PlayerFSM summoner;
    private PlayerThrower summonerThrower;
    private Animator summonerAnimator;
    private Animator cloneAnimator;
    private Vector3 lastSummonerPosition;
    private int lastStateHash;
    private bool hasSpeedParameter;
    private bool isOnNavMesh;
    private bool hasSummoner;

    void Awake()
    {
        cloneAnimator = GetComponentInChildren<Animator>();
        foreach (Collider cloneCollider in GetComponentsInChildren<Collider>(true))
            cloneCollider.enabled = false;
    }

    // 생성 직후 소환자를 기억하고 던지기를 구독한다. 소환자가 플레이어가 아니면 바로 사라진다
    public void OnSpawnedByPotion(in PotionImpactContext context)
    {
        if (context.Thrower == null || !context.Thrower.TryGetComponent(out summoner))
        {
            Destroy(gameObject);
            return;
        }

        hasSummoner = true;
        summonerThrower = summoner.Thrower;
        summonerAnimator = summoner.GetComponentInChildren<Animator>();
        hasSpeedParameter = HasFloatParameter(summonerAnimator, PlayerAnimator.SpeedParam)
                            && HasFloatParameter(cloneAnimator, PlayerAnimator.SpeedParam);
        lastSummonerPosition = summoner.transform.position;
        transform.rotation = summoner.transform.rotation;
        isOnNavMesh = TrySnapToNavMesh(transform.position, out Vector3 snapped);
        if (isOnNavMesh) transform.position = snapped;

        if (summonerThrower != null) summonerThrower.OnProjectileSpawned += MirrorThrow;
    }

    void OnDestroy()
    {
        if (summonerThrower != null) summonerThrower.OnProjectileSpawned -= MirrorThrow;
    }

    void LateUpdate()
    {
        if (!hasSummoner) return;
        if (summoner == null || summoner.IsDead)   // 소환자가 나갔거나 죽으면 같이 사라진다
        {
            Destroy(gameObject);
            return;
        }

        FollowMovement();
        MirrorAnimation();
    }

    // 소환자가 이번 프레임 움직인 만큼 같은 방향으로 움직인다. NavMesh 경계에서는 멈춘다
    private void FollowMovement()
    {
        Vector3 summonerPosition = summoner.transform.position;
        Vector3 step = summonerPosition - lastSummonerPosition;
        lastSummonerPosition = summonerPosition;
        transform.rotation = summoner.transform.rotation;

        if (step.sqrMagnitude < 0.000001f || step.magnitude > MaxFollowStep) return;

        Vector3 target = transform.position + step;
        if (!isOnNavMesh)
        {
            transform.position = target;
            return;
        }

        if (NavMesh.Raycast(transform.position, target, out NavMeshHit edgeHit, NavMesh.AllAreas))
            target = edgeHit.position;
        if (TrySnapToNavMesh(target, out Vector3 snapped))
            target = snapped;

        transform.position = target;
    }

    // 소환자의 애니메이션 상태가 바뀌면 같은 상태·진행도로 재생하고, 이동 속도 값은 매 프레임 복사한다
    private void MirrorAnimation()
    {
        if (summonerAnimator == null || cloneAnimator == null) return;

        AnimatorStateInfo state = summonerAnimator.IsInTransition(0)
            ? summonerAnimator.GetNextAnimatorStateInfo(0)
            : summonerAnimator.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash != lastStateHash)
        {
            cloneAnimator.Play(state.shortNameHash, 0, state.normalizedTime);
            lastStateHash = state.shortNameHash;
        }

        if (hasSpeedParameter)
            cloneAnimator.SetFloat(PlayerAnimator.SpeedParam, summonerAnimator.GetFloat(PlayerAnimator.SpeedParam));
    }

    // 소환자가 던진 투사체를 분신 위치 기준으로 하나 더 던진다. 분신을 만드는 포션은 따라 던지지 않는다
    private void MirrorThrow(Vector3 origin, Vector3 velocity, Vector3 gravity, PotionData potion)
    {
        if (summoner == null || SpawnsClone(potion)) return;

        Vector3 offset = transform.position - summoner.transform.position;
        summonerThrower.LaunchProjectile(origin + offset, velocity, gravity, potion);
    }

    // 이 포션이 분신을 만드는 효과를 가졌는지 확인한다 (분신이 분신을 계속 만드는 것 방지)
    private static bool SpawnsClone(PotionData potion)
    {
        if (potion == null) return false;

        foreach (PotionEffect effect in potion.Effects)
        {
            if (effect is SpawnObjectPotionEffect spawnEffect && spawnEffect.SpawnsComponent<PlayerClone>())
                return true;
        }
        return false;
    }

    // 지점 근처의 NavMesh 위치를 찾는다
    private static bool TrySnapToNavMesh(Vector3 point, out Vector3 snapped)
    {
        bool found = NavMesh.SamplePosition(point, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas);
        snapped = found ? hit.position : point;
        return found;
    }

    // Animator에 해당 float 파라미터가 있는지 확인한다 (없는 파라미터를 읽으면 경고가 찍힌다)
    private static bool HasFloatParameter(Animator animator, int parameterHash)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Float)
                return true;
        }
        return false;
    }
}
