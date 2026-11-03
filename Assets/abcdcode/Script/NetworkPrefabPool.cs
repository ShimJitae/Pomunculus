using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class NetworkPrefabPool : IPunPrefabPool
{
    private readonly Dictionary<string, GameObject> m_prefabs;
    private readonly Dictionary<string, Stack<GameObject>> m_pool = new();

    public NetworkPrefabPool(NetworkPrefabCatalog catalog)
    {
        m_prefabs = catalog.Entries.ToDictionary(x => x.name);
    }

    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        GameObject go;
        if (m_pool.TryGetValue(prefabId, out var stack) && stack.Count > 0)
        {
            go = stack.Pop();
            go.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            go = Object.Instantiate(m_prefabs[prefabId], position, rotation);
            go.name = prefabId; // Destroy에서 키로 씀
        }
        go.SetActive(false); // 비활성 상태로 반환해야 함. PUN이 ViewID 설정 후 활성화함
        return go;
    }

    public void Destroy(GameObject go)
    {
        go.SetActive(false);
        if (!m_pool.TryGetValue(go.name, out var stack))
            m_pool[go.name] = stack = new Stack<GameObject>();
        stack.Push(go);
    }
}