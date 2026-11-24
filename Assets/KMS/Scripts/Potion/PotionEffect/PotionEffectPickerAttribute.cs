using UnityEngine;

// 이 속성을 붙인 [SerializeReference] PotionEffect 리스트의 각 항목에 효과 종류 선택 버튼을 그린다
public class PotionEffectPickerAttribute : PropertyAttribute
{
    public readonly bool AllowSpawn;   // false면 메뉴에서 생성 효과(장판·오브젝트·소환)를 뺀다 (장판 틱마다 생성 방지)

    public PotionEffectPickerAttribute(bool allowSpawn = true)
    {
        AllowSpawn = allowSpawn;
    }
}
