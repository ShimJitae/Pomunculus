using System.Collections.Generic;
using UnityEngine;

/// <summary>석화 연출이 재생되는 동안 모델 표면을 돌로 바꾸고 원래 슬롯을 복구한다.</summary>
[DisallowMultipleComponent]
public sealed class StatusEffectPetrifySurface : MonoBehaviour
{
    [SerializeField] private Material stoneMaterial;

    // 저장 복원으로 같은 대상의 석화가 겹쳐도 마지막 연출만 원본을 복구한다.
    private static readonly Dictionary<Renderer, (Material[] originals, int users)> activeSurfaces = new();
    private readonly List<Renderer> appliedRenderers = new();
    private GameObject target;

    internal void Initialize(GameObject feedbackTarget) => target = feedbackTarget;

    // 유지 Player의 Feel Unity Events에서 호출한다.
    public void Apply()
    {
        if (target == null || stoneMaterial == null || appliedRenderers.Count != 0)
        {
            return;
        }

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer is not (MeshRenderer or SkinnedMeshRenderer) ||
                renderer.GetComponentInParent<StatusEffectFeedback>() != null)
            {
                continue;
            }

            if (activeSurfaces.TryGetValue(renderer, out var surface))
            {
                activeSurfaces[renderer] = (surface.originals, surface.users + 1);
                appliedRenderers.Add(renderer);
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0)
            {
                continue;
            }

            activeSurfaces.Add(renderer, (materials, 1));
            appliedRenderers.Add(renderer);
            Material[] stoneSlots = new Material[materials.Length];
            System.Array.Fill(stoneSlots, stoneMaterial);
            renderer.sharedMaterials = stoneSlots;
        }
    }

    public void Restore()
    {
        foreach (Renderer renderer in appliedRenderers)
        {
            var surface = activeSurfaces[renderer];
            if (surface.users > 1)
            {
                activeSurfaces[renderer] = (surface.originals, surface.users - 1);
                continue;
            }

            activeSurfaces.Remove(renderer);
            if (renderer != null)
            {
                renderer.sharedMaterials = surface.originals;
            }
        }
        appliedRenderers.Clear();
    }

    private void OnDisable() => Restore();
}
