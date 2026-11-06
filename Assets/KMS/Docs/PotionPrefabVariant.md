# 포션 프리팹 베리언트 가이드

> 포션 프리팹을 **던지기용(Thrown)** 과 **바닥 아이템용(Pickup)** 으로 나누되, 병 모양은 한 곳(Base)에서만 관리하는 방법을 정리한 문서입니다.
> - 예시: 화염 포션 (`Assets/KMS/Prefabs/Potion/FirePotion/`)
> - 기준: 2026-11-06

---

## 1. 프리팹 베리언트란

**다른 프리팹(부모)을 물려받는 프리팹**입니다.

- 부모의 모델·머티리얼·콜라이더 등 모든 내용을 그대로 가져온다.
- 베리언트 파일에는 **부모와 달라진 부분(오버라이드)만** 저장된다.
- 부모를 고치면 모든 베리언트에 자동으로 반영된다. 단, 베리언트에서 직접 덮어쓴 값은 베리언트 쪽 값이 유지된다.
- 한 베리언트에서 고친 내용은 다른 베리언트에 영향을 주지 않는다.

| 확인 방법 | 위치 |
|---|---|
| 베리언트인지 | Project 창 아이콘에 **화살표**가 붙어 있음 |
| 부모가 무엇인지 | 인스펙터 상단 `Variant of …` 표시 |
| 부모와 다른 부분 | 인스펙터 상단 **Overrides** 드롭다운 (덮어쓴 값은 굵은 글씨 + 왼쪽 파란 줄) |

---

## 2. 왜 포션에 베리언트를 쓰나

### 한 프리팹에 다 넣으면 생기는 문제
병 하나에 `ThrownProjectile`(던지기)과 `PotionPickup`(줍기)을 같이 붙이면 아래 문제가 생긴다.

| 문제 | 원인 |
|---|---|
| 바닥에 놓인 병이 무언가에 닿자마자 사라진다 | Unity는 **꺼진(disabled) 컴포넌트에도 `OnCollisionEnter`를 호출**한다. `ThrownProjectile`을 꺼 둬도 `Impact()` → `Destroy`가 실행된다. |
| 던지자마자 던진 사람이 다시 줍는다 | 손 위치는 `ItemPickupController` 줍기 반경(1.5m) 안이다. 날아가는 동안에도 `PotionPickup`과 줍기 레이어가 남아 있다. |
| 떨어뜨린 병이 공중에 떠 있다 | `ThrownProjectile.Awake`가 `rb.useGravity = false`로 끈다. 중력은 `Launch()`를 호출해야만 적용된다. |

이 문제를 피하려면 `isLaunched` 플래그, 레이어 전환, 중력 설정 이동 같은 **상태 전환 코드**가 계속 필요하다.

### 베리언트로 나누면
- **모양은 Base 한 곳**에서 관리한다. 병 크기나 머티리얼을 바꾸면 Thrown과 Pickup에 같이 반영된다.
- **동작은 베리언트별로 분리**된다. 한 오브젝트에 두 동작이 섞이지 않으므로 위 문제가 처음부터 생기지 않는다.
- 상태 전환 코드가 필요 없다.

---

## 3. 구조 (화염 포션 예시)

```
PotionBottle_BottomFixed.fbx                (병 모델, Imports/habinImports/Potion/기본/)
 └ FirePotion_Base      ← 모양 + 물리만
    ├ FirePotion_Thrown ← + ThrownProjectile
    └ FirePotion_Pickup ← + PotionPickup
```

| 프리팹 | 부모 | 추가 컴포넌트 | 주요 값 |
|---|---|---|---|
| `FirePotion_Base` | fbx 모델 | `BoxCollider`, `Rigidbody` | 화염 머티리얼, BoxCollider 0.3³ (center y 0.15), 레이어·태그 `Potion` |
| `FirePotion_Thrown` | `FirePotion_Base` | `ThrownProjectile` | ImpactRadius 1.5, MaxLifeTime 5, Rigidbody Interpolate |
| `FirePotion_Pickup` | `FirePotion_Base` | `PotionPickup` | potionData = `GameData/Items/Potions/10002_화염 물약` |

