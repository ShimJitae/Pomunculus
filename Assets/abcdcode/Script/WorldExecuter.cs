using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldExecuter : MonoBehaviour
{
    public void Awake()
    {
        NetworkManager.Instance.OnJoinedRoomEvent += OnJoinRoom;
        NetworkManager.Instance.OnLeftRoomEvent += OnLeaveRoom;
    }
    public void OnDestroy()
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= OnJoinRoom;
        NetworkManager.Instance.OnLeftRoomEvent -= OnLeaveRoom;
    }
    public void OnClickOpenWorld()
    {
        var name = m_roomNameText.text;
        NetworkManager.Instance.CreateRoom(name);
    }
    public void OnJoinRoom()
    {
        m_openWorldBtn.interactable = false;
    }
    public void OnLeaveRoom()
    {
        m_openWorldBtn.interactable = true;
    }
    [SerializeField]private Button m_openWorldBtn; 
    [SerializeField]private TMP_InputField m_roomNameText;
}