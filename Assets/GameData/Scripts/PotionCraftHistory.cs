using System;
using System.Collections.Generic;

namespace Pomunculus.Items
{
    public class PotionCraftHistory
    {
        // 포션 ID별로 제작에 성공한 재료 조합 보관
        private readonly Dictionary<int, List<List<int>>> records = new Dictionary<int, List<List<int>>>();

        public event Action OnChanged;

        // 저장·동기화용으로 전체 기록의 깊은 복사본을 가져온다
        public Dictionary<int, List<List<int>>> GetRecords()
        {
            var copiedRecords = new Dictionary<int, List<List<int>>>();

            foreach (var pair in records)
            {
                var copiedCombinations = new List<List<int>>(pair.Value.Count);

                foreach (List<int> combination in pair.Value)
                    copiedCombinations.Add(new List<int>(combination));   // 조합 리스트도 새로 만든다

                copiedRecords.Add(pair.Key, copiedCombinations);
            }

            return copiedRecords;
        }

        // 저장·동기화 데이터로 전체 기록을 덮어쓴다
        public void Restore(Dictionary<int, List<List<int>>> savedRecords)
        {
            records.Clear();

            if (savedRecords != null)
            {
                foreach (var pair in savedRecords)
                {
                    if (pair.Value == null)
                        continue;

                    var combinations = new List<List<int>>();

                    foreach (List<int> savedCombination in pair.Value)
                    {
                        if (savedCombination == null || savedCombination.Count == 0)
                            continue;

                        var materialIds = new List<int>(savedCombination);   // 복사
                        materialIds.Sort();                                  // Record와 같은 기준으로 정렬

                        bool isDuplicate = false;
                        foreach (List<int> combination in combinations)
                        {
                            if (IsSameCombination(combination, materialIds))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (!isDuplicate)
                            combinations.Add(materialIds);
                    }

                    if (combinations.Count > 0)
                        records.Add(pair.Key, combinations);
                }
            }

            OnChanged?.Invoke();
        }

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
            OnChanged?.Invoke();
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