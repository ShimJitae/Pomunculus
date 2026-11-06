using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>상태 이벤트를 외부 Feel 연출 인스턴스에 전달하고 생성·종료 수명을 관리한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
public sealed class StatusEffectVisualizer : MonoBehaviour
{
    [Tooltip("연출 프리팹을 붙일 기준. 비우면 이 오브젝트를 사용")]
    [SerializeField] private Transform anchor;

    [FormerlySerializedAs("modelRoot")]
    [Tooltip("Feel의 Target Reference Holder에 전달할 대상. 비우면 이 오브젝트를 사용")]
    [SerializeField] private Transform feedbackTarget;

    private readonly Dictionary<StatusEffect, StatusEffectFeedback> activeFeedbacks = new Dictionary<StatusEffect, StatusEffectFeedback>();
    private readonly List<StatusEffectFeedback> endingFeedbacks = new List<StatusEffectFeedback>();
    private StatusEffectController controller;

    public Transform Anchor => anchor != null ? anchor : transform;

    private void Awake()
    {
        controller = GetComponent<StatusEffectController>();
    }

    private void OnEnable()
    {
        controller.OnEffectRegistered += HandleRegistered;
        controller.OnEffectReapplied += HandleReapplied;
        controller.OnEffectTicked += HandleTicked;
        controller.OnEffectRemoved += HandleRemoved;

        if (controller.ActiveStatusEffects == null)
        {
            return;
        }

        // 재활성화는 지속 연출만 복구한다. 초기화 콜백이 상태 목록을 바꿀 수 있어 복사해서 순회한다.
        var effects = new List<StatusEffect>(controller.ActiveStatusEffects);
        foreach (StatusEffect effect in effects)
        {
            StatusEffectFeedback feedbacks = CreateFeedbacks(effect);
            if (feedbacks != null)
            {
                feedbacks.PlayWhileActive();
            }
        }
    }

    private void Update()
    {
        for (int i = endingFeedbacks.Count - 1; i >= 0; i--)
        {
            StatusEffectFeedback feedbacks = endingFeedbacks[i];
            if (feedbacks == null || !feedbacks.IsRemovalPlaying)
            {
                endingFeedbacks.RemoveAt(i);
                Release(feedbacks);
            }
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnEffectRegistered -= HandleRegistered;
            controller.OnEffectReapplied -= HandleReapplied;
            controller.OnEffectTicked -= HandleTicked;
            controller.OnEffectRemoved -= HandleRemoved;
        }

        foreach (StatusEffectFeedback feedbacks in activeFeedbacks.Values)
        {
            Release(feedbacks);
        }
        foreach (StatusEffectFeedback feedbacks in endingFeedbacks)
        {
            Release(feedbacks);
        }
        activeFeedbacks.Clear();
        endingFeedbacks.Clear();
    }

    private StatusEffectFeedback CreateFeedbacks(StatusEffect effect)
    {
        if (!isActiveAndEnabled || effect?.Data == null || effect.Data.FeedbackPrefab == null)
        {
            return null;
        }
        if (activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback existing))
        {
            return existing;
        }

        StatusEffectFeedback instance = Instantiate(effect.Data.FeedbackPrefab, Anchor, false);
        GameObject target = feedbackTarget != null ? feedbackTarget.gameObject : gameObject;
        instance.Initialize(target);
        if (!isActiveAndEnabled)
        {
            Release(instance);
            return null;
        }
        activeFeedbacks.Add(effect, instance);
        return instance;
    }

    private void HandleRegistered(StatusEffect effect)
    {
        StatusEffectFeedback feedbacks = CreateFeedbacks(effect);
        if (feedbacks != null)
        {
            feedbacks.PlayRegistered();
            feedbacks.PlayWhileActive();
        }
    }

    private void HandleReapplied(StatusEffect effect)
    {
        if (activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback feedbacks) && feedbacks != null)
        {
            feedbacks.PlayReapplied();
        }
    }

    private void HandleTicked(StatusEffect effect)
    {
        if (activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback feedbacks) && feedbacks != null)
        {
            feedbacks.PlayTick();
        }
    }

    private void HandleRemoved(StatusEffect effect)
    {
        if (!activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback feedbacks))
        {
            return;
        }

        activeFeedbacks.Remove(effect);
        if (feedbacks != null)
        {
            // Feel 콜백이 대상을 비활성화해도 OnDisable에서 이 인스턴스를 정리할 수 있게 먼저 보관한다.
            endingFeedbacks.Add(feedbacks);
            feedbacks.StopActiveFeedbacks();
            if (isActiveAndEnabled && endingFeedbacks.Contains(feedbacks) && feedbacks != null)
            {
                feedbacks.PlayRemoved();
            }
        }
    }

    private static void Release(StatusEffectFeedback feedbacks)
    {
        if (feedbacks != null)
        {
            feedbacks.StopAndCleanup();
            feedbacks.gameObject.SetActive(false);
            Destroy(feedbacks.gameObject);
        }
    }
}
