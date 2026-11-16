
using Unity.Collections;

public class WorldSlotLoader : Singleton<WorldSlotLoader>
{
    public WorldSlotLInfo SaveInfo
    {
        get
        {
            if(m_saveinfo == null)
            {
                Init();
            }
            return m_saveinfo;
        }
    }
    private void Init()
    {
        if(!SaveManager.Instance.Exists(INFONAME))
        {
            m_saveinfo = new WorldSlotLInfo();
            SaveManager.Instance.Save(INFONAME,SaveData.CreateSaveData(m_saveinfo));
        }
        m_saveinfo = SaveManager.Instance.Load(INFONAME).GetData<WorldSlotLInfo>();
    }
    private WorldSlotLInfo m_saveinfo;
    public void CreateNewWorld(string worldName)
    {
        SaveInfo.LastSlotId++;
        WorldSlotData data = new WorldSlotData();
        data.SlotId = SaveInfo.LastSlotId;
        data.WorldName = worldName;
        SaveInfo.Slots.Add(data);
        Save();
    }
    public void DeleteWorld(int id)
    {
        var s = SaveInfo.Slots.Find(x => x.SlotId == id);
        if(s == null)
        {
            UnityEngine.Debug.Log("존재하지 않는 슬롯입니다.");
            return;
        }
        SaveInfo.Slots.Remove(s);
        WorldManager.Instance.DeleteWorld(id);
        Save();
    }
    public void Save()
    {
        SaveManager.Instance.Save(INFONAME,SaveData.CreateSaveData(SaveInfo));
    }
    public const string INFONAME = "WorldSlotInfo";
}