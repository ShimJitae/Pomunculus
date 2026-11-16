using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>외부 프리팹의 Feel Player에 공통 대상을 연결하고 상태 시점별로 실행한다.</summary>
[DisallowMultipleComponent]
public sealed class StatusEffectFeedback : MonoBehaviour
{
    [Header("상태 이벤트")]
    [SerializeField] private MMF_Player registeredFeedbacks;
    [SerializeField] private MMF_Player reappliedFeedbacks;
    [SerializeField] private MMF_Player tickFeedbacks;
    [Tooltip("무한 반복 없이 종료되는 Player를 연결. 종료 시 값 복원도 외부 Feedback에서 구성")]
    [SerializeField] private MMF_Player removedFeedbacks;

    [Header("상태 유지")]
    [FormerlySerializedAs("bodyColorFeedbacks")]
    [Tooltip("상태 시작 시 한 번 재생하고 종료 시 정지. 반복과 연출 내용은 Feel에서 구성")]
    [SerializeField] private MMF_Player whileActiveFeedbacks;

    [SerializeField] private StatusEffectPetrifySurface petrifySurface;

    private bool initialized;
    private bool whileActiveStarted;
    private bool canReuse;
    private bool preparedForReturn;

    public bool IsRemovalPlaying => removedFeedbacks != null && removedFeedbacks.IsPlaying;
    internal (StatusEffectFeedbackPool pool, StatusEffectFeedback prefab) Rental { get; set; }

    /// <summary>대여마다 대상을 연결하고 Feel Player는 처음 한 번만 초기화한다.</summary>
    public void Initialize(GameObject target)
    {
        canReuse = true;
        preparedForReturn = false;

        if (petrifySurface != null)
        {
            petrifySurface.Initialize(target);
        }

        InitializePlayer(registeredFeedbacks, target);
        InitializePlayer(reappliedFeedbacks, target);
        InitializePlayer(tickFeedbacks, target);
        InitializePlayer(removedFeedbacks, target);
        InitializePlayer(whileActiveFeedbacks, target);
        initialized = true;
    }

    public void PlayRegistered() => Play(registeredFeedbacks);
    public void PlayReapplied() => Play(reappliedFeedbacks);
    public void PlayTick() => Play(tickFeedbacks);
    public void PlayRemoved() => Play(removedFeedbacks);

    /// <summary>재부여·틱으로 유지 연출이 중복 시작되지 않도록 한 번만 재생한다.</summary>
    public void PlayWhileActive()
    {
        if (!initialized || !isActiveAndEnabled || whileActiveStarted)
        {
            return;
        }
        whileActiveStarted = true;
        Play(whileActiveFeedbacks);
    }

    /// <summary>상태가 끝나면 등록·재부여·틱·유지 Player와 지연 코루틴을 정지한다.</summary>
    public void StopActiveFeedbacks()
    {
        if (!initialized)
        {
            return;
        }
        Stop(registeredFeedbacks);
        Stop(reappliedFeedbacks);
        Stop(tickFeedbacks);
        Stop(whileActiveFeedbacks);
        whileActiveStarted = false;
    }

    /// <summary>인스턴스 정리 시 모든 Player를 정지한다. 값 복원은 외부 연출이 담당한다.</summary>
    public void StopAndCleanup()
    {
        StopActiveFeedbacks();
        if (initialized)
        {
            Stop(removedFeedbacks);
        }
    }

    internal bool PrepareForReturn()
    {
        preparedForReturn = true;
        StopAndCleanup();

        ResetPlayer(registeredFeedbacks);
        ResetPlayer(reappliedFeedbacks);
        ResetPlayer(tickFeedbacks);
        ResetPlayer(removedFeedbacks);
        ResetPlayer(whileActiveFeedbacks);

        if (petrifySurface != null)
        {
            petrifySurface.Restore();
            petrifySurface.Initialize(null);
        }

        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        return initialized && canReuse;
    }

    private void OnDisable()
    {
        if (!preparedForReturn)
        {
            StopAndCleanup();
        }
    }

    private void Play(MMF_Player player)
    {
        if (initialized && !preparedForReturn && isActiveAndEnabled && player != null)
        {
            player.PlayFeedbacks(transform.position);
        }
    }

    private void Stop(MMF_Player player)
    {
        if (player != null)
        {
            // Feel은 StopFeedbacks에서 내부 완료 대기 플래그를 지우지 않는다.
            if (player.IsPlaying && player.ElapsedTime >= player.TotalDuration && !RepeatsForever(player))
            {
                canReuse = false;
            }

            player.StopFeedbacks();
            // Feel 5.9.1의 초기 지연 코루틴도 취소해 종료 후 뒤늦은 재생을 막는다.
            player.StopAllCoroutines();
        }
    }

    private static bool RepeatsForever(MMF_Player player)
    {
        foreach (MMF_Feedback feedback in player.FeedbacksList)
        {
            if (feedback?.Active == true && feedback.Timing?.RepeatForever == true)
            {
                return true;
            }
        }
        return false;
    }

    private void InitializePlayer(MMF_Player player, GameObject target)
    {
        if (player == null)
        {
            return;
        }

        BindTarget(player, target);
        if (!initialized)
        {
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            player.AutoInitialization = false;
            player.AutoPlayOnEnable = false;
            player.AutoPlayOnStart = false;
            player.StopFeedbacksOnDisable = true;
            player.Initialization();
            return;
        }

        foreach (MMF_Feedback feedback in player.FeedbacksList)
        {
            if (feedback?.HasAutomatedTargetAcquisition == true &&
                (feedback.ForcedReferenceHolder != null ||
                 (feedback.AutomatedTargetAcquisition != null &&
                  feedback.AutomatedTargetAcquisition.Mode != MMFeedbackTargetAcquisition.Modes.None)))
            {
                feedback.ForceAutomateTargetAcquisition();
            }
        }
    }

    private static void ResetPlayer(MMF_Player player)
    {
        if (player == null || player.FeedbacksList == null)
        {
            return;
        }

        BindTarget(player, null);
        player.ResetAllCooldowns();
        player.PlayCount = 0;
        player.InScriptDrivenPause = false;
        foreach (MMF_Feedback feedback in player.FeedbacksList)
        {
            if (feedback != null)
            {
                feedback.IsPlaying = false;
                feedback.InInitialDelay = false;
                feedback.ResetPlayCount();
            }
        }
    }

    private static void BindTarget(MMF_Player player, GameObject target)
    {
        if (player.FeedbacksList == null)
        {
            return;
        }

        foreach (MMF_Feedback feedback in player.FeedbacksList)
        {
            if (feedback is MMF_ReferenceHolder holder && holder.Label == "Target")
            {
                holder.GameObjectReference = target;
            }
        }
    }
}
