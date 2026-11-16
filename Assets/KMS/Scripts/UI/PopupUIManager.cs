using System.Collections.Generic;
using UnityEngine;

public class PopupUIManager : SingletonBehavior<PopupUIManager>
{
    private readonly List<PopupUI> openPopups = new List<PopupUI>();

    // 열려 있는 팝업이 하나라도 있는지
    public bool HasOpenPopup
    {
        get
        {
            RemoveInactivePopups();
            return openPopups.Count > 0;
        }
    }

    // 맨 위(가장 마지막에 연) 팝업. 없으면 null
    public PopupUI TopPopup
    {
        get
        {
            RemoveInactivePopups();

            if (openPopups.Count == 0)
                return null;

            return openPopups[openPopups.Count - 1];
        }
    }

    // 씬에서 켜진 채 시작한 팝업을 목록에 등록한다 (훅은 부르지 않는다)
    private void Start()
    {
        PopupUI[] activePopups = FindObjectsByType<PopupUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (PopupUI popup in activePopups)
        {
            if (!openPopups.Contains(popup))
                openPopups.Add(popup);
        }
    }

    // 팝업을 맨 위에 연다. 이미 열려 있으면 맨 위로 올리기만 한다
    public void Open(PopupUI popupUI)
    {
        RemoveInactivePopups();

        if (openPopups.Contains(popupUI))
        {
            openPopups.Remove(popupUI);
            openPopups.Add(popupUI);
            popupUI.BringToFront();
        }
        else
        {
            openPopups.Add(popupUI);
            popupUI.Show();
        }
    }

    // 팝업을 닫는다. 스택 중간에 있어도 그 팝업만 닫는다
    public void Close(PopupUI popupUI)
    {
        if (openPopups.Contains(popupUI))
        {
            openPopups.Remove(popupUI);
            popupUI.Hide();
        }
        else if (popupUI.gameObject.activeSelf)
        {
            popupUI.Hide();
        }
    }

    // 맨 위 팝업을 닫는다. 닫았으면 true (ESC 처리용)
    public bool TryCloseTop()
    {
        PopupUI top = TopPopup;

        if (top == null)
            return false;

        Close(top);
        return true;
    }

    // 열린 팝업을 위에서부터 모두 닫는다. 닫는 중 목록이 바뀌어도 안전하게 복사본을 돈다
    public void CloseAll()
    {
        RemoveInactivePopups();

        PopupUI[] popupsToClose = openPopups.ToArray();

        for (int i = popupsToClose.Length - 1; i >= 0; i--)
        {
            if (popupsToClose[i] != null)
                Close(popupsToClose[i]);
        }
    }

    // 매니저를 거치지 않고 꺼졌거나 파괴된 팝업을 목록에서 뺀다
    private void RemoveInactivePopups()
    {
        openPopups.RemoveAll(popup => popup == null || !popup.gameObject.activeSelf);
    }
}
