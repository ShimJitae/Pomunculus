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
    public void Save()
    {
        SaveManager.Instance.Save(INFONAME,SaveData.CreateSaveData(SaveInfo));
    }
    public const string INFONAME = "WorldSlotInfo";
}