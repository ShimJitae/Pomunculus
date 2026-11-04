using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour, IPlayerHUDView
{
    [Header("Stat Panel")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text staminaText;

    [Header("Status Panel")]
    [SerializeField] private GameObject statusSlot;
    [SerializeField] private List<Image> statusIcon;

    [Header("Potion Slot")]
    [SerializeField] private Image[] potionIcon;

    // HP 변경시
    public void SetHp(int currentHp, int maxHp)
    {
        hpBar.maxValue = Mathf.Max(1, maxHp);
        hpBar.value = currentHp;
        hpText.text = $"{currentHp} / {maxHp}";
    }

    // 스태미나 변경 시
    public void SetStamina(int currentStamina, int maxStamina)
    {
        staminaBar.maxValue = Mathf.Max(1, maxStamina);
        staminaBar.value = currentStamina;
        staminaText.text = $"{currentStamina} / {maxStamina}";
    }

    // 상태이상 변경 시
    public void AddStatusIcon()
    {

    }

    // 포션 장착 시(인벤토리에서 슬롯으로)
    public void PotionEquip()
    {

    }

    // 포션 변경 시(마우스 휠 ~ 지정키)
    public void PotionSwap()
    {

    }
    
}
