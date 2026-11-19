using UnityEngine;

namespace Pomunculus.Items
{
    public class MaterialPickup : WorldItemPickup
    {
        [Header("재료 정보")]
        [SerializeField] private MaterialData material;

        public MaterialData Material => material;

        // 재료 데이터의 ID를 지급한다
        public override int ItemId => material != null ? material.Id : 0;
    }
}