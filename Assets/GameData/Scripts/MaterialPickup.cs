using UnityEngine;

namespace Pomunculus.Items
{
    public class MaterialPickup : MonoBehaviour
    {
        [Header("재료 정보")]
        [SerializeField] private MaterialData material;

        [Header("드롭 개수")]
        [SerializeField, Min(1)] private int amount = 1;

        public MaterialData Material => material;
        public int Amount => amount;
    }
}