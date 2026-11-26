using UnityEngine;
using UnityEngine.AI;

namespace Pomunculus.Items
{
    /// <summary>
    /// 바닥에 놓인 줍기 아이템의 공통 기능.
    /// 수량 동기화와 방장 확정 줍기를 담당하고, 하위 클래스는 어떤 아이템인지(ItemId)만 알려준다.
    /// </summary>
    public abstract class WorldItemPickup : MonoBehaviour, ITMPWorldObj, IWorldRPC
    {
        [Header("드롭 개수")]
        [SerializeField, Min(1)] private int amount = 1;

        [Header("낙하 복구")]
        [Tooltip("마지막으로 놓인 위치보다 이만큼 아래로 떨어지면 되돌린다")]
        [SerializeField, Min(1f)] private float fallResetDistance = 5f;

        private Vector3 restPosition;   // 마지막으로 놓인 위치 (생성 위치, 이동 연출이 끝난 위치)

        public int Amount => amount;

        // 드롭 수량을 바꾼다 (최소 1). 방장이 생성 직후, 전파 전에 호출해야 다른 화면에도 같은 수량이 간다
        public void SetAmount(int value) => amount = Mathf.Max(1, value);
        public int ID { get; set; } = -1;
        public string PrefabId { get; set; }

        // 이 픽업이 지급할 아이템 ID. 데이터가 비어 있으면 0 이하를 반환한다
        public abstract int ItemId { get; }

        private enum PickupRPCType : byte
        {
            Pickup,
            Displace
        }

        // 플레이어가 버린 아이템의 SaveData.Child 키
        private const string DropFlightKey = "dropFlight";       // [도착 x, y, z, 비행 시간, 포물선 높이]
        private const string DropperKey = "dropper";             // 버린 플레이어 ActorNumber
        private const string DropperDelayKey = "dropperDelay";   // 버린 사람이 다시 주울 수 있을 때까지의 시간(초)

        private const int NoDropper = int.MinValue;

        private bool isPickedUp;   // 방장 화면에서 이미 누군가에게 지급했는지 (중복 지급 방지)
        private Rigidbody body;
        private Vector3 displaceStart;
        private Vector3 displaceEnd;
        private float displaceDuration;
        private float displaceElapsed;
        private float displaceArcHeight;
        private bool isDisplacing;
        private bool isDropFlight;   // 플레이어가 버려서 튀어나가는 중인지 (넉백 이동과 구분, 이때만 줍기 불가)

        private int dropperActor = NoDropper;   // 이 아이템을 버린 플레이어 (없으면 NoDropper)
        private float dropperLockedUntil;       // 버린 플레이어가 이 시각 전까지는 줍지 못한다 (로컬 Time.time 기준)

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            restPosition = transform.position;   // Instantiate 시점의 생성 위치
        }

        /// <summary>
        /// 플레이어가 버린 아이템의 생성 데이터를 만든다.
        /// 수량은 기존과 같이 Data에, 튀어나가는 궤적과 버린 사람 정보는 Child에 담겨 생성과 함께 모든 화면에 전파된다.
        /// </summary>
        public static SaveData CreateDropData(int amount, Vector3 landingPoint, float duration, float arcHeight,
            int dropperActor, float dropperDelay)
        {
            SaveData data = global::SaveData.CreateSaveData(Mathf.Max(1, amount));
            data.Child[DropFlightKey] = global::SaveData.CreateSaveData(
                new[] { landingPoint.x, landingPoint.y, landingPoint.z, duration, arcHeight });
            data.Child[DropperKey] = global::SaveData.CreateSaveData(dropperActor);
            data.Child[DropperDelayKey] = global::SaveData.CreateSaveData(dropperDelay);
            return data;
        }

        // 지금 이 플레이어가 주울 수 있는지. 날아가는 중이거나, 버린 본인이 대기 시간 안이면 false
        public bool CanBePickedUpBy(int actor)
        {
            if (isDropFlight)
                return false;

            return actor != dropperActor || Time.time >= dropperLockedUntil;
        }

        // WorldManager가 생성 직후 네트워크 ID를 넣어준다
        public void InitWorldObj(int id, string prefabId)
        {
            ID = id;
            PrefabId = prefabId;
        }

        // 전파·스냅샷에 드롭 수량을 담는다.
        // 날아가는 중이거나 버린 사람 잠금이 남아 있으면 그 정보도 담는다 (방장이 생성 직후 다른 화면에 전파할 때 이 값이 쓰임).
        // 착지가 끝난 뒤에는 수량만 저장하므로, 늦게 들어온 사람에게는 착지한 상태로 보인다
        public void SaveData(SaveData data)
        {
            data.SetData(amount);

            if (isDisplacing)
            {
                float remainingDuration = Mathf.Max(0.01f, displaceDuration - displaceElapsed);
                float arcHeight = displaceElapsed <= 0f ? displaceArcHeight : 0f;   // 도중 합류면 남은 구간은 직선으로
                data.Child[DropFlightKey] = global::SaveData.CreateSaveData(
                    new[] { displaceEnd.x, displaceEnd.y, displaceEnd.z, remainingDuration, arcHeight });
            }

            float remainingLock = dropperLockedUntil - Time.time;
            if (dropperActor != NoDropper && remainingLock > 0f)
            {
                data.Child[DropperKey] = global::SaveData.CreateSaveData(dropperActor);
                data.Child[DropperDelayKey] = global::SaveData.CreateSaveData(remainingLock);
            }
        }

        // 받은 데이터로 드롭 수량을 복원한다 (데이터가 없으면 프리팹 기본값 유지).
        // 플레이어가 버린 아이템이면 현재 위치(시작점)에서 착지점까지 튀어나가는 연출을 시작한다
        public void LoadData(SaveData data)
        {
            if (data?.Data == null)
                return;

            amount = Mathf.Max(1, data.GetData<int>());
            LoadDropData(data);
        }

