using Photon.Pun;
using UnityEngine;

// 내 플레이어의 지역이 바뀌면 해당 지역 BGM으로 바꾼다
// 플레이어는 나중에 스폰되므로 WorldManager.OnSpawnedEvent로 내 플레이어의 트래커를 찾아 구독한다
public class RegionBgmController : MonoBehaviour
{
    private void Start()
    {
        WorldManager.Instance.OnSpawnedEvent += OnWorldObjSpawned;
        // 이 컴포넌트보다 플레이어가 먼저 생성된 경우
        foreach (var obj in WorldManager.Instance.Objects.GetList()) OnWorldObjSpawned(obj);
    }

    private void OnDestroy()
    {
        if (WorldManager.Instance != null) WorldManager.Instance.OnSpawnedEvent -= OnWorldObjSpawned;
        Bind(null);
    }

    private void OnWorldObjSpawned(IWorldObj obj)
    {
        // 원격 플레이어의 지역 변화로 내 BGM이 바뀌면 안 되므로 내 플레이어만
        if (obj is not IPlayerObj p || p.Owner != PhotonNetwork.LocalPlayer.ActorNumber) return;
        // GameManager가 먼저 붙이지만, 실행 순서가 달라도 동작하도록 없으면 추가
        if (!obj.gameObject.TryGetComponent(out PlayerRegionTracker tracker))
            tracker = obj.gameObject.AddComponent<PlayerRegionTracker>();
        Bind(tracker);
    }

    private void Bind(PlayerRegionTracker tracker)
    {
        if (m_tracker == tracker) return;
        if (m_tracker != null) m_tracker.OnRegionChanged -= OnRegionChanged;
        m_tracker = tracker;
        if (m_tracker == null) return;
        m_tracker.OnRegionChanged += OnRegionChanged;
        // 이미 지역 안에 있는 상태로 연결된 경우 바로 반영
        if (m_tracker.CurrentRegion != RegionType.None) ChangeBgm(m_tracker.CurrentRegion);
    }

    private void OnRegionChanged(RegionType prev, RegionType next)
    {
        // 볼륨 사이 틈이나 디스폰(OnDisable)으로 None이 되면 현재 BGM 유지
        if (next == RegionType.None) return;
        ChangeBgm(next);
    }

    private void ChangeBgm(RegionType region)
    {
        // 같은 지역에 다시 들어와도 곡이 처음부터 다시 재생되지 않게 막음
        if (region == m_playingRegion) return;
        m_playingRegion = region;
        // TODO 지역별 클립 조회와 실제 재생은 다음 단계에서 구현
        Debug.Log($"[RegionBgmController] BGM 변경 → {region}");
    }

    private PlayerRegionTracker m_tracker;
    private RegionType m_playingRegion = RegionType.None;
}
