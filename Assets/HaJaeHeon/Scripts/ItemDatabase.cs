using Pomunculus.Items;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<MaterialData> data = new();

    private Dictionary<int, MaterialData> material = new Dictionary<int, MaterialData>();

    private void OnEnable()
    {
        InitDatabase();
    }

    private void InitDatabase()
    {
        material.Clear();

        for (int i = 0; i < data.Count; i++)
        {
            MaterialData materialData = data[i];

            if (materialData == null)
                continue;

            int itemId = materialData.Id;

            if (itemId <= 0 || !material.TryAdd(itemId, materialData))
                continue;

            //Debug.Log($"키 : {itemId} / 밸류 : {material.Values}");
        }
    }

    public MaterialData TryGetDataSO(int itemId)
    {
        if (material.TryGetValue(itemId, out MaterialData so))
        {
            return so;
        }

        Debug.Log($"{itemId}에 해당하는 데이터 없음");
        return null;
    }

    public void ShowDB()
    {
        Debug.Log($"원본 목록: {data.Count}개 / 조회용 사전: {material.Count}개");
        foreach (var entry in material)
        {
            Debug.Log($"ID : {entry.Key} / SO이름 : {entry.Value.name}");
        }
    }

    public GameObject GetItemPrefab(int itemId)
    {
        MaterialData itemSO = TryGetDataSO(itemId);

        if (itemSO == null)
        {
            Debug.Log($"{itemId}에 맞는 SO없음");
            return null;
        }
        return itemSO.Prefab;
    }
}
