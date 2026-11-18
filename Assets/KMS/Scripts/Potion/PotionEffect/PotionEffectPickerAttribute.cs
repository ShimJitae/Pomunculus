using UnityEngine;

// 이 속성을 붙인 [SerializeReference] PotionEffect 리스트의 각 항목에 효과 종류 선택 버튼을 그린다
public class PotionEffectPickerAttribute : PropertyAttribute
{
    public readonly bool AllowZone;   // false면 메뉴에서 장판 효과를 뺀다 (장판 안에 장판 방지)

    public PotionEffectPickerAttribute(bool allowZone = true)
    {
        AllowZone = allowZone;
    }
}