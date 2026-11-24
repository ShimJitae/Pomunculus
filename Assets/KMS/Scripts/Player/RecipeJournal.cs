using System;
using System.Collections.Generic;

namespace Pomunculus.Items
{
    // 플레이어가 포션 제작으로 발견한 레시피(포션 ID) 목록. PlayerFSM이 소유하고 저장·동기화한다
    public class RecipeJournal
    {
        private const int EmptyBottleId = 10001;

        private readonly List<int> discoveredPotionIds = new List<int>();

        // 발견 목록이 바뀌었을 때 (새 발견, 저장·동기화 데이터로 복원)
        public event Action OnChanged;

        // 새 레시피를 발견 목록에 추가한다. 빈 병이거나 이미 발견했으면 false
        public bool Discover(int potionId)
        {
            if (potionId == EmptyBottleId || discoveredPotionIds.Contains(potionId))
                return false;

            discoveredPotionIds.Add(potionId);
            OnChanged?.Invoke();
            return true;
        }

        // 이미 발견한 레시피인지 확인한다
        public bool IsDiscovered(int potionId)
        {
            return discoveredPotionIds.Contains(potionId);
        }

        // 저장·동기화용으로 발견 목록의 복사본을 가져온다
        public List<int> GetDiscoveredIds()
        {
            return new List<int>(discoveredPotionIds);
        }

        // 저장·동기화 데이터로 발견 목록을 덮어쓴다
        public void Restore(IEnumerable<int> potionIds)
        {
            discoveredPotionIds.Clear();

            if (potionIds != null)
            {
                foreach (int potionId in potionIds)
                {
                    if (potionId == EmptyBottleId || discoveredPotionIds.Contains(potionId))
                        continue;

                    discoveredPotionIds.Add(potionId);
                }
            }

            OnChanged?.Invoke();
        }
    }
}
