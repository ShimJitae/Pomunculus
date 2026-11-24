using Photon.Pun;
using Pomunculus.Items;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 포션으로 소환되는 플레이어 분신. 방장이 SummonPotionEffect로 생성하고, 소환한 플레이어의 컴퓨터가 소유한다.
/// 소유자 화면에서만 소환자의 이동·회전·애니메이션·던지기를 따라 하고, 위치·애니메이션은 WorldTransformSync/WorldAnimatorSync로,
/// 던지기는 RPC로 다른 화면에 전파한다. 일반 Entity라서 넉백·상태이상 같은 포션 효과를 그대로 받는다 (피해는 받지 않음).
/// 저장되지 않는 임시 오브젝트(ITMPWorldObj)다.
/// </summary>
public class PlayerClone : Entity, ITMPWorldObj, IWorldRPC, IPotionSpawned
{
    private const float MaxFollowStep = 3f;         // 한 프레임에 이보다 많이 움직이면 순간이동으로 보고 따라가지 않는다
    private const float MinThrowFacingTime = 0.2f;  // 던진 뒤 목표를 바라보는 최소 시간
    private const int EmptyBottleId = -1;           // 포션 없이 던진 빈 병
    private const string SummonerIdKey = "summoner";
    private const string StatusKey = "status";

    // 첫 바이트로 RPC 종류를 구분한다. Entity의 RpcRegisterStatusEffect(200)와 겹치지 않게 둔다
    private enum CloneRPCType : byte
    {
        Throw = 1,
    }

    public int Owner { get; private set; }                 // 소환한 플레이어의 ActorNumber
    public int SummonerId { get; private set; } = -1;      // 소환한 플레이어(PlayerFSM)의 월드 ID
    public PlayerFSM Summoner => summoner;
    public bool IsMine => !PhotonNetwork.InRoom
                          || (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == Owner);

    // 상태이상은 소유자 화면에서 확정해 전파한다
    protected override bool HasStateAuthority => IsMine;

    private PlayerFSM summoner;
    private PlayerThrower summonerThrower;
    private Animator summonerAnimator;
    private Animator cloneAnimator;
    private NavMeshAgent agent;
    private WorldTransformSync transformSync;
    private WorldAnimatorSync animatorSync;
    private ThrowTrajectoryPreview aimPreview;   // 소환자가 Shift로 조준할 때 분신 궤적 표시 (없으면 표시 안 함)
    private Vector3 lastSummonerPosition;
    private int lastStateHash;
    private bool hasSpeedParameter;
    private Quaternion throwFacingRotation;      // 던질 때·조준할 때 목표를 바라보는 회전
    private float throwFacingEndTime;            // 이 시간까지는 소환자 대신 목표 방향을 본다

    protected override void Awake()
    {
        base.Awake();
        cloneAnimator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        transformSync = GetComponent<WorldTransformSync>();
        animatorSync = GetComponent<WorldAnimatorSync>();
        aimPreview = GetComponentInChildren<ThrowTrajectoryPreview>(true);
        if (agent != null) agent.updateRotation = false;   // 회전은 직접 정한다
    }

    void OnDestroy() => UnbindSummoner();

    // ═══════════════ 생성 · 소유 ═══════════════

    // WorldManager가 생성 직후 호출한다. 소유 정보는 OnSpawnedByPotion(방장) 또는 LoadData(나머지)에서 채운다
    public override void InitWorldObj(int id, string prefabId) => ApplyOwner();

    // 방장이 생성 직후, 다른 화면에 전파하기 전에 호출한다. 던진 플레이어를 소유자로 정한다
    public void OnSpawnedByPotion(in PotionImpactContext context)
    {
        if (context.Thrower == null || !context.Thrower.TryGetComponent(out PlayerFSM thrower)) return;

        Owner = thrower.Owner;
        SummonerId = thrower.ID;
        ApplyOwner();
        TryBindSummoner();
    }

    // 소유자만 위치·애니메이션을 보내고 NavMeshAgent로 움직인다. 나머지 화면은 동기화 결과만 받는다
    private void ApplyOwner()
    {
        if (transformSync != null) transformSync.IsOwner = () => IsMine;
        if (animatorSync != null) animatorSync.IsOwner = () => IsMine;
        if (agent == null) return;

        agent.enabled = IsMine;
        if (IsMine) agent.Warp(transform.position);
    }

