using UnityEngine;

namespace Pomunculus.Items
{
    public class MaterialPickup : MonoBehaviour, ITMPWorldObj
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
    }
}