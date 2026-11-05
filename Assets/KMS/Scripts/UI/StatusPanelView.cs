using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상태이상 아이콘 목록을 그리는 View. 아이콘은 StatusEffectIcon 프리팹을 생성해 붙인다.
public class StatusPanelView : MonoBehaviour, IStatusPanelView
{
    private const string IconChildName = "Icon";

    [SerializeField] private Transform statusSlot;          // 아이콘이 붙을 부모 (Layout Group)
    [SerializeField] private GameObject statusIconPrefab;   // StatusEffectIcon.prefab

    private readonly List<GameObject> statusIcons = new();

    // 상태이상이 걸렸을 때 아이콘 추가
    public void AddStatusIcon(StatusEffect icon)
    {
        GameObject statusIcon = Instantiate(statusIconPrefab, statusSlot);
        Transform iconChild = statusIcon.transform.Find(IconChildName);
        if (iconChild != null && iconChild.TryGetComponent(out Image iconImage))
        {
            //iconImage.sprite = icon;
        }

        statusIcons.Add(statusIcon);
    }

    public void RemoveStatusIcon(int index)
    {
        if (index < 0 || index >= statusIcons.Count) return;

        Destroy(statusIcons[index]);
        statusIcons.RemoveAt(index);
    }

    public void AddStatusIcon(StatusEffectType type, Sprite icon, int stackCount, float remainingDuration, float totalDuration)
    {
        
    }

    public void UpdateStatusIcon(StatusEffectType type, int stackCount, float remainingDuration, float totalDuration)
    {
        
    }

    public void RemoveStatusIcon(StatusEffectType type)
    {
        
    }

    // 모든 상태이상 아이콘 제거 (사망·부활 등)
    public void ClearStatusIcons()
    {
        for (int i = 0; i < statusIcons.Count; i++)
        {
            Destroy(statusIcons[i]);
        }

        statusIcons.Clear();
    }
}
