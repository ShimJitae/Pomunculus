using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("페이지 순서")]
    [SerializeField] private GameObject[] pages;

    [Header("버튼")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;

    [Header("페이지 번호")]
    [SerializeField] private TMP_Text pageText;

    [Header("상호작용 안내")]
    [SerializeField] private GameObject interactionText;

    public GameObject InteractionText => interactionText;

    private int currentPage;

    private void OnEnable()
    {
        currentPage = 0;
        RefreshPages();
    }

    public void NextPage()
    {
        if (pages == null || currentPage >= pages.Length - 1)
            return;

        currentPage++;
        RefreshPages();
    }

    public void BackPage()
    {
        if (pages == null || currentPage <= 0)
            return;

        currentPage--;
        RefreshPages();
    }

    private void RefreshPages()
    {
        int pageCount = pages != null ? pages.Length : 0;

        for (int i = 0; i < pageCount; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == currentPage);
        }

        if (backButton != null)
            backButton.interactable = currentPage > 0;

        if (nextButton != null)
            nextButton.interactable = currentPage < pageCount - 1;

        if (pageText != null)
            pageText.text = $"{(pageCount > 0 ? currentPage + 1 : 0)} / {pageCount}";
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}