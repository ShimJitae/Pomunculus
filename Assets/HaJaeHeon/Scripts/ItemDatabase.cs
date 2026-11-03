using Pomunculus.Items;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
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
            MaterialData so = data[i];

            if (so == null)
                continue;

            int itemId = so.Id;

            if (itemId <= 0 || !material.TryAdd(itemId, so))
                continue;

            Debug.Log($"키 : {itemId} / 밸류 : {material.Values}");
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
}
