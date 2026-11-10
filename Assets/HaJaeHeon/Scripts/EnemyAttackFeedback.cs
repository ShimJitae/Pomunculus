using MoreMountains.Feedbacks;
using UnityEngine;

// 적 스킬 연출: 스킬 위치(월드 좌표)에 파티클을 소환. 대상에 붙는 상태이상 연출과 달리 따라다니지 않음
[DisallowMultipleComponent]
public class EnemyAttackFeedback : MonoBehaviour
{
    [SerializeField] private MMF_Player telegraphFeedbacks;   // 예고 시작 시 (없으면 비워 둠)
    [SerializeField] private MMF_Player hitFeedbacks;         // 판정 순간

    private void Awake()
    {
        InitializePlayer(telegraphFeedbacks);
        InitializePlayer(hitFeedbacks);
    }

    public void PlayTelegraph(Vector3 position, Quaternion rotation) => PlayAt(telegraphFeedbacks, position, rotation);
    public void PlayHit(Vector3 position, Quaternion rotation) => PlayAt(hitFeedbacks, position, rotation);

    // 그로기·기절·사망으로 취소되면 예고 연출 정지 (Feel의 초기 지연 코루틴까지 정지)
    public void StopTelegraph()
    {
        if (telegraphFeedbacks == null)
            return;

        telegraphFeedbacks.StopFeedbacks();
        telegraphFeedbacks.StopAllCoroutines();
    }

    // Position Mode = Script → 넘긴 위치에 소환 / Apply Rotation → Player 오브젝트의 회전을 사용
    private static void PlayAt(MMF_Player feedbackPlayer, Vector3 position, Quaternion rotation)
    {
        if (feedbackPlayer == null)
            return;

        feedbackPlayer.transform.rotation = rotation;
        feedbackPlayer.PlayFeedbacks(position);
    }

    private static void InitializePlayer(MMF_Player feedbackPlayer)
    {
        if (feedbackPlayer == null)
            return;

        feedbackPlayer.InitializationMode = MMFeedbacks.InitializationModes.Script;
        feedbackPlayer.AutoPlayOnEnable = false;
        feedbackPlayer.AutoPlayOnStart = false;
        feedbackPlayer.Initialization();
    }
}