using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DropItemManager : SingletonBehavior<DropItemManager>
{
    public EnemyDatabase monsterDatabase;

    [ContextMenu("DB확인")]
    public void ShowDB()
    {
        monsterDatabase.ShowDB();
    }

    public int TryGetDropItemID(int monsterId)
    {
        if (monsterDatabase.TryGetDropItemID(monsterId) != -1)
        {
            return monsterDatabase.TryGetDataSO(monsterId).GetDropId();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return -1;
    }

    public void SpawnDropItem(int monsterId)
    {
        if(monsterId == -1)
        {
            Debug.Log("dropId 가 잘못되었습니다.");
            return;
        }

        Debug.Log($"아이템 ID : {monsterDatabase.TryGetDropItemID(monsterId)} / 아이템 이름 : {monsterDatabase.TryGetDropItemName(monsterId)} >> 아이템 소환 /소환 로직 구현 추가되면 넣을 예정");
    }
}
