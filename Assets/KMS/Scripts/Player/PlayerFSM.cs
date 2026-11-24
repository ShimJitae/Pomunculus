using Photon.Pun;
using Pomunculus.Items;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 클릭 이동 플레이어 (FSM Owner)
///  - 우클릭 : 이동 (누르고 있으면 커서를 따라감). 적을 클릭해도 이동만 한다.
///  - 좌클릭 : 클릭한 지면 위치로 물체를 던짐
///  - Space  : 커서 방향으로 회피
/// 입력·이동·던지기·체력은 각 컴포넌트가 담당하고, 이 클래스는 FSM 구동과 상태 간 공유 데이터만 맡는다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMotor), typeof(PlayerThrower))]
[RequireComponent(typeof(Inventory), typeof(PlayerAnimator))]
public class PlayerFSM : Entity, IPlayerObj, IHealable, IShieldable, IWorldRPC
{
    [SerializeField] private PlayerTuningSO tuning;
    public PlayerTuningSO Tuning => tuning;
    public PlayerDataSO Data => so as PlayerDataSO;

    [Header("위치")]
    [SerializeField] private Transform spawnPos;
    private Vector3 startPos;
    [Header("디버그")]
    [SerializeField] private PlayerStateType currentStateView;

    private StateMachine<PlayerStateType, PlayerFSM> fsm;

    // ───── 네트워크 시 사용하는 컴포넌트 ─────
    private WorldTransformSync worldTransform;
    private WorldAnimatorSync worldAnimator;

