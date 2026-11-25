using TMPro;
using UnityEngine;

public class PlayerNameUI : MonoBehaviour
{
    private Transform cameraTransform;
        private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            cameraTransform = mainCamera.transform;
        }

        transform.rotation = cameraTransform.rotation;
    }
    [SerializeField]private TMP_Text m_nameText;
    public string PlayerName
    {
        get
        {
            return m_nameText.text;
        }
        set
        {
            m_nameText.text = value;
        }
    }
}
public static class PlayerNameUIExtension
{
    public static PlayerNameUI GetNameUI(this PlayerFSM player)
    {
        for(int i = 0; i < player.transform.childCount; i++)
        {
            var pnu = player.transform.GetChild(i).gameObject.GetComponent<PlayerNameUI>();
            if(pnu != null) return pnu;
        }
        return null;
    }
}