using UnityEngine;

public class ESCPanel : MonoBehaviour
{
    private PopupUI escPopup;

    // ESC 키를 직접 처리할지. OpenUI처럼 ESC를 대신 처리하는 스크립트가 있으면 false로 바꾼다
    public bool HandlesEscapeKey { get; set; } = true;

    public void Awake()
    {
        escPopup = Root.GetComponent<PopupUI>();

        Root.gameObject.SetActive(false);
    }
    public void Update()
    {
        // 채팅 중의 Esc는 채팅창을 닫는 데 쓰이므로 무시
        if(HandlesEscapeKey && Input.GetKeyDown(KeyCode.Escape) && !ChatUI.IsTyping)
        {
            Toggle();
        }
    }
    public void Toggle()
    {
        if(escPopup != null)
        {
            escPopup.Toggle();
            return;
        }

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