    // ───── 상태들이 사용하는 컴포넌트 ─────
    public PlayerStats Stats { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public PlayerMotor Motor { get; private set; }
    public PlayerThrower Thrower { get; private set; }
    public PlayerInteractor Interactor { get; private set; }   // 없을 수 있음 (null 허용)
    public Inventory Inventory { get; private set; }
    public PotionLoadout Loadout { get; } = new PotionLoadout();
    public RecipeJournal Recipes { get; } = new RecipeJournal();
    public PlayerAnimator Animator { get; private set; }
    public PlayerActionGate Actions { get; private set; }

    // ───── 상태들이 읽어 가는 데이터 ─────
    public PlayerStateType CurrentStateType => fsm.CurrentStateType;
    public PlayerStateType PreviousStateType => fsm.PreviousStateType;
    public bool IsDead => CheckDie();
    public Vector3 LastHitSourcePosition { get; private set; }

    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    private WorldMapUI worldMapUI;   // 씬의 지도 UI. 처음 Tab을 누를 때 찾아서 캐시
    private TestItemGiver itemGiver; // 디버그 아이템 추가 키(F1/F2)용. 없으면 키가 무시됨

    private bool IsDamageBlocked =>
    IsInvincible || CheckDie() ||
    (fsm.CurrentState is PlayerStateBase state && state.IsInvincible(this));

    public int Owner { get; set; }
    public bool IsMine => !PhotonNetwork.InRoom ||
                      (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == Owner);

    protected override bool HasStateAuthority { get => IsMine; }

    public event Action OnDied;
    public event Action OnRevived;
    public event Action OnRespawnRequested;
    public event Action<int, int> OnItemGranted;   // 방장이 확정한 아이템 지급 (itemId, amount). 내 플레이어에서만 발생

    //public void RequestRespawn()
    //{
    //    if (IsDead) OnRespawnRequested?.Invoke();
    //}

    protected override void Awake()
    {
        base.Awake();

        if (Data == null) Debug.LogError("[PlayerFSM] so 슬롯에 PlayerDataSO가 필요합니다.", this);
        if (tuning == null) Debug.LogError("[PlayerFSM] tuning 슬롯이 비어 있습니다.", this);

        worldTransform = GetComponent<WorldTransformSync>();
        worldAnimator = GetComponent<WorldAnimatorSync>();

        Input = GetComponent<PlayerInputReader>();
        Motor = GetComponent<PlayerMotor>();
        Thrower = GetComponent<PlayerThrower>();
        Interactor = GetComponent<PlayerInteractor>();
        Inventory = GetComponentInChildren<Inventory>();
        itemGiver = GetComponentInChildren<TestItemGiver>();
        Animator = GetComponent<PlayerAnimator>();

        Motor.Initialize(Tuning, Data.GetSpeed());
        Stats = new PlayerStats(this, Data);
        Actions = new PlayerActionGate(this);

        Stats.OnShieldChanged += HandleShieldChanged;
        Inventory.Changed += SendInventorySync;
        Loadout.OnEquipChanged += HandleLoadoutEquipChanged;
        Recipes.OnChanged += SendRecipeSync;

        startPos = transform.position;

        fsm = new StateMachine<PlayerStateType, PlayerFSM>(this);
        fsm.AddState(new PlayerIdleState());
        fsm.AddState(new PlayerMoveState());
        fsm.AddState(new PlayerAttackState());
        fsm.AddState(new PlayerDodgeState());
        fsm.AddState(new PlayerHitState());
        fsm.AddState(new PlayerDeadState());
        fsm.ValidateAllRegistered();
    }


    // ══════════════════GameManager 관리 ═══════════════════════
    // 오브젝트 소환 (Awake 초기화)를 여기서
    // id -> 월드 매니저쪽에서 지정해주는 아이디
    // prefabId -> 프리팹 이름이 곧 아이디
    public override void InitWorldObj(int id, string prefabId)
    {
        //ID = id;
        //PrefabId = prefabId;
        SetOwner(Owner);
    }

    // ActorNumber 가져오기
    public void SetOwner(int actor)
    {
        Owner = actor;
        if (worldTransform != null) worldTransform.IsOwner = () => IsMine;
        if (worldAnimator != null) worldAnimator.IsOwner = () => IsMine;
        Motor.SetAgentEnabled(IsMine);

        // 다른 플레이어와 포션 색상 다르게 하기 (임시)
        skinnedMeshRenderer.material.color = IsMine ? Color.red : Color.green;
    }

    private void Start()
    {
        if (!IsMine && TryGetComponent(out AudioListener listener))
        {
            listener.enabled = false;
            Destroy(listener);
        }

        SetState(IsDead ? PlayerStateType.Dead : PlayerStateType.Idle);
    }

    void Update()
    {
        if (!IsMine) return;

        Input.ReadInput();
        if (Input.ConsumePotionSlotSelect(out int slot)) Loadout.SelectSlot(slot);
        if (Input.IsWorldMapPressed()) ToggleWorldMap();
        ReadDebugKeys();
        fsm.Update();
        Stats.Tick(Time.deltaTime);

        Motor.SetMoveSpeed(Stats.MoveSpeed);
        currentStateView = fsm.CurrentStateType;
    }

    public void SetState(PlayerStateType next, bool allowReenter = false)
        => fsm.ChangeState(next, allowReenter);

    // ═══════════════════════ 입력 ═══════════════════════

    // Tab: 지도 열기/닫기. 플레이어는 런타임에 스폰되므로 씬의 지도 UI를 처음 쓸 때 찾는다
    void ToggleWorldMap()
    {
        if (worldMapUI == null)
            worldMapUI = FindAnyObjectByType<WorldMapUI>(FindObjectsInactive.Include);
        if (worldMapUI == null) return;

        if (worldMapUI.IsOpen) worldMapUI.Close();
        else worldMapUI.Open();
    }

    // 테스트용 키: H = 10 피해(에디터 전용), F1 = 모든 재료 50개, F2 = 모든 포션 50개
    void ReadDebugKeys()
    {
#if UNITY_EDITOR
        if (Input.IsDebugDamagePressed()) TakeDamage(10, transform.position + transform.forward);
#endif
        if (itemGiver != null)
        {
            if (Input.IsDebugGiveMaterialsPressed()) itemGiver.GiveAllMaterials();
            if (Input.IsDebugGivePotionsPressed()) itemGiver.GiveAllPotion();
        }
    }


    // 받는 피해 비율을 적용하고, 보호막이 먼저 흡수한 뒤 남은 피해만 HP에 입힌다.
    public override void TakeDamage(int damage)
    {
        if (!IsMine || IsDamageBlocked || IsDead) return;

        int scaledDamage = Mathf.CeilToInt(damage * DamageTakenRatio);
        int remainingDamage = Stats.AbsorbDamage(scaledDamage);
        if (remainingDamage <= 0) return;

        int hpBefore = CurrentHp;
        base.TakeDamage(remainingDamage);
        if (CurrentHp != hpBefore)
        {
            SendHpSync();
            Stats.NotifyHpChanged();
        }
    }


    // 투사체·공격 피해. 맞은 위치로 넉백 + 경직
    public override void TakeDamage(int damage, Vector3 sourcePosition)
    {
        if (!IsMine) { SendDamageRequest(damage, sourcePosition); return; }

        int hpBefore = CurrentHp;
        TakeDamage(damage);
        if (CurrentHp < hpBefore && !CheckDie())
        {
            LastHitSourcePosition = sourcePosition;
            SetState(PlayerStateType.Hit, allowReenter: true);
        }
    }

    // 받는 회복 비율(독 등)을 적용해 회복하고, HP가 바뀌었으면 알린다
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (!IsMine) { SendHealRequest(amount); return; }
        if (IsDead) return;   // 사망 여부는 소유자 화면에서만 판단한다 (복사본 상태는 늦을 수 있음)

        int hpBefore = CurrentHp;
        CurrentHp += Mathf.CeilToInt(amount * HealReceivedRatio);   // setter가 최대 HP로 제한
        if (CurrentHp != hpBefore)
        {
            SendHpSync();
            Stats.NotifyHpChanged();
        }

    }

