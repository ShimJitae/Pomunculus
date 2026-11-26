using UnityEngine;
using UnityEngine.Playables;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject endingPanel;
    [Tooltip("보스 클리어 후 크레딧보다 먼저 재생할 엔딩 연출 Timeline (평소에는 오브젝트를 꺼 둔다)")]
    [SerializeField] private PlayableDirector endingDirector;

    private bool hasEndingStarted;
    private bool hasCreditOpened;

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
        // 재생 종료와 비활성화가 겹쳐 stopped가 두 번 와도 한 번만 연다
        if (hasCreditOpened)
            return;

        hasCreditOpened = true;
        endingPanel.SetActive(true);
    }

    // 보스 클리어 시 꺼 두었던 엔딩 연출을 켜고 처음부터 재생한다.
    private void PlayEndingTimeline()
    {
        if (hasEndingStarted)
            return;

        hasEndingStarted = true;

        // Timeline이 연결되지 않았으면 기존러럼 바로 크레딧을 연다.
        if(endingDirector == null)
        {
            Debug.Log("엔딩 timeline 없어서 크레딧을 바로 오픈");
            EndingPanelOpen();
            return;
        }

        endingDirector.gameObject.SetActive(true);
        endingDirector.time = 0f;
        endingDirector.Play();
    }

    // TimeLine이 끝나면 (Warp Mode = None)  크레딧을 연다.
    private void HandleEndingTimelineStopped(PlayableDirector stoppedDirector)
    {
        if (!hasEndingStarted)
            return;

        // 엔딩 카메라, 모델 함께 꺼서 원래 플레이어 카메라 화면으로 돌아간다.
        stoppedDirector.gameObject.SetActive(false);

        EndingPanelOpen();
    }


    #region 버튼

    public void OnClickStay()
    {
        endingPanel.SetActive(false);
    }

    public void OnClickTitle()
    {
        GameManager.Instance.GoToTitle();
    }

    #endregion

}