- Base에는 **동작 스크립트를 넣지 않는다.**
- 화염 머티리얼은 `Imports/habinImports/Potion/10002_화염 물약.prefab`과 같은 것을 쓴다.

---

## 4. 새 포션 추가하는 법

예: 냉기 포션(`ColdPotion`)

1. **폴더 만들기**: `Assets/KMS/Prefabs/Potion/ColdPotion/`
2. **Base 만들기**
   1. `PotionBottle_BottomFixed.fbx`를 씬에 드래그한다.
   2. 포션 머티리얼을 입힌다(`habinImports/Potion/<번호>_<이름>.prefab`의 머티리얼 참고).
   3. `BoxCollider`와 `Rigidbody`를 추가하고, 레이어·태그를 `Potion`으로 정한다(자식 포함).
   4. 폴더로 드래그해서 `ColdPotion_Base`로 저장한 뒤 씬에서 지운다.
3. **베리언트 만들기**
   1. `ColdPotion_Base` 우클릭 → **Create → Prefab Variant**로 `ColdPotion_Thrown`을 만들고 `ThrownProjectile`을 추가한다.
   2. 다시 우클릭 → **Create → Prefab Variant**로 `ColdPotion_Pickup`을 만들고 `PotionPickup`을 추가한다. `Potion Data`에 해당 `PotionData` 에셋을 연결한다.

### 이름 규칙
| 항목 | 규칙 |
|---|---|
| 폴더 | `Assets/KMS/Prefabs/Potion/<영문이름>Potion/` |
| Base | `<영문이름>Potion_Base` |
| 던지기용 | `<영문이름>Potion_Thrown` |
| 바닥 아이템용 | `<영문이름>Potion_Pickup` |

---

## 5. 수정할 때 규칙

| 바꾸려는 것 | 수정할 곳 |
|---|---|
| 모양, 머티리얼, 콜라이더 크기, Rigidbody 질량 | **Base** |
| 착탄 반경, 수명, 착탄 이펙트 | **Thrown** |
| 연결된 `PotionData` | **Pickup** |

- 베리언트에서 Base 값을 실수로 바꿨다면 인스펙터 **Overrides → Revert**로 되돌린다.
- 베리언트에서 바꾼 값을 모든 베리언트에 적용하고 싶으면 **Overrides → Apply to Base**를 쓴다(Base가 바뀌므로 주의).
- 씬에 놓인 인스턴스에서 값을 바꾸면 그 씬에만 저장된다. 공통으로 바꿀 값은 프리팹을 열어서 수정한다.

---

## 6. 아직 연결되지 않은 부분

- 아직은 `PotionData`가 Thrown / Pickup 프리팹을 참조하지 않는다. 그래서 던질 때는 모든 포션이 `PlayerThrower.emptyBottlePrefab`으로 생성된다.
- 연결 예정 구조:
  ```csharp
  // PotionData
  public ThrownProjectile ProjectilePrefab;   // 던질 때. 비우면 빈 병
  public PotionPickup PickupPrefab;           // 인벤토리에서 버릴 때
  ```
  ```csharp
  // PlayerThrower.ThrowProjectile
  ThrownProjectile prefab = potion != null && potion.ProjectilePrefab != null
      ? potion.ProjectilePrefab
      : emptyBottlePrefab;
  ```
- 인벤토리에서 버리기를 구현할 때는 `ItemPickupController`가 버린 병을 바로 다시 줍지 않도록 막아야 한다. 방법은 둘 중 하나다.
  - 버린 뒤 몇 초 동안은 주울 수 없게 한다.
  - 줍기 반경 밖에 떨어뜨린다.
