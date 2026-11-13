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