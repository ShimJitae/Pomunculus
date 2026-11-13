using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PopupUI 테스트 씬의 키 입력과 상태 표시를 담당한다.
/// 1~4: 팝업 토글 / Esc: 맨 위 닫기 / X: 모두 닫기
/// </summary>
public class PopupUITester : MonoBehaviour
{
    [SerializeField] private PopupUITestPopup[] popups = new PopupUITestPopup[4];
    [SerializeField] private TMP_Text statusText;

    private static readonly Key[] toggleKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
    private readonly StringBuilder statusBuilder = new StringBuilder();

    // 키 입력을 처리하고 상태 텍스트를 갱신한다
    private void Update()
    {
        HandleKeyboard();
        RefreshStatusText();
    }

    // 테스트용 단축키를 읽는다
    private void HandleKeyboard()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        for (int i = 0; i < popups.Length && i < toggleKeys.Length; i++)
        {
            if (popups[i] != null && keyboard[toggleKeys[i]].wasPressedThisFrame)
                popups[i].Toggle();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
            CloseTop();

        if (keyboard.xKey.wasPressedThisFrame)
            CloseAll();
    }

    // 맨 위 팝업을 닫는다. 버튼에서도 호출한다
    public void CloseTop()
    {
        if (PopupUIManager.Instance == null)
            return;

        bool closed = PopupUIManager.Instance.TryCloseTop();
        Debug.Log($"[PopupUITest] TryCloseTop → {closed}");
    }

    // 열린 팝업을 모두 닫는다. 버튼에서도 호출한다
    public void CloseAll()
    {
        if (PopupUIManager.Instance == null)
            return;

        PopupUIManager.Instance.CloseAll();
        Debug.Log("[PopupUITest] CloseAll");
    }

    // 매니저 상태와 팝업별 상태를 텍스트로 보여준다
    private void RefreshStatusText()
    {
        if (statusText == null)
            return;

        PopupUIManager manager = PopupUIManager.Instance;

        statusBuilder.Clear();
        statusBuilder.AppendLine("<b>[키]</b> 1~4 토글 / Esc 맨 위 닫기 / X 모두 닫기");
        statusBuilder.AppendLine();

        if (manager == null)
        {
            statusBuilder.AppendLine("<color=#FF6060>PopupUIManager 없음 (폴백 동작)</color>");
        }
        else
        {
            PopupUI top = manager.TopPopup;
            statusBuilder.AppendLine($"HasOpenPopup : {manager.HasOpenPopup}");
            statusBuilder.AppendLine($"TopPopup     : {(top != null ? top.name : "-")}");
        }

        statusBuilder.AppendLine();
        statusBuilder.AppendLine("이름 | 열림 | sibling | Open/Close 훅");

        foreach (PopupUITestPopup popup in popups)
        {
            if (popup == null)
                continue;

            string openMark = popup.IsOpen ? "<color=#60FF60>O</color>" : "<color=#808080>X</color>";
            statusBuilder.AppendLine(
                $"{popup.name} | {openMark} | {popup.transform.GetSiblingIndex()} | {popup.OpenedCount}/{popup.ClosedCount}");
        }

        statusText.text = statusBuilder.ToString();
    }
}
