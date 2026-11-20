using TMPro;
using UnityEngine;

// 프로필(닉네임) 변경 창. m_nextName에 입력한 이름으로 바꾸고 m_curName에 현재 이름을 표시
public class ProfileChangeUI : MonoBehaviour
{
    public void Open()
    {
        this.gameObject.SetActive(true);
    }
    public void Close()
    {
        this.gameObject.SetActive(false);
    }
    public void OnClickChange()
    {
        if (!GameManager.Instance.ChangeUserName(m_nextName.text)) return;
        m_nextName.text = "";
        RefreshCurName();
    }
    private void OnEnable()
    {
        RefreshCurName();
    }
    private void RefreshCurName()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        m_curName.text = gm.LocalUserProfile.UserName;
    }
    [SerializeField]private TMP_Text m_curName;
    [SerializeField]private TMP_InputField m_nextName;
}
