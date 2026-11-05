using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 대상의 외곽선 표시를 관리한다. 상태별 외곽선 요청 중 우선순위가 가장 높은 하나만 적용한다.
/// 대상마다 외곽선 머티리얼 인스턴스를 하나 만들어 렌더러의 sharedMaterials 끝에 붙이거나 뗀다.
/// StatusEffectVisualizer가 생성·정리하며, 컴포넌트가 아닌 일반 클래스이다.
/// </summary>
public sealed class StatusOutlineChannel
{
    private static readonly int outlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int outlineWidthId = Shader.PropertyToID("_OutlineWidth");

    private readonly Renderer[] renderers;
    private readonly Dictionary<StatusEffect, OutlinePart> requests = new Dictionary<StatusEffect, OutlinePart>();
    private readonly List<Material> materialBuffer = new List<Material>();
    private Material outlineMaterial;
    private OutlinePart appliedPart;
    private bool isAttached;

    /// <summary>외곽선을 붙일 렌더러 목록으로 생성한다.</summary>
    public StatusOutlineChannel(Renderer[] renderers)
    {
        this.renderers = renderers;
    }

    /// <summary>상태의 외곽선을 요청한다. 같은 상태가 다시 요청하면 설정을 덮어쓴다.</summary>
    public void Request(StatusEffect effect, OutlinePart part)
    {
        if (effect == null || part == null)
        {
            return;
        }

        requests[effect] = part;
        Refresh();
    }

    /// <summary>상태의 외곽선 요청을 해제한다.</summary>
    public void Release(StatusEffect effect)
    {
        if (effect != null && requests.Remove(effect))
        {
            Refresh();
        }
    }

    /// <summary>모든 요청을 지우고 외곽선을 뗀다. 머티리얼은 다시 쓸 수 있게 남긴다.</summary>
    public void Clear()
    {
        requests.Clear();
        appliedPart = null;
        Detach();
    }

    /// <summary>외곽선을 떼고 생성한 머티리얼을 파괴한다. 대상이 파괴될 때 호출한다.</summary>
    public void Dispose()
    {
        Clear();

        if (outlineMaterial != null)
        {
            Object.Destroy(outlineMaterial);
            outlineMaterial = null;
        }
    }

    /// <summary>남은 요청 중 우선순위가 가장 높은 외곽선을 적용한다. 요청이 없으면 외곽선을 뗀다.</summary>
    private void Refresh()
    {
        OutlinePart best = null;
        foreach (OutlinePart part in requests.Values)
        {
            // 셰이더가 없는 설정 오류 파츠는 후보에서 빼서, 다른 정상 요청이 가려지지 않게 한다.
            if (part.OutlineShader == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[StatusOutlineChannel] OutlinePart에 외곽선 셰이더가 지정되지 않아 무시합니다.", part);
#endif
                continue;
            }

            // 우선순위가 같으면 현재 표시 중인 색을 유지해 깜빡임을 막는다.
            if (best == null || part.Priority > best.Priority || (part.Priority == best.Priority && part == appliedPart))
            {
                best = part;
            }
        }

        if (best == null)
        {
            appliedPart = null;
            Detach();
            return;
        }

        PrepareMaterial(best.OutlineShader);
        outlineMaterial.SetColor(outlineColorId, best.OutlineColor);
        outlineMaterial.SetFloat(outlineWidthId, best.OutlineWidth);
        appliedPart = best;
        Attach();
    }

    /// <summary>외곽선 머티리얼을 처음 사용할 때 만들고, 셰이더가 다르면 교체한다.</summary>
    private void PrepareMaterial(Shader shader)
    {
        if (outlineMaterial == null)
        {
            outlineMaterial = new Material(shader);
            outlineMaterial.name = "StatusOutline (Runtime)";
        }
        else if (outlineMaterial.shader != shader)
        {
            outlineMaterial.shader = shader;
        }
    }

    /// <summary>각 렌더러의 머티리얼 목록 끝에 외곽선 머티리얼을 붙인다.</summary>
    private void Attach()
    {
        if (isAttached)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
            {
                continue;
            }

            // sharedMaterials를 쓰면 원본 머티리얼의 복제본이 생기지 않는다.
            target.GetSharedMaterials(materialBuffer);
            if (!materialBuffer.Contains(outlineMaterial))
            {
                materialBuffer.Add(outlineMaterial);
                target.sharedMaterials = materialBuffer.ToArray();
            }
        }

        isAttached = true;
    }

    /// <summary>각 렌더러에서 이 채널의 외곽선 머티리얼만 뗀다. 다른 기능이 붙인 머티리얼은 건드리지 않는다.</summary>
    private void Detach()
    {
        if (!isAttached)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null)
            {
                continue;
            }

            target.GetSharedMaterials(materialBuffer);
            if (materialBuffer.Remove(outlineMaterial))
            {
                target.sharedMaterials = materialBuffer.ToArray();
            }
        }

        isAttached = false;
    }
}
