using TMPro;
using UnityEngine;

public class WorldSlotUI :MonoBehaviour
{
    public void Init(WorldSlotData data)
    {
        Data = data;
        m_worldNametxt.text = Data.WorldName;
    }
    public void OnClickDelete()
    {
        WorldSlotLoader.Instance.DeleteWorld(Data.SlotId);
        m_parent.Refresh();
    }
    public void OnClickEnter()
    {
        GameManager.Instance.EnterWorld(Data.SlotId);
    }
    public WorldSlotData Data{get;set;}
    [SerializeField]private TextMeshProUGUI m_worldNametxt;
    [SerializeField]private WorldSlotListUI m_parent;
}