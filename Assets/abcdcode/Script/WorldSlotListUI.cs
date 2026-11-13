using System.Collections.Generic;
using UnityEngine;

public class WorldSlotListUI : MonoBehaviour
{
    public void Awake()
    {
        
    }
    public void Init()
    {
        SaveManager.Instance.Exists(SAVE_NAME);
    }
    public void ClearList()
    {
        
    }
    private List<WorldSlotUI> m_List = new List<WorldSlotUI>();
    private Queue<WorldSlotUI> m_pool = new Queue<WorldSlotUI>();
    [SerializeField]private GameObject m_SlotPrefab;
    public const string SAVE_NAME = "SaveSlotInfo";
}
public class WorldSlotData
{
    public int SlotId{get;set;}
    public string WorldName{get;set;}
}
public class WorldSlotLInfo
{
    public List<WorldSlotData> Slots{get;set;}
}