    private void HandleShieldChanged(int shield) => SendShieldSync();

    public void GetShield(int amount, float duration)
    {
        if (amount <= 0) return;
        if (!IsMine) { SendShieldRequest(amount, duration); return; }
        if (IsDead) return;

        Stats.AddShield(amount, duration);
    }

    protected override void OnDie()
    {
        Stats.ClearShield();
        SetState(PlayerStateType.Dead);
        if (IsMine && Inventory != null) Inventory.DropAllItems();
        OnDied?.Invoke();
    }

    public void Revive()
    {
        CurrentHp = so.GetMaxHp();
        isDie = false;
        Stats.RestoreAll();
        Stats.ClearShield();
        Input.ClearMoveCommand();

        transform.position = spawnPos != null ? spawnPos.position : startPos;

        Motor.SetAgentEnabled(true);
        SetState(PlayerStateType.Idle);
        SendHpSync();
        OnRevived?.Invoke();
    }

    #region 데이터

    // ═════════════════════ Save & Load ═══════════════════════

    // 데이터 저장 및 불어오기 위치값은 굳이 안넣어도 됨 이미 알아하는중
    // 데이터 저장
    // 상시 저장 알아한다.
    // 소유권? 인벤토리 데이터, 알아낸 레시피 기록 - 이외에도 필요한거 생각해보기

    // 현재 상태를 묶어서 저장 데이터에 담는다.
    private bool isLoadingState;

    // 방장이 아닌 소유자만 방장 사본에 상태를 보낸다. 방장/오프라인은 자기 오브젝트가 곧 저장 대상
    private bool ShouldSyncToMaster => IsMine && PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient;

    public override void SaveData(SaveData _data)
    {
        var s = new SaveData();
        base.SaveData(s); // Entity 내부 SaveData 불러오기 -> 상태이상 데이터와 플레이어의 데이터 구분
        var state = new PlayerWorldData
        {
            Owner = Owner,
            CurrentHp = CurrentHp,
            CurrentStamina = Stats.CurrentStamina,

            Item = Inventory.Slots,
            PotionSlotIds = Loadout.GetSlotIds(),
            SelectedPotionSlot = Loadout.SelectedIndex,
            DiscoveredPotionIds = Recipes.GetDiscoveredIds()
        };

        _data.SetData(state);
        _data.Child["b"] = s;
    }

    // 저장 데이터에서 상태를 꺼내 적용한다.
    public override void LoadData(SaveData _data)
    {

        if (_data?.Data == null) return; // 처음 생성됐거나 예전 세이브라 데이터가 없을 때
        isLoadingState = true;
        try
        {
            if (_data.Child.TryGetValue("b", out SaveData entityData))
                base.LoadData(entityData);
            var state = _data.GetData<PlayerWorldData>();
            SetOwner(state.Owner);
            CurrentHp = state.CurrentHp;
            isDie = CurrentHp <= 0;
            Stats.LoadStamina(state.CurrentStamina);
            Stats.NotifyHpChanged();
            if (state.Item != null)
                RestoreInventory(state.Item);

            if (state.PotionSlotIds != null)
                Loadout.Restore(state.PotionSlotIds, state.SelectedPotionSlot);

            if (state.DiscoveredPotionIds != null)   // 레시피 기록이 없는 예전 세이브면 null
                Recipes.Restore(state.DiscoveredPotionIds);
        }
        finally
        {
            isLoadingState = false;
        }
    }
    #endregion

