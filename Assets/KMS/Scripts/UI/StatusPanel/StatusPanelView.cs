using System.Collections.Generic;
using UnityEngine;

// 상태이상 아이콘 목록을 그리는 View. 아이콘은 StatusEffectIcon 프리팹을 생성해 붙인다.
public class StatusPanelView : MonoBehaviour, IStatusPanelView
{
    [SerializeField] private Transform statusSlot;          // 아이콘이 붙을 부모 (Layout Group)
    [SerializeField] private StatusEffectIconView statusIconPrefab;

    private readonly Dictionary<StatusEffectType, StatusEffectIconView> statusIcons = new();

    // 상태이상이 걸렸을 때 아이콘 추가
    public void AddStatusIcon(StatusEffectType type, Sprite icon, EffectCategory category, int stackCount, float remainingDuration, float totalDuration)
    {
        if (statusIcons.TryGetValue(type, out StatusEffectIconView iconView))
            iconView.Refresh(stackCount, remainingDuration, totalDuration);
        else
        {
            iconView = Instantiate(statusIconPrefab, statusSlot);
            statusIcons.Add(type, iconView);
            iconView.Setup(icon, category, stackCount, remainingDuration, totalDuration);
        }
    }

    // 상태이상 갱신
    public void UpdateStatusIcon(StatusEffectType type, int stackCount, float remainingDuration, float totalDuration)
    {
        if (statusIcons.TryGetValue(type, out StatusEffectIconView iconView))
            iconView.Refresh(stackCount, remainingDuration, totalDuration);
    }

    // 상태이상 만료 시 아이콘 삭제
    public void RemoveStatusIcon(StatusEffectType type)
    {
        if (statusIcons.TryGetValue(type, out StatusEffectIconView iconView))
        {
            Destroy(iconView.gameObject);
            statusIcons.Remove(type);
        }
    }

    // 모든 상태이상 아이콘 제거 (사망·부활 등)
    public void ClearStatusIcons()
    {
        foreach (StatusEffectIconView iconView in statusIcons.Values)
        {
            if (iconView != null)
                Destroy(iconView.gameObject);
        }

        statusIcons.Clear();
    }
}
