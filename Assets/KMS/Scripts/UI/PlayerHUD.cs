using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Stat Panel")]
    [SerializeField] private Slider HPBar;
    [SerializeField] private Slider StaminaBar;

    [Header("Status Panel")]
    [SerializeField] private GameObject StatusSlot;
    [SerializeField] private List<Image> StatusIcon;

    [Header("Potion Slot")]
    [SerializeField] private Image[] PotionIcon;

    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void Start()
    {

    }

    // 체력 변경 시
    private void OnHpHandler(int hp)
    {
        HPBar.value += hp;
    }

    // 스테미나 변경 시
    private void OnStaminaHandler(int stamina)
    {
        StaminaBar.value += stamina;
    }


    // 상태이상 변경 시
    private void OnStatusEffectHandler()
    {

    }

    // 포션 장착 시(인벤토리에서)
    private void OnPotionEquipHandler()
    {

    }

    // 포션 변경 시(마우스 휠 ~ 지정키)
    private void OnPotionSwapHandler()
    {

    }
    
}
