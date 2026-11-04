using System;
using UnityEngine;

// 플레이어 상호작용. 주변 대상을 감지만 하고, 입력(G)은 PlayerInputReader → FSM 상태가 Interact()를 호출한다
public class PlayerInteractor : MonoBehaviour
{
    [Header("감지")]
    public float radius = 2f;
    [Tooltip("상호작용 오브젝트만 들어 있는 레이어를 지정")]
    public LayerMask interactableLayer;
    public float scanInterval = 0.1f;

    // 현재 대상이 바뀔 때 호출 (null = 주변에 없음). 안내 UI, 하이라이트 연결용
    public event Action<IInteractable> OnTargetChanged;

    // 현재 상호작용 가능한 대상. 없거나 파괴됐으면 null
    public IInteractable Current => clientComponent != null ? client : null;

    readonly Collider[] buffer = new Collider[16];
    IInteractable client;
    Component clientComponent;   // 파괴 여부를 Unity 방식으로 확인하기 위해 함께 보관
    float nextScanAt;

    void Update()
    {
        if (Time.time >= nextScanAt)
        {
            nextScanAt = Time.time + scanInterval;
            Scan();
        }
    }

    public void Interact()
    {
        Scan();   // 마지막 검사 이후 움직였을 수 있으니 누르는 순간 다시 확인
        Current?.Activate();
    }

    void Scan()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, buffer, interactableLayer, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        Component bestComponent = null;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            // Collider가 자식에 있어도 찾을 수 있도록 부모까지 검색
            var interactable = buffer[i].GetComponentInParent<IInteractable>();
            if (interactable is not Component component) continue;

            float sqrDist = (component.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = interactable;
                bestComponent = component;
            }
        }

        if (ReferenceEquals(bestComponent, clientComponent)) return;

        client = best;
        clientComponent = bestComponent;
        OnTargetChanged?.Invoke(Current);
    }

    void OnDisable()
    {
        // 플레이어가 꺼지면(사망 등) 안내 UI도 사라지도록
        if (ReferenceEquals(clientComponent, null)) return;
        client = null;
        clientComponent = null;
        OnTargetChanged?.Invoke(null);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}