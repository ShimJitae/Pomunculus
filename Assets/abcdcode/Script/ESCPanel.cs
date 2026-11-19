using UnityEngine;

public class ESCPanel : MonoBehaviour
{
    public void Awake()
    {
        Root.gameObject.SetActive(false);
    }
    public void Update()
    {
        // 채팅 중의 Esc는 채팅창을 닫는 데 쓰이므로 무시
        if(Input.GetKeyDown(KeyCode.Escape) && !ChatUI.IsTyping)
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
        GameManager.Instance.GoToTitle();
    }
    public void SpawnMob()
    {
        WorldManager.Instance.Spawn("EvilMagePADefault",new Vector3(5,1,5),Quaternion.identity,null);
    }
    public void SpawnTree()
    {
         WorldManager.Instance.Spawn("GatherTree",new Vector3(-5,1,-5),Quaternion.identity,null);
    }
    [SerializeField]private Transform Root;
}