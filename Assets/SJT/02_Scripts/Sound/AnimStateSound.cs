using UnityEngine;

/// <summary>애니메이터 상태에 붙여, 상태 진입 후 지정 시간에 효과음을 한 번 재생합니다. 원격 플레이어도 애니메이터 상태 동기화로 재생됩니다.</summary>
public sealed class AnimStateSound : StateMachineBehaviour
{
    [SerializeField, Tooltip("재생할 효과음입니다. 비어 있으면 재생하지 않습니다.")] private AudioClip clip;
    [SerializeField, Min(0f), Tooltip("상태 진입 후 재생 시점(초)입니다.")] private float playTime;

    private AudioSource audioSource;
    private bool hasPlayed;
    private float lastNormalizedTime;

    /// <summary>재생 상태를 초기화하고 Animator와 같은 오브젝트의 AudioSource를 한 번 캐싱합니다.</summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasPlayed = false;
        lastNormalizedTime = 0f;

        if (audioSource != null)
        {
            return;
        }

        if (!animator.TryGetComponent(out audioSource))
        {
#if UNITY_EDITOR
            Debug.LogError($"[{nameof(AnimStateSound)}] {animator.name}에 AudioSource가 없습니다.", animator);
#endif
        }
    }

    /// <summary>같은 상태의 재시작을 감지하고, 재생 시점이 지나면 효과음을 한 번 재생합니다.</summary>
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Play(hash, 0, 0f)로 같은 상태를 다시 시작하면 OnStateEnter 없이 진행도만 되돌아갈 수 있다
        float normalizedTime = stateInfo.normalizedTime;
        if (normalizedTime < lastNormalizedTime)
        {
            hasPlayed = false;
        }

        lastNormalizedTime = normalizedTime;

        if (hasPlayed || clip == null || audioSource == null)
        {
            return;
        }

        // 원격 화면에서 상태가 늦게 시작해 이미 재생 시점을 지났어도 바로 재생한다
        if (normalizedTime * stateInfo.length < playTime)
        {
            return;
        }

        hasPlayed = true;
        audioSource.PlayOneShot(clip);
    }
}
