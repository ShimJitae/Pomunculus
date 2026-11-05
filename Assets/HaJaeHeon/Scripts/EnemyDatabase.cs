using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Scriptable Objects/EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    [SerializeField] private List<EnemyDataSO> data = new();

    private Dictionary<int, EnemyDataSO> enemy = new();

    private void OnEnable()
    {
        InitDatabase();
    }

    private void InitDatabase()
    {
        enemy.Clear();

        for (int i = 0; i < data.Count; i++)
        {
            EnemyDataSO so = data[i];

            if (so == null)
                continue;

            int enemyId = so.GetEnemyId();

            if (enemyId <= 0 || !enemy.TryAdd(enemyId, so))
                continue;

            //Debug.Log($"키 : {enemyId} / 밸류 : {enemy.Values}");
        }

        //foreach (EnemyDataSO m in data)
        //{
        //    enemy.Add(m.GetEnemyId(), m);
        //}
    }

    public EnemyDataSO TryGetDataSO(int monsterId)
    {
        if (enemy.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so;
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return null;
    }

    public int TryGetDropItemID(int monsterId)
    {
        if (enemy.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so.GetDropId();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return -1;
    }

    public string TryGetDropItemName(int monsterId)
    {
        if (enemy.TryGetValue(monsterId, out EnemyDataSO so))
        {
            return so.GetDropName();
        }

        Debug.Log($"{monsterId}에 해당하는 데이터 없음");
        return null;
    }

    public void ShowDB()
    {
        Debug.Log($"원본 목록: {data.Count}개 / 조회용 사전: {enemy.Count}개");
        foreach (var entry in enemy)
        {
            Debug.Log($"ID : {entry.Key} / SO이름 : {entry.Value.name}");
        }
    }

    public void SpawnEnemy(Vector3 spawnPositon)
    {
        int value = Random.Range(0, data.Count);

        GameObject prefab = data[value].GetEnemyPrefab();

        GameObject enemyObject = GameObject.Instantiate(prefab, spawnPositon, Quaternion.identity);

        //GameObject target = PoolEnemy.Instance.player;
        //enemyObject.GetComponent<Enemy>().SetTarget(target);
    }

    public void SpawnWitch(Vector3 spawnPositon)
    {
        GameObject prefab;

        if(enemy.TryGetValue(40006, out  EnemyDataSO so))
        {
            prefab = so.GetEnemyPrefab();

            GameObject enemyObject = GameObject.Instantiate(prefab, spawnPositon, Quaternion.identity);

            //GameObject target = PoolEnemy.Instance.player;
            //enemyObject.GetComponent<Enemy>().SetTarget(target);
        }
    }
}
