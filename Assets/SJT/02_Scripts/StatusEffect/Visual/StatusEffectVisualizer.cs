using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 같은 오브젝트의 StatusEffectController 이벤트를 받아 각 상태의 연출 파츠를 실행한다.
/// 파츠는 공유 에셋이므로 대상별 실행 상태(외곽선 채널 등)는 이 컴포넌트가 가진다.
/// 이 컴포넌트가 없으면 상태이상 로직은 연출 없이 그대로 동작한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
public sealed class StatusEffectVisualizer : MonoBehaviour
{
    [Tooltip("파티클 등 연출을 재생할 기준 위치. 비우면 이 오브젝트의 위치를 사용")]
    [SerializeField] private Transform anchor;

    [Tooltip("외곽선을 표시할 렌더러 목록. 비우면 자식의 MeshRenderer·SkinnedMeshRenderer를 자동으로 수집")]
    [SerializeField] private Renderer[] targetRenderers;

    private StatusEffectController controller;
    private StatusOutlineChannel outlineChannel;

    /// <summary>연출을 재생할 기준 위치를 반환한다. 지정한 기준이 런타임에 파괴됐으면 이 오브젝트의 위치를 사용한다.</summary>
    public Transform Anchor => anchor != null ? anchor : transform;

    /// <summary>이 대상의 외곽선 채널을 반환한다. OutlinePart가 요청·해제에 사용한다.</summary>
    public StatusOutlineChannel OutlineChannel => outlineChannel;

    private void Awake()
    {
        controller = GetComponent<StatusEffectController>();

        if (anchor == null)
        {
            anchor = transform;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = CollectRenderers();
        }

        outlineChannel = new StatusOutlineChannel(targetRenderers);
    }

    private void OnEnable()
    {
        if (controller == null)
        {
            return;
        }

        controller.OnEffectRegistered += HandleRegistered;
        controller.OnEffectReapplied += HandleReapplied;
        controller.OnEffectTicked += HandleTicked;
        controller.OnEffectRemoved += HandleRemoved;

        // 비활성화 중에도 남아 있던 상태의 지속 연출(외곽선 등)만 다시 켠다.
        IReadOnlyList<StatusEffect> activeEffects = controller.ActiveStatusEffects;
        if (activeEffects == null)
        {
            return;
        }

        for (int i = 0; i < activeEffects.Count; i++)
        {
            RestoreParts(activeEffects[i]);
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

        outlineChannel?.Clear();
    }

    private void OnDestroy()
    {
        outlineChannel?.Dispose();
    }

    /// <summary>상태가 걸리면 파츠의 시작 연출을 실행한다.</summary>
    private void HandleRegistered(StatusEffect effect)
    {
        IReadOnlyList<StatusVisualPart> parts = GetParts(effect);
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
            {
                parts[i].OnStart(this, effect);
            }
        }
    }

    /// <summary>재활성화 시 남아 있던 상태의 지속 연출을 다시 켠다.</summary>
    private void RestoreParts(StatusEffect effect)
    {
        IReadOnlyList<StatusVisualPart> parts = GetParts(effect);
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
            {
                parts[i].OnRestore(this, effect);
            }
        }
    }

    /// <summary>상태가 중첩되면 파츠의 중첩 연출을 실행한다.</summary>
    private void HandleReapplied(StatusEffect effect)
    {
        IReadOnlyList<StatusVisualPart> parts = GetParts(effect);
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
            {
                parts[i].OnReapply(this, effect);
            }
        }
    }

    /// <summary>발동 주기마다 파츠의 틱 연출을 실행한다.</summary>
    private void HandleTicked(StatusEffect effect)
    {
        IReadOnlyList<StatusVisualPart> parts = GetParts(effect);
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
            {
                parts[i].OnTick(this, effect);
            }
        }
    }

    /// <summary>상태가 끝나면 파츠의 종료 연출을 실행한다.</summary>
    private void HandleRemoved(StatusEffect effect)
    {
        IReadOnlyList<StatusVisualPart> parts = GetParts(effect);
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null)
            {
                parts[i].OnEnd(this, effect);
            }
        }
    }

    /// <summary>상태의 연출 파츠 목록을 반환한다. 데이터가 없거나 파츠가 비어 있으면 null이다.</summary>
    private static IReadOnlyList<StatusVisualPart> GetParts(StatusEffect effect)
    {
        StatusEffectData data = effect?.Data;
        if (data == null || data.VisualParts == null || data.VisualParts.Count == 0)
        {
            return null;
        }

        return data.VisualParts;
    }

    /// <summary>자식에서 외곽선을 붙일 메시 렌더러를 수집한다. 파티클 등 다른 렌더러는 제외한다.</summary>
    private Renderer[] CollectRenderers()
    {
        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        var meshRenderers = new List<Renderer>(found.Length);

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] is MeshRenderer || found[i] is SkinnedMeshRenderer)
            {
                meshRenderers.Add(found[i]);
            }
        }

#if UNITY_EDITOR
        if (meshRenderers.Count == 0)
        {
            Debug.LogWarning("[StatusEffectVisualizer] 외곽선을 표시할 렌더러를 찾지 못했습니다.", this);
        }
#endif

        return meshRenderers.ToArray();
    }
}
