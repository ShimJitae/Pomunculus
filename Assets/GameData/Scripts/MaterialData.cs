using UnityEngine;

namespace Pomunculus.Items
{
    [CreateAssetMenu(
        fileName = "Material",
        menuName = "Pomunculus/Items/Material")]
    public class MaterialData : ScriptableObject
    {
        [Header("기본 정보")]
        public int Id;
        public string ItemName;

        [TextArea(2, 5)]
        public string Description;

        [Header("속성")]
        public float PhysicsElement;
        public float FireElement;
        public float ColdElement;
        public float WindElement;

        [Header("유니티에서 직접 연결")]
        public Sprite Icon;
        public GameObject WorldPrefab;
    }
}