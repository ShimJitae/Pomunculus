using System.Collections.Generic;
using UnityEngine;

namespace Pomunculus.Items
{
    public class PotionCraftHistory : MonoBehaviour
    {
        // 포션 ID별로 제작에 성공한 재료 조합 보관
        private readonly Dictionary<int, List<List<int>>> records = new Dictionary<int, List<List<int>>>();

        public void Record(int potionId, List<MaterialData> materials)
        {
            if (materials == null || materials.Count == 0)
                return;

            List<int> materialIds = new List<int>();

            foreach (MaterialData material in materials)
            {
                if (material == null)
                    return;

                materialIds.Add(material.Id);
            }

            // 순서가 다른 조합도 동일하게 비교
            materialIds.Sort();

            if (!records.TryGetValue(potionId, out var combinations))
            {
                combinations = new List<List<int>>();
                records.Add(potionId, combinations);
            }

            foreach (List<int> combination in combinations)
            {
                if (IsSameCombination(combination, materialIds))
                    return;
            }

            combinations.Add(materialIds);
        }

        private bool IsSameCombination(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
        // 해당 포션으로 기록된 조합 개수
        public int GetCombinationCount(int potionId)
        {
            if (!records.TryGetValue(potionId, out var combinations))
                return 0;

            return combinations.Count;
        }

        // 특정 조합에 들어간 재료 ID 목록
        public IReadOnlyList<int> GetCombination(int potionId, int index)
        {
            if (!records.TryGetValue(potionId, out var combinations))
                return null;

            if (index < 0 || index >= combinations.Count)
                return null;

            return combinations[index].AsReadOnly();
        }
    }
}