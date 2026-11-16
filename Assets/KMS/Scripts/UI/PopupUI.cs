using UnityEngine;

public class PopupUI : MonoBehaviour
{
    [SerializeField] private bool bringToFrontOnOpen = true;

    private bool isClosing;

    public bool IsOpen => gameObject.activeSelf;

    // 팝업을 연다. 매니저가 없으면 직접 켠다
    public void Open()
    {
        if (PopupUIManager.Instance != null)
            PopupUIManager.Instance.Open(this);
        else if (!IsOpen)
            Show();
    }

    // 팝업을 닫는다. 매니저가 없으면 직접 끈다
    public void Close()
    {
        if (PopupUIManager.Instance != null)
            PopupUIManager.Instance.Close(this);
        else if (IsOpen)
            Hide();
    }

    // 열려 있으면 닫고, 닫혀 있으면 연다
    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    // 매니저 전용: 닫기 훅을 부른 뒤 끈다. 훅 안에서 다시 닫거나 예외가 나도 한 번만, 반드시 꺼진다
    internal void Hide()
    {
        if (isClosing)
            return;

        isClosing = true;

        try
        {
            OnClosed();
        }
        finally
        {
            gameObject.SetActive(false);
            isClosing = false;
        }
    }

    // 매니저 전용: 설정이 켜져 있으면 같은 부모 아래 맨 앞에 그린다
    internal void BringToFront()
    {
        if (bringToFrontOnOpen)
            transform.SetAsLastSibling();
    }

    // 매니저 전용: 켜고 맨 앞으로 올린 뒤 열기 훅을 부른다
    internal void Show()
    {
        gameObject.SetActive(true);
        BringToFront();
        OnOpened();
    }

    // 열린 직후 하위 클래스가 할 일
    protected virtual void OnOpened() { }
    // 닫히기 직전 하위 클래스가 할 일 (예: 제작창 재료 반환)
    protected virtual void OnClosed() { }
}
