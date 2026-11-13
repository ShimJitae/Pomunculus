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

    [Tooltip("활성 모델의 범위에 1m 기준 연출의 중심과 크기를 생성 시 한 번 맞춤")]
    [SerializeField] private bool fitToModelBounds;

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
                if (i > endingFeedbacks.Count)
                {
                    return;
                }
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

        var feedbacksToRelease = new List<StatusEffectFeedback>(activeFeedbacks.Values);
        feedbacksToRelease.AddRange(endingFeedbacks);
        activeFeedbacks.Clear();
        endingFeedbacks.Clear();
        foreach (StatusEffectFeedback feedbacks in feedbacksToRelease)
        {
            Release(feedbacks);
        }
    }

    private StatusEffectFeedback CreateFeedbacks(StatusEffect effect)
    {
        StatusEffectFeedback prefab = effect?.Data?.FeedbackPrefab;
        StatusEffectFeedbackPool pool = StatusEffectFeedbackPool.Instance;
        if (!isActiveAndEnabled || prefab == null)
        {
            return null;
        }
        if (activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback existing) && existing != null)
        {
            return existing;
        }

        Transform effectAnchor = Anchor;
        Bounds modelBounds = default;
        bool shouldFit = fitToModelBounds && TryGetModelBounds(effectAnchor, out modelBounds);
        StatusEffectFeedback instance = pool != null
            ? pool.Get(prefab)
            : Instantiate(prefab, effectAnchor, false);
        instance.Rental = (pool, prefab);
        Transform instanceTransform = instance.transform;
        Transform prefabTransform = prefab.transform;
        instanceTransform.SetParent(effectAnchor, false);
        instanceTransform.SetLocalPositionAndRotation(
            shouldFit ? modelBounds.center : prefabTransform.localPosition,
            prefabTransform.localRotation);
        instanceTransform.localScale = shouldFit
            ? Vector3.Max(modelBounds.size, Vector3.one * 0.01f)
            : prefabTransform.localScale;

        GameObject target = feedbackTarget != null ? feedbackTarget.gameObject : gameObject;
        activeFeedbacks[effect] = instance;
        instance.gameObject.SetActive(prefab.gameObject.activeSelf);
        if (!activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback current) || current != instance)
        {
            return null;
        }

        instance.Initialize(target);
        return activeFeedbacks.TryGetValue(effect, out current) && current == instance ? instance : null;
    }

    private bool TryGetModelBounds(Transform effectAnchor, out Bounds bounds)
    {
        bounds = default;
        if (EncapsulateModelBounds(GetComponentsInChildren<SkinnedMeshRenderer>(), effectAnchor, ref bounds))
        {
            return true;
        }
        return EncapsulateModelBounds(GetComponentsInChildren<MeshRenderer>(), effectAnchor, ref bounds);
    }

    private static bool EncapsulateModelBounds(Renderer[] renderers, Transform effectAnchor, ref Bounds bounds)
    {
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled || renderer.GetComponentInParent<StatusEffectFeedback>() != null)
            {
                continue;
            }

            // 각 Renderer의 로컬 범위를 anchor 공간으로 옮겨 부모 회전·크기와 여러 모델 파트를 반영한다.
            Bounds localBounds = renderer.localBounds;
            Matrix4x4 toAnchor = effectAnchor.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = localBounds.center + Vector3.Scale(localBounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                point = toAnchor.MultiplyPoint3x4(point);
                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }
        return hasBounds;
    }

    private void HandleRegistered(StatusEffect effect)
    {
        StatusEffectFeedback feedbacks = CreateFeedbacks(effect);
        if (feedbacks != null)
        {
            feedbacks.PlayRegistered();
            if (activeFeedbacks.TryGetValue(effect, out StatusEffectFeedback current) && current == feedbacks)
            {
                feedbacks.PlayWhileActive();
            }
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
        if (feedbacks == null)
        {
            return;
        }

        bool wasActive = feedbacks.gameObject.activeInHierarchy;
        bool canReuse = feedbacks.PrepareForReturn();
        var (pool, prefab) = feedbacks.Rental;
        if (canReuse && wasActive && feedbacks.gameObject.activeInHierarchy && pool != null)
        {
            pool.Return(prefab, feedbacks);
            return;
        }

        if (feedbacks.gameObject.activeInHierarchy)
        {
            feedbacks.gameObject.SetActive(false);
        }
        Destroy(feedbacks.gameObject);
    }
}
