using TMPro;
using UnityEngine;

public class WorldCreateUI : MonoBehaviour
{
    public void Init()
    {
        m_worldName.text = "New World";
        time = 60;
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
        WorldSlotLoader.Instance.CreateNewWorld(worldName,time);
        Close();
        m_slotList.Refresh();
    }
    public void Update()
    {
        m_timeText.text = $"DayTime : {time}";
    }
    public void PressTimeUp()
    {
        time += 1;
    }
    public void PressTimeDown()
    {
        time -= 1;
        if(time <= 10) time = 10;
    }
    private int time;
    [SerializeField]private TMP_Text m_timeText;
    [SerializeField]private TMP_InputField m_worldName;
    [SerializeField]private WorldSlotListUI m_slotList;
}