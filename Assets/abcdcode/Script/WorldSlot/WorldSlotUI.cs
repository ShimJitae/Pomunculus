using UnityEngine;

public class WorldSlotUI :MonoBehaviour
{
    public void Init(WorldSlotData data)
    {
        Data = data;
    }
    public void OnClickDelete()
    {
        
    }
    public void OnClickEnter()
    {
        
    }
    public WorldSlotData Data{get;set;}
    [SerializeField]private WorldSlotListUI m_parent;
}