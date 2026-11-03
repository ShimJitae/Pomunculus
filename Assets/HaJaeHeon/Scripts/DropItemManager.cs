using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DropItemManager : SingletonBehavior<DropItemManager>
{
    public EnemyDatabase monsterDatabase;

    public event Action<int> OnDropItem;

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
        if(monsterDatabase == null || monsterId <= 0 || monsterDatabase.TryGetDataSO(monsterId) == null)
        {
            Debug.Log("spawn에서 잘못됨");
            return;
        }

        int dropId = monsterDatabase.TryGetDropItemID(monsterId);
        string dropName = monsterDatabase.TryGetDropItemName(monsterId);

        if(dropId <= 0 || dropName == null)
        {
            Debug.Log("db에서 가져오는데 잘못됨");
            return;
        }

        OnDropItem?.Invoke(dropId);
        Debug.Log($"아이템 ID : {dropId} / 아이템 이름 : {dropName} >> 아이템 소환 /소환 로직 구현 추가되면 넣을 예정");
    }
}
