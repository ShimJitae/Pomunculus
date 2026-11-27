using System.Collections.Generic;
using UnityEngine;

public class WorldSlotListUI : MonoBehaviour
{
    public void Init()
    {
        Refresh();
        m_worldcreateUI.Close();
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
        m_worldcreateUI.Open();
    }
    public void Refresh()
    {
        ClearList();

        List<WorldSlotData> slots = WorldSlotLoader.Instance.SaveInfo.Slots;
        foreach (WorldSlotData data in slots)
        {
            if (!data.IsCompatible) continue; // 버전이 다른 월드는 불러올 수 없으므로 표시하지 않음
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
    [SerializeField]private WorldCreateUI m_worldcreateUI;
}
public class WorldSlotData
{
    public int SlotId{get;set;}
    public string WorldName{get;set;}
    public float DayTime{get;set;} = DefaultDayTime; // 하루 길이(초). 이 값이 없던 이전 슬롯도 기본값으로 읽힌다
    // 슬롯을 만들 때의 GameManager.Version. 이 값이 없던 이전 슬롯은 0으로 읽혀서 호환되지 않는 슬롯으로 처리된다
    // (기본값을 현재 버전으로 두면 이전 슬롯도 역직렬화 시 현재 버전으로 채워지므로 반드시 0으로 둘 것)
    public int WorldVersion{get;set;}
    [Newtonsoft.Json.JsonIgnore] public bool IsCompatible => WorldVersion == GameManager.Instance.Version;
    public const float DefaultDayTime = 60f;
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
