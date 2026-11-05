using System.Collections.Generic;
using UnityEngine;

// 상태이상 패널 View 인터페이스. Model(상태이상 목록) 확정 시 시그니처를 조정한다.
public interface IStatusPanelView
{
    public void AddStatusIcon(StatusEffectType type, Sprite icon, int stackCount, float remainingDuration, float totalDuration);
    public void UpdateStatusIcon(StatusEffectType type, int stackCount, float remainingDuration, float totalDuration);
    public void RemoveStatusIcon(StatusEffectType type);
    public void ClearStatusIcons();
}
