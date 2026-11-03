using UnityEngine;

public enum EffectCategory
{
    None,
    Buff,
    Debuff
}

public enum StatusEffectType
{
    None,
    Burn,               // 화상
    // Chill,              // 냉기
    // Poison,             // 독
    // Slime,              // 점액
    // Wet,                // 젖음
    // Stun,               // 기절
    // Freeze,             // 빙결
    // Bind,               // 구속
    // Vulnerable,         // 취약
    // Corrosion,          // 부식
    // Fear,               // 공포

    // CurseGreed,         // 저주 - 탐욕
    // CurseHeat,          // 저주 - 열기
    // CurseCold,          // 저주 - 혹한
    // CurseDisease,       // 저주 - 질병
    // CurseErosion,       // 저주 - 침식

    // Explosion,          // 폭파
    // Haste,              // 헤이스트
    // Boost,              // 부스트

    // ResistHeat,         // 저항 - 열기
    // ResistCold,         // 저항 - 혹한
    // ResistDisease,      // 저항 - 질병

    // Petrify,            // 석화
    // Invincible,         // 무적
}
