using UnityEngine;

namespace Pomunculus.Items
{
    public class MaterialPickup : MonoBehaviour, ITMPWorldObj, IWorldRPC
    {
        [Header("재료 정보")]
        [SerializeField] private MaterialData material;
        [Header("드롭 개수")]
        [SerializeField, Min(1)] private int amount = 1;
        public MaterialData Material => material;
        public int Amount => amount;
        public int ID { get; set; } = -1;
        public string PrefabId { get; set; }
        public void InitWorldObj(int id, string prefabId)
        {
            ID = id;
            PrefabId = prefabId;
        }
        public void SaveData(SaveData data)
        {
        }
        public void LoadData(SaveData data)
        {
        }

        private enum PickupRPCType : byte
        {
            Pickup
        }

        private bool isPickedUp;   // 방장 화면에서 이미 누군가에게 지급했는지 (중복 지급 방지)

        // 방장에게 이 재료를 줍겠다고 요청한다 (방장·오프라인이면 바로 처리됨)
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
            if (isPickedUp || material == null) return;

            PlayerFSM picker = FindPlayer(pickerActor);
            if (picker == null) return;   // 요청 직후 나간 경우

            isPickedUp = true;
            picker.SendGrantItem(material.Id, amount);
            this.Despawn();   // IWOHelper.Despawn → WorldManager.Despawn
        }

        // ActorNumber로 플레이어를 찾는다
        private static PlayerFSM FindPlayer(int actor)
        {
            foreach (var obj in WorldManager.Instance.Objects.GetList())
                if (obj is PlayerFSM player && player.Owner == actor) return player;
            return null;
        }
    }
}