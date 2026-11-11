using UnityEngine;

// 상태의 분류. SO에 정수값으로 저장되므로 값을 명시한다.
public enum EffectCategory
{
    None = 0,
    Buff = 1,       // 버프
    Debuff = 2,     // 디버프
    Curse = 3       // 저주
}

// SO에는 정수값으로 저장되므로 일부만 주석을 해제해도 값이 밀리지 않도록 명시한다.
public enum StatusEffectType
{
    None = 0,
    Burn = 1,               // 화상
    Chill = 2,              // 냉기
    Poison = 3,             // 독
    Slime = 4,              // 점액
    Wet = 5,                // 젖음
    Stun = 6,               // 기절
    Freeze = 7,             // 빙결
    Bind = 8,               // 구속
    Vulnerable = 9,         // 취약
    // Corrosion = 10,      // 부식
    // Fear = 11,           // 공포

    // CurseGreed = 12,     // 저주 - 탐욕
    CurseHeat = 13,         // 저주 - 열기
    CurseCold = 14,         // 저주 - 혹한
    CurseDisease = 15,      // 저주 - 질병
    // CurseErosion = 16,   // 저주 - 침식

    // Explosion = 17,      // 폭파
    Haste = 18,             // 헤이스트
    // Boost = 19,          // 부스트

    ResistHeat = 20,        // 저항 - 열기
    ResistCold = 21,        // 저항 - 혹한
    ResistDisease = 22,     // 저항 - 질병

    Petrify = 23,           // 석화
    Invincible = 24,        // 무적
}
