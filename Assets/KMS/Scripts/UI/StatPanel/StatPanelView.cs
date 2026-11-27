using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HP·스태미나·보호막 바를 그리는 View. Model(PlayerStats)은 StatPanelPresenter가 연결한다.
public class StatPanelView : MonoBehaviour, IStatPanelView
{
    [SerializeField] private MMProgressBar hpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TMP_Text staminaText;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;   // HP Fill 뒤에 깔리는 Filled(Horizontal) 이미지
    [SerializeField] private Color shieldTextColor = new Color(1f, 0.84f, 0.3f);

    private int currentHp;
    private int maxHp;
    private int currentShield;
    private bool hasHp;   // 첫 값은 연출 없이 바로 채운다

    // HP 변경 시 바와 텍스트 갱신. HP 값이 실제로 바뀔 때만 닳음/참 연출을 재생한다
    public void SetHp(int currentHp, int maxHp)
    {
        bool hpChanged = hasHp && currentHp != this.currentHp;
        this.currentHp = currentHp;
        this.maxHp = maxHp;
        hasHp = true;
        RefreshHpBar(hpChanged);
    }

    // 보호막 변경은 바 비율만 바뀌므로 연출 없이 즉시 반영한다
    public void SetShield(int currentShield)
    {
        this.currentShield = Mathf.Max(0, currentShield);
        RefreshHpBar(false);
    }

    // 스태미나 변경 시 바와 텍스트 갱신
    public void SetStamina(int currentStamina, int maxStamina)
    {
        staminaBar.maxValue = Mathf.Max(1, maxStamina);
        staminaBar.value = currentStamina;
        staminaText.text = $"{currentStamina} / {maxStamina}";
    }

    /// <summary>
    /// HP+보호막을 한 바에 그린다. HP+보호막이 최대 HP를 넘으면 그 합을 바 전체 길이로 삼아
    /// HP 구간 뒤에 보호막 구간이 이어 붙도록 한다.
    /// </summary>
    private void RefreshHpBar(bool animate)
    {
        int barTotal = Mathf.Max(1, maxHp, currentHp + currentShield);

        float hpRatio = (float)currentHp / barTotal;
        if (animate)
        {
            hpBar.UpdateBar01(hpRatio);
        }
        else
        {
            hpBar.SetBar01(hpRatio);
        }

        if (shieldFill != null)
        {
            shieldFill.fillAmount = currentShield > 0 ? (float)(currentHp + currentShield) / barTotal : 0f;
        }

        string hpLabel = $"{currentHp} / {maxHp}";
        if (currentShield > 0)
        {
            string shieldColorHex = ColorUtility.ToHtmlStringRGB(shieldTextColor);
            hpLabel = $"<color=#{shieldColorHex}>+{currentShield}</color> {hpLabel}";
        }

        hpText.text = hpLabel;
    }
}
