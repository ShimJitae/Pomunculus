using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    [CreateAssetMenu(fileName = "Potion", menuName = "Pomunculus/Items/Potion")]
    public class PotionData : ScriptableObject
    {
        [Header("기본 정보")]
        public int Id;
        public string ItemName;

        [TextArea(1, 5)]
        public string Description;

        [Header("아이콘")]
        public Sprite Icon;

        [Header("원소 수치")]
        public float PhysicsElement;
        public float FireElement;
        public float ColdElement;
        public float WindElement;

        [Header("던지기")]
        [Tooltip("던질 때 생성할 프리팹. 비우면 빈 병")]
        [SerializeField] private ThrownProjectile projectilePrefab;

        [Tooltip("착탄 시 대상에게 순서대로 적용할 효과")]
        [SerializeReference] private List<PotionEffect> effects = new List<PotionEffect>();

        public ThrownProjectile ProjectilePrefab => projectilePrefab;
        public IReadOnlyList<PotionEffect> Effects => effects;
    }
}