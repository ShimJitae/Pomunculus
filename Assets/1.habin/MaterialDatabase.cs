using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    [CreateAssetMenu(
        fileName = "MaterialDatabase",
        menuName = "Pomunculus/Items/MaterialDatabase")]
    public class MaterialDatabase : ScriptableObject
    {
        [SerializeField]
        private List<MaterialData> materials = new List<MaterialData>();

        public IReadOnlyList<MaterialData> Materials => materials;

        private Dictionary<int, MaterialData> materialById;
        private static MaterialDatabase instance;

        // Resources에서 DB 불러오기
        public static MaterialDatabase Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<MaterialDatabase>("MaterialDatabase");

                return instance;
            }
        }

        // 목록이 변경되면 ID 검색용 사전 초기화
        private void OnEnable() => materialById = null;
        private void OnValidate() => materialById = null;

        // ID로 재료 찾기
        public MaterialData Find(int materialId)
        {
            if (materialById == null)
                BuildLookup();

            return materialById.TryGetValue(materialId, out MaterialData material)
                ? material
                : null;
        }

        private void BuildLookup()
        {
            materialById = new Dictionary<int, MaterialData>();

            foreach (MaterialData material in materials)
            {
                if (material != null)
                    materialById.TryAdd(material.Id, material);
            }
        }
    }
}