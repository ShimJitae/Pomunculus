using Pomunculus.Items;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 입력을 읽는 유일한 곳. 키 정의는 PlayerControls.inputactions 에셋에 있다.
/// 입력을 버퍼에 기록만 하고, 실제 행동은 상태들이 Consume* 로 꺼내 쓸 때 일어난다.
/// 자체 Update 없음 — PlayerFSM이 매 프레임 ReadInput()을 호출한다.
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    [Tooltip("PlayerControls.inputactions")]
    [SerializeField] private InputActionAsset controls;

    public float HoldRepathInterval = 0.1f;   // 우클릭 홀드 시 목적지 갱신 간격
    public float NavSampleRadius = 2f;        // 클릭 지점을 NavMesh 위로 보정하는 범위
    public LayerMask GroundLayer;
    public LayerMask UILayer;
    [Tooltip("클릭 위치에 표시할 오브젝트")]
    public GameObject MoveMarker;
    public float InputBufferTime = 0.15f;

    public bool HasPendingMove { get; private set; }     // 아직 처리되지 않은 우클릭 이동 명령

    private Camera cam;
    private Vector3 pendingDestination;
    private Vector3 bufferedAttackTarget;
    private float attackPressedAt = float.NegativeInfinity;
    private float dodgePressedAt = float.NegativeInfinity;
    private float interactPressedAt = float.NegativeInfinity;
    private float nextHoldRepathAt = float.NegativeInfinity;
    private int pendingPotionSlot = -1;                   // 처리 대기 중인 포션 슬롯 선택 (-1이면 없음)

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dodgeAction;
    private InputAction interactAction;
    private InputAction pointAction;
    private InputAction[] potionSlotActions;              // 1/2/3 키. 인덱스 = 슬롯 번호
#if UNITY_EDITOR
    private InputActionMap debugMap;
    private InputAction debugDamageAction;
    private InputAction debugReviveAction;
