using UnityEngine;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject endingPanel;

    private void OnEnable()
    {
        CheckClearGame.OnChangeGameState += EndingPanelOpen;
    }

    private void OnDisable()
    {
        CheckClearGame.OnChangeGameState -= EndingPanelOpen;
    }


    private void EndingPanelOpen()
    {
        endingPanel.SetActive(true);
    }
}
