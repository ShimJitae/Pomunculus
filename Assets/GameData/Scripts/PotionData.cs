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
    }
}