#endif

    // 카메라와 액션을 캐시한다. 액션 이름이 에셋과 다르면 여기서 바로 에러가 난다
    void Awake()
    {
        cam = Camera.main;

        playerMap = controls.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        attackAction = playerMap.FindAction("Attack", throwIfNotFound: true);
        dodgeAction = playerMap.FindAction("Dodge", throwIfNotFound: true);
        interactAction = playerMap.FindAction("Interact", throwIfNotFound: true);
        pointAction = playerMap.FindAction("Point", throwIfNotFound: true);
        potionSlotActions = new InputAction[PotionLoadout.SlotCount];
        for (int i = 0; i < PotionLoadout.SlotCount; i++)
        {
            potionSlotActions[i] = playerMap.FindAction("PotionSlot" + (i + 1), throwIfNotFound: true);
        }
#if UNITY_EDITOR
        debugMap = controls.FindActionMap("Debug", throwIfNotFound: true);
        debugDamageAction = debugMap.FindAction("DebugDamage", throwIfNotFound: true);
        debugReviveAction = debugMap.FindAction("DebugRevive", throwIfNotFound: true);
#endif
    }

    // 이 컴포넌트가 켜져 있는 동안만 입력을 받는다 (원격 플레이어는 컴포넌트를 끄면 됨)
    void OnEnable()
    {
        playerMap.Enable();
#if UNITY_EDITOR
        debugMap.Enable();
#endif
    }

    void OnDisable()
    {
        playerMap.Disable();
#if UNITY_EDITOR
        debugMap.Disable();
#endif
    }

    // 마우스 커서가 UI 위에 있는지 확인한다
    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 이번 프레임의 입력을 읽어 버퍼에 기록한다
    public void ReadInput()
    {
        bool pointerOverUI = IsPointerOverUI();

        // 우클릭: 이동 명령. 누르고 있으면 일정 간격으로 목적지 갱신
        bool rightDown = moveAction.WasPressedThisFrame();
        bool rightHeld = moveAction.IsPressed();
        if (rightDown || (rightHeld && Time.time >= nextHoldRepathAt))
        {
            nextHoldRepathAt = Time.time + HoldRepathInterval;
            if (TryGetNavPointUnderMouse(out Vector3 dest))
            {
                pendingDestination = dest;
                HasPendingMove = true;
                if (rightDown) ShowMoveMarker(dest);
            }
        }

        // 좌클릭: 던지기. 클릭한 순간의 마우스 위치를 목표로 저장
        if (!pointerOverUI && attackAction.WasPressedThisFrame() && TryGetMouseWorldPoint(out Vector3 target))
        {
            attackPressedAt = Time.time;
            bufferedAttackTarget = target;
        }

        if (dodgeAction.WasPressedThisFrame()) dodgePressedAt = Time.time;
        if (interactAction.WasPressedThisFrame()) interactPressedAt = Time.time;

        // 1/2/3: 포션 슬롯 선택. 같은 프레임에 여러 개를 누르면 뒤 번호가 우선
        for (int i = 0; i < potionSlotActions.Length; i++)
        {
            if (potionSlotActions[i].WasPressedThisFrame()) pendingPotionSlot = i;
        }
    }

    /// <summary>
    /// 처리 대기 중인 이동 명령을 꺼낸다.
    /// 공격·피격 중에 누른 우클릭은 사라지지 않고 남아 있다가, 상태가 끝나면 실행된다.
    /// </summary>
    public bool ConsumeMoveCommand(out Vector3 destination)
    {
        destination = pendingDestination;
        if (!HasPendingMove) return false;
        HasPendingMove = false;
        return true;
    }

    public void ClearMoveCommand() => HasPendingMove = false;

    // 버퍼에 남은 좌클릭을 꺼낸다. target은 사거리 보정 전 원래 클릭 위치
    public bool ConsumeAttack(out Vector3 target)
    {
        target = bufferedAttackTarget;
        if (Time.time - attackPressedAt > InputBufferTime) return false;
        attackPressedAt = float.NegativeInfinity;
        return true;
    }

    // 버퍼에 남은 Space를 꺼낸다. 쿨다운 확인은 호출하는 쪽에서 먼저 한다
    public bool ConsumeDodge()
    {
        if (Time.time - dodgePressedAt > InputBufferTime) return false;
        dodgePressedAt = float.NegativeInfinity;
        return true;
    }

    // 버퍼에 남은 상호작용 입력을 꺼낸다
    public bool ConsumeInteract()
    {
        if (Time.time - interactPressedAt > InputBufferTime) return false;
        interactPressedAt = float.NegativeInfinity;
        return true;
    }

    // 처리 대기 중인 포션 슬롯 선택을 꺼낸다. slotIndex는 0부터 시작 (1키 = 0)
    public bool ConsumePotionSlotSelect(out int slotIndex)
    {
        slotIndex = pendingPotionSlot;
        if (pendingPotionSlot < 0) return false;
        pendingPotionSlot = -1;
        return true;
    }

#if UNITY_EDITOR
    // 디버그 피격 키(H)를 이번 프레임에 눌렀는지
    public bool IsDebugDamagePressed() => debugDamageAction.WasPressedThisFrame();

    // 디버그 부활 키(R)를 이번 프레임에 눌렀는지
    public bool IsDebugRevivePressed() => debugReviveAction.WasPressedThisFrame();
#endif

    // 마우스 아래 지면 위치 (지형 높이 반영). 실패 시 플레이어 높이 평면으로 대체
    public bool TryGetMouseWorldPoint(out Vector3 point)
    {
        point = default;
        if (cam == null || pointAction.controls.Count == 0) return false;   // 포인터 장치가 없음

        Ray ray = cam.ScreenPointToRay(pointAction.ReadValue<Vector2>());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, GroundLayer, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        var plane = new Plane(Vector3.up, transform.position);
        if (!plane.Raycast(ray, out float enter)) return false;
        point = ray.GetPoint(enter);
        return true;
    }

    // 마우스 아래 지점을 갈 수 있는 NavMesh 위치로 보정
    bool TryGetNavPointUnderMouse(out Vector3 point)
    {
        point = default;
        if (!TryGetMouseWorldPoint(out Vector3 ground)) return false;
        if (!NavMesh.SamplePosition(ground, out NavMeshHit navHit, NavSampleRadius, NavMesh.AllAreas))
            return false;

        point = navHit.position;
        return true;
    }

    void ShowMoveMarker(Vector3 position)
    {
        if (MoveMarker == null) return;
        MoveMarker.transform.position = position;
        MoveMarker.SetActive(false);   // 껐다 켜서 파티클 등을 다시 재생
        MoveMarker.SetActive(true);
    }
}
