using UnityEngine;

/// <summary>
/// 로컬 플레이어에 붙어 OccluderDetector의 검사 기준이 된다.
/// 다른 플레이어에게 붙은 것은 Start에서 스스로 제거된다
/// </summary>
public sealed class OccluderTarget : MonoBehaviour
{
    [Tooltip("절두체 중심. 발보다 몸통 높이가 자연스럽다")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1f, 0f);

    // 첫 등록이 끝났는지. 로컬 여부는 Start에서야 알 수 있어 첫 등록은 Start에서 한다
    private bool hasStarted;

    /// <summary>절두체 중심이 될 월드 위치</summary>
    public Vector3 CenterPosition => transform.position + centerOffset;

    // Owner는 생성 직후(방장: Spawn의 onInit, 그 외: LoadData)에 정해지므로 Awake·OnEnable에서는 아직 0이다.
    // Start는 그 뒤에 실행되므로 여기서 확인한다. 플레이어를 풀링으로 재사용하게 되면 이 전제가 깨진다
    private void Start()
    {
        PlayerFSM player = GetComponent<PlayerFSM>();
        if (player == null || !player.IsMine)
        {
            Destroy(this);
            return;
        }

        hasStarted = true;
        OccluderDetector.SetTarget(this);
    }

    private void OnEnable()
    {
        // 첫 활성화는 Start가 맡는다. 이후 재활성화만 여기서 등록한다
        if (hasStarted)
        {
            OccluderDetector.SetTarget(this);
        }
    }

    private void OnDisable()
    {
        OccluderDetector.ClearTarget(this);
    }
}