using UnityEngine;

namespace Pomunculus.Items
{
    public class PotionPickup : MonoBehaviour
    {
        [Header("포션 정보")]
        [SerializeField] private PotionData potionData;
        public PotionData Data => potionData;
    }
}