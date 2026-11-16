using System.Collections.Generic;
using UnityEngine;

public class WorldSlotListUI : MonoBehaviour
{
    public void Init()
    {
        Refresh();
    }
    public void Open()
    {
        this.gameObject.SetActive(true);
        Init();
    }
    public void Close()
    {
        this.gameObject.SetActive(false);
    }
    public void ClearList()
    {
        // 기존 슬롯은 비활성화 후 풀로 반환
        foreach (WorldSlotUI slot in m_List)
        {
            slot.gameObject.SetActive(false);
            m_pool.Enqueue(slot);
        }
        m_List.Clear();
    }
    public void OnClickCreateWorld()
    {
        
    }
    public void Refresh()
    {
        ClearList();

        List<WorldSlotData> slots = WorldSlotLoader.Instance.SaveInfo.Slots;
        foreach (WorldSlotData data in slots)
        {
            WorldSlotUI slotUI;
            if (m_pool.Count > 0)
            {
                slotUI = m_pool.Dequeue();
            }
            else
            {
                GameObject go = Instantiate(m_SlotPrefab, m_SlotListRoot);
                slotUI = go.GetComponent<WorldSlotUI>();
            }
            slotUI.transform.SetAsLastSibling();
            slotUI.gameObject.SetActive(true);
            slotUI.Init(data);
            m_List.Add(slotUI);
        }
    }
    private List<WorldSlotUI> m_List = new List<WorldSlotUI>();
    private Queue<WorldSlotUI> m_pool = new Queue<WorldSlotUI>();
    [SerializeField]private GameObject m_SlotPrefab;
    [SerializeField]private Transform m_SlotListRoot;
}
public class WorldSlotData
{
    public int SlotId{get;set;}
    public string WorldName{get;set;}
}
public class WorldSlotLInfo
{
    public WorldSlotLInfo()
    {
        Slots = new List<WorldSlotData>();
    }
    public int LastSlotId{get;set;}
    public List<WorldSlotData> Slots{get;set;}
}