    // 전파·스냅샷에 소유자, 소환자 ID, 상태이상을 담는다
    public override void SaveData(SaveData data)
    {
        data.SetData(Owner);
        data.Child[SummonerIdKey] = global::SaveData.CreateSaveData(SummonerId);

        var status = new SaveData();
        base.SaveData(status);
        data.Child[StatusKey] = status;
    }

    // 받은 데이터로 소유자와 소환자를 복원한다
    public override void LoadData(SaveData data)
    {
        if (data?.Data == null) return;

        Owner = data.GetData<int>();
        if (data.Child.TryGetValue(SummonerIdKey, out SaveData summonerId)) SummonerId = summonerId.GetData<int>();
        if (data.Child.TryGetValue(StatusKey, out SaveData status)) base.LoadData(status);
        ApplyOwner();
        TryBindSummoner();
    }

    // 소환자를 월드 ID로 찾아 연결한다. 이미 연결됐거나 찾으면 true
    private bool TryBindSummoner()
    {
        if (summoner != null) return true;
        if (SummonerId < 0 || WorldManager.Instance.Find(SummonerId) is not PlayerFSM found) return false;

        summoner = found;
        summonerThrower = found.Thrower;
        summonerAnimator = found.GetComponentInChildren<Animator>();
        hasSpeedParameter = HasFloatParameter(summonerAnimator, PlayerAnimator.SpeedParam)
                            && HasFloatParameter(cloneAnimator, PlayerAnimator.SpeedParam);
        lastSummonerPosition = found.transform.position;
        if (summonerThrower != null) summonerThrower.OnProjectileSpawned += MirrorThrow;
        return true;
    }

    private void UnbindSummoner()
    {
        if (summonerThrower != null) summonerThrower.OnProjectileSpawned -= MirrorThrow;
        summonerThrower = null;
        summoner = null;
    }

    // ═══════════════ 피해 무시 ═══════════════

    // 분신은 피해를 받지 않는다 (넉백·상태이상은 받는다)
    public override void TakeDamage(int damage) { }
    public override void TakeDamage(int damage, Vector3 sourcePosition) { }

    // ═══════════════ 소유자 화면에서 따라 하기 ═══════════════

    void LateUpdate()
    {
        if (!TryBindSummoner() || !IsMine || summoner.IsDead)
        {
            if (aimPreview != null) aimPreview.Hide();
            return;   // 제거는 방장의 PotionSummonLifetime이 한다
        }

        FollowMovement();
        MirrorAnimation();
        UpdateAimPreview();
    }

    /// <summary>
    /// 소환자가 스스로 움직인 만큼 같은 방향으로 움직인다. NavMeshAgent로 움직여 NavMesh 밖으로 나가지 않는다.
    /// 소환자가 포션 넉백으로 밀린 만큼은 빼고, 피격 경직(Hit) 중에는 따라가지 않는다.
    /// </summary>
    private void FollowMovement()
    {
        Vector3 summonerPosition = summoner.transform.position;
        Vector3 step = summonerPosition - lastSummonerPosition;
        lastSummonerPosition = summonerPosition;
        transform.rotation = Time.time < throwFacingEndTime ? throwFacingRotation : summoner.transform.rotation;

        if (summoner.CurrentStateType == PlayerStateType.Hit) return;
        step -= GetSummonerKnockbackStep();
        if (step.sqrMagnitude < 0.000001f || step.magnitude > MaxFollowStep) return;

        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(step);
        else transform.position += step;
    }

