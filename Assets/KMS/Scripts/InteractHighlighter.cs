using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerInteractor의 현재 대상에 아웃라인 머티리얼을 덧붙여 테두리를 표시한다.
/// 플레이어(PlayerInteractor가 있는 오브젝트)에 함께 붙여서 사용.
/// </summary>
[RequireComponent(typeof(PlayerInteractor))]
public class InteractHighlighter : MonoBehaviour
{
    [Tooltip("Custom/InteractOutline 셰이더로 만든 머티리얼")]
    public Material outlineMaterial;

    PlayerInteractor interactor;
    readonly List<Renderer> highlighted = new List<Renderer>();
    readonly List<Material> temp = new List<Material>();

    void Awake() => interactor = GetComponent<PlayerInteractor>();

    void OnEnable() => interactor.OnTargetChanged += HandleTargetChanged;

    void OnDisable()
    {
        interactor.OnTargetChanged -= HandleTargetChanged;
        ClearHighlight();
    }

    void HandleTargetChanged(IInteractable target)
    {
        ClearHighlight();

        if (outlineMaterial == null) return;
        if (target is not Component component || component == null) return;   // 대상 없음 또는 파괴됨

        // 대상과 자식의 모든 메시에 적용 (모델이 여러 조각이어도 전체 테두리)
        foreach (Renderer r in component.GetComponentsInChildren<Renderer>())
        {
            if (r is not MeshRenderer && r is not SkinnedMeshRenderer) continue;   // 파티클 등 제외

            // sharedMaterials를 쓰면 머티리얼 복제본이 생기지 않는다
            r.GetSharedMaterials(temp);
            if (temp.Contains(outlineMaterial)) continue;

            temp.Add(outlineMaterial);
            r.sharedMaterials = temp.ToArray();
            highlighted.Add(r);
        }
    }

    void ClearHighlight()
    {
        foreach (Renderer r in highlighted)
        {
            if (r == null) continue;   // 하이라이트 중에 파괴된 대상

            // 저장해둔 배열로 되돌리지 않고 아웃라인만 빼낸다
            // (하이라이트 중에 대상이 스스로 머티리얼을 바꿨을 수도 있으므로)
            r.GetSharedMaterials(temp);
            if (temp.Remove(outlineMaterial))
                r.sharedMaterials = temp.ToArray();
        }
        highlighted.Clear();
    }
}