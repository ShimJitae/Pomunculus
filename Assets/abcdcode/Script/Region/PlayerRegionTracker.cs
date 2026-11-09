using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 프리팹에 붙여서 현재 지역을 추적한다
public class PlayerRegionTracker : MonoBehaviour
{
    public RegionType CurrentRegion { get; private set; } = RegionType.None;

    // (이전 지역, 새 지역)
    public event Action<RegionType, RegionType> OnRegionChanged;

    private void Awake()
    {
        // 트리거 이벤트는 둘 중 한쪽에 Rigidbody가 있어야 발생한다.
        // 플레이어는 NavMeshAgent로 움직이므로 키네마틱으로 붙여 물리 영향은 받지 않게 한다
        if (!TryGetComponent(out Rigidbody rb)) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnDisable()
    {
        // 비활성화(풀 반납 등) 시에는 OnTriggerExit가 오지 않으므로 직접 비운다.
        // 다시 활성화되면 겹쳐 있는 볼륨에서 OnTriggerEnter가 새로 들어온다
        m_volumes.Clear();
        Refresh();
    }

    public void Enter(RegionVolume volume)
    {
        m_volumes.Remove(volume);
        m_volumes.Add(volume);
        Refresh();
    }

    public void Exit(RegionVolume volume)
    {
        if (m_volumes.Remove(volume)) Refresh();
    }

    private void Refresh()
    {
        m_volumes.RemoveAll(v => v == null || !v.isActiveAndEnabled);

        RegionType next = RegionType.None;
        int best = int.MinValue;
        // 뒤에 있을수록 나중에 들어간 볼륨이므로 >= 로 덮어쓴다
        foreach (var volume in m_volumes)
        {
            if (volume.Priority < best) continue;
            best = volume.Priority;
            next = volume.Region;
        }

        if (next == CurrentRegion) return;
        RegionType prev = CurrentRegion;
        CurrentRegion = next;
        OnRegionChanged?.Invoke(prev, next);
    }

    private readonly List<RegionVolume> m_volumes = new List<RegionVolume>();
}
