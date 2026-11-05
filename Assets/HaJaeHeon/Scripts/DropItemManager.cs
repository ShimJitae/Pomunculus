using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DropItemManager : SingletonBehavior<DropItemManager>
{
    public EnemyDatabase EnemyDatabase;
    public ItemDatabase ItemDatabase;

    public event Action<int> OnDropItem;

    public float randomRange;

    public Vector3 ItemDropOffset;

    [ContextMenu("DB확인")]
    public void ShowEnemyDB()
    {
        EnemyDatabase.ShowDB();
    }

    public void ShowItemDB()
    {
        ItemDatabase.ShowDB();
    }

    public int TryGetDropItemID(int monsterId)
    {
        if (EnemyDatabase.TryGetDropItemID(monsterId) != -1)
        {
            return EnemyDatabase.TryGetDataSO(monsterId).GetDropId();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return -1;
    }

    public void SpawnDropItem(int monsterId, Vector3 spawnPosition)
    {
        if(EnemyDatabase == null || monsterId <= 0 || EnemyDatabase.TryGetDataSO(monsterId) == null)
        {
            Debug.Log("spawn에서 잘못됨");
            return;
        }

        int dropId = EnemyDatabase.TryGetDropItemID(monsterId);
        string dropName = EnemyDatabase.TryGetDropItemName(monsterId);

        if(dropId <= 0 || dropName == null)
        {
            Debug.Log("db에서 가져오는데 잘못됨");
            return;
        }

        if (ItemDatabase != null && ItemDatabase.GetItemPrefab(dropId) != null)
        {
            GameObject item = ItemDatabase.GetItemPrefab(dropId);
            GameObject.Instantiate(item, spawnPosition + ItemDropOffset, Quaternion.identity);
        }

        Debug.Log($"아이템 ID : {dropId} / 아이템 이름 : {dropName} >> 아이템 소환 /소환 로직 구현 추가되면 넣을 예정");
        
        OnDropItem?.Invoke(dropId);
    }

    public Vector3 RandomPositionSpawn()
    {
        float randomValue = UnityEngine.Random.Range(-randomRange, randomRange);
        Vector3 randomPosition = new Vector3(randomValue, 0f, randomValue);

        return randomPosition;
    }

    [ContextMenu("적 소환")]
    public void SpawnEnemy()
    {
        EnemyDatabase.SpawnEnemy(RandomPositionSpawn());
    }

    [ContextMenu("마녀 소환")]
    public void SpawnWitch()
    {
        EnemyDatabase.SpawnWitch(RandomPositionSpawn());
    }
}
