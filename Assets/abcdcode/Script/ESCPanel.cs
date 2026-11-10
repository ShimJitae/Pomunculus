using UnityEngine;

public class ESCPanel : MonoBehaviour
{
    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }
    }
    public void Toggle()
    {
        if(Root.gameObject.activeSelf)
        {
            Root.gameObject.SetActive(false);
        }
        else
        {
            Root.gameObject.SetActive(true);
        }
    }
    public void GoToTitle()
    {
        NetworkManager.Instance.LeaveRoom();
    }
    [SerializeField]private Transform Root;
}