using TMPro;
using UnityEngine;

public class WorldCreateUI : MonoBehaviour
{
    public void Init()
    {
        m_worldName.text = "New World";
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
    public void OnClickCreateWorld()
    {
        string worldName = m_worldName.text;
        WorldSlotLoader.Instance.CreateNewWorld(worldName);
        Close();
        m_slotList.Refresh();
    }
    [SerializeField]private TMP_InputField m_worldName;
    [SerializeField]private WorldSlotListUI m_slotList;
}