        // 버린 사람 정보와 튀어나가는 궤적을 읽어 적용한다. Child가 없으면(일반 드롭) 아무것도 하지 않는다
        private void LoadDropData(SaveData data)
        {
            if (data.Child == null)
                return;

            if (data.Child.TryGetValue(DropperKey, out SaveData dropper) && dropper?.Data != null)
            {
                dropperActor = dropper.GetData<int>();
                float delay = 0f;
                if (data.Child.TryGetValue(DropperDelayKey, out SaveData delayData) && delayData?.Data != null)
                    delay = delayData.GetData<float>();
                dropperLockedUntil = Time.time + delay;
            }

            if (data.Child.TryGetValue(DropFlightKey, out SaveData flight) && flight?.Data != null)
            {
                float[] values = flight.GetData<float[]>();
                if (values != null && values.Length >= 5)
                {
                    Vector3 landingPoint = new Vector3(values[0], values[1], values[2]);
                    StartDisplace(transform.position, landingPoint, values[3], values[4]);
                    isDropFlight = true;
                }
            }
        }

        // 방장에게 이 아이템을 줍겠다고 요청한다 (방장·오프라인이면 바로 처리됨)
        public void RequestPickup(int pickerActor)
        {
            PacketData packet = new();
            packet.Write((byte)PickupRPCType.Pickup);
            packet.Write(pickerActor);
            this.SendRPC(packet, WorldRPCTarget.MasterClient);
        }

        // RPCHelper가 ID로 찾아서 호출한다. 보낸 순서 그대로 읽는다
        public void ReceiveRPC(PacketData packet)
        {
            PickupRPCType rpcType = (PickupRPCType)packet.ReadByte();

            switch (rpcType)
            {
                case PickupRPCType.Pickup:
                    HandlePickupRequest(packet.ReadInt());
                    break;
                case PickupRPCType.Displace:
                    StartDisplace(packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat(), 0f);
                    break;
            }
        }

        // 방장 전용. 먼저 온 요청 하나만 지급하고 필드에서 제거한다
        private void HandlePickupRequest(int pickerActor)
        {
            if (isPickedUp || ItemId <= 0)
                return;

            PlayerFSM picker = FindPlayer(pickerActor);
            if (picker == null)
                return;   // 요청 직후 나간 경우

            isPickedUp = true;
            picker.SendGrantItem(ItemId, amount);
            this.Despawn();
        }

        // 방장 전용. 시작·도착 지점을 모든 클라이언트에 보내 같은 경로로 옮기게 한다
        public void SendDisplace(Vector3 destination, float duration)
        {
            PacketData packet = new();
            packet.Write((byte)PickupRPCType.Displace);
            packet.Write(transform.position);   // 시작 위치도 방장 기준으로 맞춘다
            packet.Write(destination);
            packet.Write(duration);
            this.SendRPC(packet, WorldRPCTarget.All);   // 방장 화면은 즉시 실행
        }

        // 받은 시작점에서 도착점까지 일정 시간 동안 옮긴다. arcHeight가 0보다 크면 포물선을 그린다. 이동 중에는 물리를 끈다
        private void StartDisplace(Vector3 start, Vector3 end, float duration, float arcHeight)
        {
            displaceStart = start;
            displaceEnd = end;
            displaceDuration = Mathf.Max(0.01f, duration);
            displaceElapsed = 0f;
            displaceArcHeight = Mathf.Max(0f, arcHeight);
            isDisplacing = true;
            isDropFlight = false;   // 버리기 연출이면 호출한 쪽(LoadDropData)에서 다시 켠다

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            transform.position = start;
        }

        void Update()
        {
            if (!isDisplacing) { CheckFallReset(); return; }

            displaceElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(displaceElapsed / displaceDuration);
            float arcOffset = displaceArcHeight * 4f * t * (1f - t);   // t=0.5에서 최고점, 양 끝에서 0
            transform.position = Vector3.Lerp(displaceStart, displaceEnd, t) + Vector3.up * arcOffset;
            if (t < 1f) return;

            isDisplacing = false;
            isDropFlight = false;
            transform.position = displaceEnd;               // 모든 화면에서 같은 자리로 확정
            restPosition = displaceEnd;                     // 넉백·버리기 도착점을 새 기준으로
            if (body != null) body.isKinematic = false;
        }

        // 기준 위치보다 너무 아래로 떨어지면(바닥을 뚫음) 기준 위치의 땅 위로 되돌린다
        private void CheckFallReset()
        {
            if (body == null || body.isKinematic) return;
            if (transform.position.y > restPosition.y - fallResetDistance) return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            // 생성 위치가 장식품 때문에 최대 5m 올라갈 수 있으므로 그보다 넉넉하게 아래를 본다
            if (Physics.Raycast(restPosition + Vector3.up, Vector3.down, out RaycastHit hit, 15f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point + Vector3.up * 0.3f;   // 받쳐 줄 바닥이 있으면 그 위로
                return;
            }

            // 맵에 구멍이 있어 받쳐 줄 바닥이 없는 예외 상황: 계속 떨어졌다 올라오기를 반복하지 않도록 길 지도 위에 고정
            if (NavMesh.SamplePosition(restPosition, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                transform.position = navHit.position;
            body.isKinematic = true;
        }

        // ActorNumber로 플레이어를 찾는다
        private static PlayerFSM FindPlayer(int actor)
        {
            foreach (IWorldObj obj in WorldManager.Instance.Objects.GetList())
            {
                if (obj is PlayerFSM player && player.Owner == actor)
                    return player;
            }
            return null;
        }
    }
}