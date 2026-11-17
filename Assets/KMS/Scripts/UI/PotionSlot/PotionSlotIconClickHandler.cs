using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 포션 아이콘 우클릭을 감지해 View에 슬롯 번호를 전달한다
public class PotionSlotIconClickHandler : MonoBehaviour, IPointerClickHandler, IRightClickConsumer
{
    private int slotIndex;
    private Action<int> onRightClicked;

    // View가 슬롯 번호와 콜백을 넘겨 초기화한다
    public void Initialize(int slotIndex, Action<int> onRightClicked)
    {
        this.slotIndex = slotIndex;
        this.onRightClicked = onRightClicked;
    }

    // 우클릭일 때만 콜백을 호출한다
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        onRightClicked?.Invoke(slotIndex);
    }
}