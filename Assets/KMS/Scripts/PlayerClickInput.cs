using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ClickMoveExample
{
    /// <summary>
    /// 마우스·키보드 입력을 읽어 버퍼에 기록한다. 실제 행동은 상태들이 Consume* 로 꺼내 쓸 때 일어난다.
    /// 자체 Update 없음 — PlayerClickFSM이 매 프레임 ReadInput()을 호출한다.
    /// </summary>
    public class PlayerClickInput : MonoBehaviour
    {
        public float holdRepathInterval = 0.1f;   // 우클릭 홀드 시 목적지 갱신 간격
        public float navSampleRadius = 2f;        // 클릭 지점을 NavMesh 위로 보정하는 범위
        [Tooltip("마우스 레이캐스트 대상. 바닥 레이어만 지정하는 것을 권장")]
        public LayerMask groundLayer = ~0;
        [Tooltip("선택: 클릭 위치에 표시할 오브젝트 (파티클 등, 씬에 하나 두고 재사용)")]
        public GameObject moveMarker;
        public float inputBufferTime = 0.15f;

        public bool HasPendingMove { get; private set; }     // 아직 처리되지 않은 우클릭 이동 명령

        Camera cam;
        Vector3 pendingDestination;
        Vector3 bufferedAttackTarget;
        float attackPressedAt = float.NegativeInfinity;
        float dodgePressedAt = float.NegativeInfinity;
        float nextHoldRepathAt = float.NegativeInfinity;

        void Awake() => cam = Camera.main;

        public void ReadInput()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null) return;   // 마우스가 없는 환경 (모바일 등)

            // 우클릭: 이동 명령. 누르고 있으면 일정 간격으로 목적지 갱신
            bool rightDown = mouse.rightButton.wasPressedThisFrame;
            bool rightHeld = mouse.rightButton.isPressed;
            if (rightDown || (rightHeld && Time.time >= nextHoldRepathAt))
            {
                nextHoldRepathAt = Time.time + holdRepathInterval;
                if (TryGetNavPointUnderMouse(out Vector3 dest))
                {
                    pendingDestination = dest;
                    HasPendingMove = true;
                    if (rightDown) ShowMoveMarker(dest);
                }
            }

            // 좌클릭: 던지기. 클릭한 순간의 마우스 위치를 목표로 저장
            if (mouse.leftButton.wasPressedThisFrame && TryGetMouseWorldPoint(out Vector3 target))
            {
                attackPressedAt = Time.time;
                bufferedAttackTarget = target;
            }

            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame) dodgePressedAt = Time.time;
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

        /// <summary>버퍼에 남은 좌클릭을 꺼낸다. target은 사거리 보정 전 원래 클릭 위치</summary>
        public bool ConsumeAttack(out Vector3 target)
        {
            target = bufferedAttackTarget;
            if (Time.time - attackPressedAt > inputBufferTime) return false;
            attackPressedAt = float.NegativeInfinity;
            return true;
        }

        /// <summary>버퍼에 남은 Space를 꺼낸다. 쿨다운 확인은 호출하는 쪽에서 먼저 한다</summary>
        public bool ConsumeDodge()
        {
            if (Time.time - dodgePressedAt > inputBufferTime) return false;
            dodgePressedAt = float.NegativeInfinity;
            return true;
        }

        /// <summary>마우스 아래 지면 위치 (지형 높이 반영). 실패 시 플레이어 높이 평면으로 대체</summary>
        public bool TryGetMouseWorldPoint(out Vector3 point)
        {
            point = default;
            Mouse mouse = Mouse.current;
            if (cam == null || mouse == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            var plane = new Plane(Vector3.up, transform.position);
            if (!plane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        /// <summary>마우스 아래 지점을 갈 수 있는 NavMesh 위치로 보정</summary>
        bool TryGetNavPointUnderMouse(out Vector3 point)
        {
            point = default;
            if (!TryGetMouseWorldPoint(out Vector3 ground)) return false;
            if (!NavMesh.SamplePosition(ground, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                return false;

            point = navHit.position;
            return true;
        }

        void ShowMoveMarker(Vector3 position)
        {
            if (moveMarker == null) return;
            moveMarker.transform.position = position;
            moveMarker.SetActive(false);   // 껐다 켜서 파티클 등을 다시 재생
            moveMarker.SetActive(true);
        }
    }
}
