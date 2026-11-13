using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HP·스태미나 바를 그리는 View. Model(PlayerStats)은 StatPanelPresenter가 연결한다.
public class StatPanelView : MonoBehaviour, IStatPanelView
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text staminaText;

    // HP 변경 시 바와 텍스트 갱신
    public void SetHp(int currentHp, int maxHp)
    {
        hpBar.maxValue = Mathf.Max(1, maxHp);
        hpBar.value = currentHp;
        hpText.text = $"{currentHp} / {maxHp}";
    }

    // 스태미나 변경 시 바와 텍스트 갱신
    public void SetStamina(int currentStamina, int maxStamina)
    {
        staminaBar.maxValue = Mathf.Max(1, maxStamina);
        staminaBar.value = currentStamina;
        staminaText.text = $"{currentStamina} / {maxStamina}";
    }
}