    // 소환자가 이번 프레임 포션 넉백으로 밀린 양. 없으면 0
    private Vector3 GetSummonerKnockbackStep()
    {
        if (!summoner.TryGetComponent(out PotionKnockbackMover mover) || mover.LastStepFrame != Time.frameCount)
            return Vector3.zero;
        return mover.LastStepDisplacement;
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

    /// <summary>
    /// 소환자가 Shift로 조준 중이면 분신이 던질 궤적을 그리고 목표를 바라본다.
    /// 목표는 실제로 던질 때와 같게 분신 위치 기준 사거리로 자른다. 소환자 본인 화면에만 보인다.
    /// </summary>
    private void UpdateAimPreview()
    {
        if (aimPreview == null) return;
        if (!ThrowAimIndicator.TryGetAimPoint(summoner, out Vector3 mousePoint))
        {
            aimPreview.Hide();
            return;
        }

        Vector3 goal = summonerThrower.ClampToThrowRange(transform.position, mousePoint);
        FaceTarget(goal, 0f);

        Vector3 origin = summonerThrower.GetThrowOrigin() + (transform.position - summoner.transform.position);
        aimPreview.Show(summonerThrower, origin, goal, summoner.Actions.GetThrowablePotion());
    }

    // ═══════════════ 던지기 ═══════════════

    /// <summary>
    /// 소환자가 던지면(모든 화면에서 호출됨) 소유자 화면만 분신의 발사 정보를 계산해 모두에게 보낸다.
    /// 목표가 분신 사거리 밖이면 같은 사거리 끝으로 당기고, 던지기 동작이 끝날 때까지 목표를 바라본다.
    /// 분신을 소환하는 포션은 따라 던지지 않는다.
    /// </summary>
    private void MirrorThrow(Vector3 origin, Vector3 target, PotionData potion)
    {
        if (!IsMine || summoner == null || SpawnsClone(potion)) return;

        Vector3 clampedTarget = summonerThrower.ClampToThrowRange(transform.position, target);
        FaceTarget(clampedTarget, GetRemainingThrowTime());

        Vector3 cloneOrigin = origin + (transform.position - summoner.transform.position);
        Vector3 velocity = summonerThrower.CalculateThrowVelocity(cloneOrigin, clampedTarget, out Vector3 gravity);

        PacketData packet = new();
        packet.Write((byte)CloneRPCType.Throw);
        packet.Write(cloneOrigin);
        packet.Write(velocity);
        packet.Write(gravity);
        packet.Write(potion != null ? potion.Id : EmptyBottleId);
        this.SendRPC(packet, WorldRPCTarget.All);   // 내 화면도 ReceiveRPC를 거쳐 생성된다
    }

    // RPCHelper가 ID로 찾아서 호출한다. 보낸 순서 그대로 읽는다
    public void ReceiveRPC(PacketData packet)
    {
        byte rpcType = packet.ReadByte();
        if (rpcType == RpcRegisterStatusEffect)
        {
            ReceiveRegisterStatusEffect(packet);
            return;
        }

        switch ((CloneRPCType)rpcType)
        {
            case CloneRPCType.Throw:
                Vector3 origin = packet.ReadVector3();
                Vector3 velocity = packet.ReadVector3();
                Vector3 gravity = packet.ReadVector3();
                int potionId = packet.ReadInt();
                if (TryBindSummoner() && summonerThrower != null)
                    summonerThrower.LaunchProjectile(origin, velocity, gravity, FindPotion(potionId));
                break;
        }
    }

    // holdTime 동안 소환자 회전 대신 목표 방향(수평)을 보게 한다
    private void FaceTarget(Vector3 target, float holdTime)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        throwFacingRotation = Quaternion.LookRotation(direction);
        transform.rotation = throwFacingRotation;
        throwFacingEndTime = Mathf.Max(throwFacingEndTime, Time.time + holdTime);
    }

    // 던지기 동작에서 발사 이후 남은 시간. 이 시간 동안 분신이 목표를 바라본다
    private float GetRemainingThrowTime()
    {
        if (summoner.Tuning == null) return MinThrowFacingTime;
        return Mathf.Max(MinThrowFacingTime, summoner.Tuning.AttackDuration - summoner.Tuning.ThrowReleaseTime);
    }

    // ═══════════════ 유틸 ═══════════════

    // Id로 포션을 찾는다. 빈 병이면 null
    private static PotionData FindPotion(int potionId)
    {
        if (potionId == EmptyBottleId || PotionDatabase.Instance == null) return null;
        return PotionDatabase.Instance.Find(potionId);
    }

    // 이 포션이 분신을 만드는 효과를 가졌는지 확인한다 (분신이 분신을 계속 만드는 것 방지)
    private static bool SpawnsClone(PotionData potion)
    {
        if (potion == null) return false;

        foreach (PotionEffect effect in potion.Effects)
        {
            if (effect is SummonPotionEffect summon && summon.SpawnsComponent<PlayerClone>()) return true;
        }
        return false;
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