    #region 네트워크

    // ═════════════════════ RPC ═══════════════════════

    private enum PlayerRPCType : byte
    {
        SyncHp,
        Throw,
        RequestDamage,
        RequestHeal,
        GrantItem,
        SyncLoadout,
        RequestShield,
        SyncShield,
        SyncInventory,
        SyncRecipes,
        RegisterStatusEffect = RpcRegisterStatusEffect, // Entity에서 정의한 하위 RPC enum과 겹치지 않는 공용 번호.
    }

    // 소유자가 확정한 HP를 다른 플레이어에게 전송
    private void SendHpSync()
    {
        if (!IsMine) return;

        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.SyncHp);
        packet.Write(CurrentHp);
        this.SendRPC(packet, WorldRPCTarget.Others);
    }

    private void SendShieldSync()
    {
        if (!IsMine) return;

        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.SyncShield);
        packet.Write(Stats.CurrentShield);
        this.SendRPC(packet, WorldRPCTarget.Others);
    }

    private void SendInventorySync()
    {
        if (!ShouldSyncToMaster || isLoadingState) return;

        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.SyncInventory);
        packet.Write(Inventory.Slots.Count);            // 받는 쪽이 읽을 개수
        foreach (InventorySlot slot in Inventory.Slots)  // 리스트 순서가 곧 슬롯 위치
        {
            packet.Write(slot.ItemId);
            packet.Write(slot.Amount);
        }
        this.SendRPC(packet, WorldRPCTarget.MasterClient);
    }

    // 발견한 레시피 목록 전체를 방장 사본에 보낸다
    private void SendRecipeSync()
    {
        if (!ShouldSyncToMaster || isLoadingState) return;

        List<int> potionIds = Recipes.GetDiscoveredIds();
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.SyncRecipes);
        packet.Write(potionIds.Count);                  // 받는 쪽이 읽을 개수
        foreach (int potionId in potionIds)
            packet.Write(potionId);
        this.SendRPC(packet, WorldRPCTarget.MasterClient);
    }

    // 장착이 바뀌면 전체 장착 상태를 다른 클라이언트에 보낸다
    private void HandleLoadoutEquipChanged(int slotIndex, PotionData potion) => SendLoadoutSync();

    private void SendLoadoutSync()
    {
        if (!ShouldSyncToMaster || isLoadingState) return;
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.SyncLoadout);
        for (int i = 0; i < PotionLoadout.SlotCount; i++)
        {
            PotionData potion = Loadout.GetSlot(i);
            packet.Write(potion != null ? potion.Id : -1);   // 빈 슬롯은 -1
        }
        packet.Write(Loadout.SelectedIndex);
        this.SendRPC(packet, WorldRPCTarget.MasterClient);
    }

    // 던지기 정보를 모든 클라이언트에 보낸다 (내 화면은 즉시 실행). 목표 지점은 분신이 같은 곳을 따라 던질 때 쓴다
    public void SendThrow(Vector3 origin, Vector3 velocity, Vector3 gravity, Vector3 target, int potionId)
    {
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.Throw);
        packet.Write(origin);
        packet.Write(velocity);
        packet.Write(gravity);
        packet.Write(target);
        packet.Write(potionId);
        this.SendRPC(packet, WorldRPCTarget.All);   // 확장 메서드라 this. 필요
    }

    // 소유자가 아닌 화면에서 받은 피해를 소유자에게만 요청한다
    public void SendDamageRequest(int damage, Vector3 sourcePosition)
    {
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.RequestDamage);
        packet.Write(damage);
        packet.Write(sourcePosition);
        this.SendRPC(packet, Owner);
    }

    // 소유자가 아닌 화면에서 받은 회복을 소유자에게만 요청한다
    public void SendHealRequest(int amount)
    {
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.RequestHeal);
        packet.Write(amount);
        this.SendRPC(packet, Owner);
    }

    public void SendShieldRequest(int amount, float duration)
    {
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.RequestShield);
        packet.Write(amount);
        packet.Write(duration);
        this.SendRPC(packet, Owner);
    }

    // 방장이 확정한 아이템을 이 플레이어의 소유자에게만 지급한다
    public void SendGrantItem(int itemId, int amount)
    {
        PacketData packet = new();
        packet.Write((byte)PlayerRPCType.GrantItem);
        packet.Write(itemId);
        packet.Write(amount);
        this.SendRPC(packet, Owner);
    }

    // RPCHelper가 ID로 찾아서 호출한다. 보낸 순서 그대로 읽는다
    public void ReceiveRPC(PacketData packet)
    {
        PlayerRPCType rpcType = (PlayerRPCType)packet.ReadByte();

        switch (rpcType)
        {
            case PlayerRPCType.Throw:
                Vector3 origin = packet.ReadVector3();
                Vector3 velocity = packet.ReadVector3();
                Vector3 gravity = packet.ReadVector3();
                Vector3 target = packet.ReadVector3();
                int potionId = packet.ReadInt();
                Thrower.SpawnProjectile(origin, velocity, gravity, target, potionId);
                break;
            case PlayerRPCType.SyncHp:
                ApplyHp(packet.ReadInt());
                break;
            case PlayerRPCType.RequestDamage:
                int damage = packet.ReadInt();
                Vector3 sourcePosition = packet.ReadVector3();
                if (IsMine) TakeDamage(damage, sourcePosition);
                break;
            case PlayerRPCType.RequestHeal:
                int heal = packet.ReadInt();
                if (IsMine) Heal(heal);
                break;
            case PlayerRPCType.GrantItem:
                int itemId = packet.ReadInt();
                int amount = packet.ReadInt();
                if (IsMine)
                {
                    Inventory.AddItem(itemId, amount);
                    OnItemGranted?.Invoke(itemId, amount);
                }
                break;
            case PlayerRPCType.SyncLoadout:
                int[] ids = new int[PotionLoadout.SlotCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = packet.ReadInt();
                int selected = packet.ReadInt();
                if (!IsMine) Loadout.Restore(ids, selected);
                break;
            case PlayerRPCType.RequestShield:
                int shield = packet.ReadInt();
                float duration = packet.ReadFloat();
                if (IsMine) GetShield(shield, duration);
                break;
            case PlayerRPCType.SyncShield:
                ApplyShield(packet.ReadInt());
                break;
            case PlayerRPCType.SyncInventory:
                int count = packet.ReadInt();
                var items = new List<InventorySlot>(count);
                for (int i = 0; i < count; i++)
                    items.Add(new InventorySlot(packet.ReadInt(), packet.ReadInt()));
                if (!IsMine) RestoreInventory(items);
                break;
            case PlayerRPCType.SyncRecipes:
                int recipeCount = packet.ReadInt();
                var recipeIds = new List<int>(recipeCount);
                for (int i = 0; i < recipeCount; i++)
                    recipeIds.Add(packet.ReadInt());
                if (!IsMine) Recipes.Restore(recipeIds);
                break;
            case PlayerRPCType.RegisterStatusEffect:
                ReceiveRegisterStatusEffect(packet);   // 소유자 여부와 상관없이 모두 적용
                break;
        }
    }

    private void RestoreInventory(List<InventorySlot> items)
    {
        Inventory.Slots.Clear();
        foreach (var item in items)
            Inventory.AddItem(item.ItemId, item.Amount);
    }

    // 소유자가 확정한 HP를 적용한다
    private void ApplyHp(int hp)
    {
        bool wasDead = isDie;
        CurrentHp = hp;
        isDie = CurrentHp <= 0;
        Stats.NotifyHpChanged();

        if (!wasDead && isDie)
            OnDied?.Invoke();
        else if (wasDead && !isDie)
            OnRevived?.Invoke();
    }

    private void ApplyShield(int shield) => Stats.ApplySyncedShield(shield);

    // 장착 변경 이벤트 구독을 해제한다
    private void OnDestroy()
    {
        Stats.OnShieldChanged -= HandleShieldChanged;
        Loadout.OnEquipChanged -= HandleLoadoutEquipChanged;
        Recipes.OnChanged -= SendRecipeSync;
    }

    #endregion
}
