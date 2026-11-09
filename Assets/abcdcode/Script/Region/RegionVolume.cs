using UnityEngine;

// 플레이어가 들어오고 나가는 것을 PlayerRegionTracker에 알려주는 트리거 볼륨
// World 프리팹 바깥(씬 루트의 Regions 아래)에 배치한다
[RequireComponent(typeof(Collider))]
public class RegionVolume : MonoBehaviour
{
    public RegionType Region => m_region;
    public int Priority => m_priority;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var tracker = other.GetComponentInParent<PlayerRegionTracker>();
        if (tracker != null) tracker.Enter(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var tracker = other.GetComponentInParent<PlayerRegionTracker>();
        if (tracker != null) tracker.Exit(this);
    }

    private void OnDrawGizmos()
    {
        if (!TryGetComponent(out BoxCollider box)) return;

        Color color = m_region switch
        {
            RegionType.Forest => new Color(0.2f, 0.8f, 0.2f),
            RegionType.Volcano => new Color(1f, 0.35f, 0.1f),
            _ => Color.gray,
        };
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = color;
        Gizmos.DrawWireCube(box.center, box.size);
    }

    [SerializeField] private RegionType m_region;
    // 볼륨이 겹칠 때 높은 값이 우선. 같으면 나중에 들어간 볼륨이 우선
    [SerializeField] private int m_priority;
}
