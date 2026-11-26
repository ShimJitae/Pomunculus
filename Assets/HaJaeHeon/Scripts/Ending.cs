using UnityEngine;
using UnityEngine.Playables;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private PlayableDirector endingDirector;

    private bool hasEndingStarted;

    private void OnEnable()
    {
        CheckClearGame.OnChangeGameState += PlayEndingTimeline;

        if (endingDirector != null)
            endingDirector.stopped += HandleEndingTimelineStopped;
    }

    private void OnDisable()
    {
        CheckClearGame.OnChangeGameState -= PlayEndingTimeline;

        if (endingDirector != null)
            endingDirector.stopped -= HandleEndingTimelineStopped;
    }


    private void EndingPanelOpen()
    {
        endingPanel.SetActive(true);
    }

    private void PlayEndingTimeline()
    {
        if (hasEndingStarted)
            return;

        hasEndingStarted = true;

        if(endingDirector == null)
        {
            Debug.Log("엔딩 timeline 없어서 크레딧을 바로 오픈");
            EndingPanelOpen();
            return;
        }

        endingDirector.time = 0f;
        endingDirector.Play();
    }

    private void HandleEndingTimelineStopped(PlayableDirector stoppedDirector)
    {
        if (!hasEndingStarted)
            return;

        EndingPanelOpen();
    }
}
