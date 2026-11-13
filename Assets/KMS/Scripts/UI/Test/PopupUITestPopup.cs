using TMPro;
using UnityEngine;

/// <summary>
/// PopupUI 테스트 씬 전용 팝업.
/// 열기/닫기 훅이 몇 번 불렸는지 세서 화면과 콘솔에 보여준다.
/// </summary>
public class PopupUITestPopup : PopupUI
{
    [SerializeField] private TMP_Text countText;

    public int OpenedCount { get; private set; }
    public int ClosedCount { get; private set; }

    // 열린 직후 횟수를 올리고 표시한다
    protected override void OnOpened()
    {
        OpenedCount++;
        Debug.Log($"[PopupUITest] {name} OnOpened ({OpenedCount})", this);
        RefreshCountText();
    }

    // 닫히기 직전 횟수를 올리고 표시한다
    protected override void OnClosed()
    {
        ClosedCount++;
        Debug.Log($"[PopupUITest] {name} OnClosed ({ClosedCount})", this);
        RefreshCountText();
    }

    // 매니저를 거치지 않고 끈다. 목록 정리(RemoveInactivePopups) 테스트용
    public void DisableDirectly()
    {
        Debug.Log($"[PopupUITest] {name} 직접 SetActive(false)", this);
        gameObject.SetActive(false);
    }

    // 훅 호출 횟수를 패널 텍스트에 표시한다
    private void RefreshCountText()
    {
        if (countText != null)
            countText.text = $"Open {OpenedCount}회 / Close {ClosedCount}회";
    }
}
