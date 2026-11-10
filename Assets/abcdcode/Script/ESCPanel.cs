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
    [SerializeField]private Transform Root;
}