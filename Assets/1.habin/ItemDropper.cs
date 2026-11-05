using UnityEngine;

[RequireComponent(typeof(Entity))]
public class ItemDropper : MonoBehaviour, IWorldObj
{
    [Header("떨어뜨릴 재료 프리팹")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Header("생성 높이")]
    [SerializeField, Min(0f)] private float spawnHeight = 0.5f;

    [Header("아이템 사이 높이 간격")]
    [SerializeField, Min(0.01f)] private float heightGap = 0.5f;

    private bool hasDropped;

    public int ID { get; set; } = -1;
    public string PrefabId { get; set; }

    public void DropItems()
    {
        if (hasDropped)
            return;

        hasDropped = true;

        float height = spawnHeight;

        if (itemPrefabs != null)
        {
            foreach (GameObject prefab in itemPrefabs)
            {
                if (prefab == null)
                    continue;

                Vector3 position = transform.position + Vector3.up * height;

                Instantiate(prefab, position, Quaternion.identity);


                height += heightGap;
            }
        }

        Destroy(gameObject);
    }

    public void InitWorldObj(int id, string prefabId)
    {
        ID = id;
        PrefabId = prefabId;
    }

    public void SaveData(SaveData data)
    {
    }

    public void LoadData(SaveData data)
    {
    }
}

//using System.Collections.Generic;
//using Photon.Pun;
//using UnityEngine;

//[RequireComponent(typeof(Entity))]
//public class ItemDropper : MonoBehaviour, IWorldObj
//{
//    [Header("떨어뜨릴 재료 프리팹")]
//    [SerializeField] private GameObject[] itemPrefabs;

//    [Header("생성 높이")]
//    [SerializeField, Min(0f)] private float spawnHeight = 0.5f;

//    [Header("아이템 사이 높이 간격")]
//    [SerializeField, Min(0.01f)] private float heightGap = 0.5f;

//    private bool hasDropped;

//    public int ID { get; private set; } = -1;
//    public string PrefabId { get; private set; }

//    public void InitWorldObj(int id, string prefabId)
//    {
//        ID = id;
//        PrefabId = prefabId;
//    }

//    public void DropItems()
//    {
//        if (hasDropped)
//            return;

//        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
//            return;

//        WorldManager world = WorldManager.Instance;

//        if (!world.IsReady)
//        {
//            Debug.LogWarning("월드 초기화가 완료되지 않았습니다.", this);
//            return;
//        }

//        if (ID < 0 || !ReferenceEquals(world.Find(ID), this))
//        {
//            Debug.LogWarning("이 오브젝트는 WorldManager.Spawn으로 생성해야 합니다.", this);
//            return;
//        }

//        hasDropped = true;

//        float height = spawnHeight;
//        List<int> spawnedIds = new List<int>();

//        if (itemPrefabs != null)
//        {
//            foreach (GameObject prefab in itemPrefabs)
//            {
//                if (prefab == null)
//                    continue;

//                Vector3 position = transform.position + Vector3.up * height;

//                IWorldObj item = world.Spawn(prefab.name, position, Quaternion.identity);

//                if (item == null)
//                {
//                    foreach (int spawnedId in spawnedIds)
//                    {
//                        world.Despawn(spawnedId);
//                    }

//                    hasDropped = false;

//                    Debug.LogError($"재료 생성 실패: {prefab.name}. 카탈로그 등록과 IWorldObj를 확인해 주세요.", this);
//                    return;
//                }
//                spawnedIds.Add(item.ID);
//                height += heightGap;
//            }
//        }
//        world.Despawn(ID);
//    }
//    public void SaveData(SaveData data)
//    {
//    }

//    public void LoadData(SaveData data)
//    {
//    }
//}