using UnityEngine;

namespace Pomunculus.Items
{
    public class PotionPickup : WorldItemPickup
    {
        [Header("포션 정보")]
        [SerializeField] private PotionData potionData;

        public PotionData Data => potionData;

        // 포션 데이터의 ID를 지급한다
        public override int ItemId => potionData != null ? potionData.Id : 0;
    }
}