using UnityEngine;

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

        public int Amount => amount;
        public int ID { get; set; } = -1;
        public string PrefabId { get; set; }

        // 이 픽업이 지급할 아이템 ID. 데이터가 비어 있으면 0 이하를 반환한다
        public abstract int ItemId { get; }

        private enum PickupRPCType : byte
        {
            Pickup
        }

        private bool isPickedUp;   // 방장 화면에서 이미 누군가에게 지급했는지 (중복 지급 방지)

        // WorldManager가 생성 직후 네트워크 ID를 넣어준다
        public void InitWorldObj(int id, string prefabId)
        {
            ID = id;
            PrefabId = prefabId;
        }

        // 전파·스냅샷에 드롭 수량을 담는다
        public void SaveData(SaveData data)
        {
            data.SetData(amount);
        }

        // 받은 데이터로 드롭 수량을 복원한다 (데이터가 없으면 프리팹 기본값 유지)
        public void LoadData(SaveData data)
        {
            if (data?.Data == null)
                return;

            amount = Mathf.Max(1, data.GetData<int>());
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