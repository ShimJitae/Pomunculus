using UnityEngine;
using UnityEngine.UI;

public class DayUI : MonoBehaviour
{
    public void Update()
    {
        var data = WorldManager.Instance.CurWorldData;
        if (data == null) return; // 월드 시작 전
        // 오늘 남은 시간 비율 (하루 시작 1 -> 하루 끝 0)
        m_clockUI.fillAmount = 1f - Mathf.Clamp01(data.Time / WorldManager.DayLength);
    }
    [SerializeField]private Image m_clockUI;
}