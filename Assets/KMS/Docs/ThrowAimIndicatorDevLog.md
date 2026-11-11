# 던지기 조준 표시(Shift Aim) 개발 기록

> Shift를 누르고 있으면 포션의 **포물선 궤적**과 **착탄 피격 범위**를 보여 주는 기능을 만든 과정과,
> 조준선이 실제 포션 궤적과 어긋나던 문제를 단계별로 고친 기록입니다.
> - **Part 1**: 시간순 작업 기록 (무엇이 문제였고, 어떻게 바꿨는지)
> - **Part 2**: 현재 구조와 설정 방법
> - **Part 3**: 남은 작업
>
> 기준: 2026-11-10, `feat/kwon/player` 브랜치 작업 트리 (커밋 전)

---

## 0. 개요

### 조작
| 입력 | 동작 |
|---|---|
| Shift (누르고 있는 동안) | 손에서 마우스 위치까지 포물선 궤적 + 착탄 지점의 피격 범위 원 표시 |
| 좌클릭 | 던지기 (조준 여부와 상관없이 가능) |

- 조준선은 **내 화면에만** 보인다. 로컬 연출이라 네트워크 동기화는 하지 않는다.
- 실제 던지기와 **같은 계산 함수**를 쓰므로 조준선과 실제 궤적이 같다.

### 관련 파일
| 파일 | 역할 |
|---|---|
| `Input/PlayerControls.inputactions` | `Player/Aim` 액션 (`<Keyboard>/leftShift`) |
| `Scripts/Player/PlayerInputReader.cs` | `IsAimHeld` — Aim 키를 누르고 있는지 |
| `Scripts/Player/ThrowAimIndicator.cs` | 궤적과 범위 원을 그리는 컴포넌트 (신규) |
| `Scripts/Player/PlayerThrower.cs` | `GetProjectilePrefab` 추가, 발사 속도 적분 보정 |
| `Scripts/Player/PlayerFSM.cs` | `GetThrowablePotion` 추가 (포션 / 빈 병 판정 통일) |
| `Scripts/Potion/ThrownProjectile.cs` | `TryGetCollisionSphere` 추가 (충돌 구 정보 조회) |
| `Materials/AimIndicatorLine.mat` | 조준선용 머티리얼 (신규) |
| `Prefabs/Player/Player.prefab` | `AimIndicator` 자식 (`TrajectoryLine`, `RangeCircle`) |
| `Prefabs/Potion/EmptyPotion/EmptyPotion_base.prefab` | 콜라이더를 Box → Sphere로 교체 |
| `Prefabs/Potion/EmptyPotion/EmptyPotion_Thrown.prefab` | 콜라이더 중심을 피벗에 맞춤 |

---

# Part 1. 시간순 작업 기록

## 1단계. Aim 입력 추가

`PlayerControls.inputactions`의 `Player` 맵에 Button 타입 `Aim` 액션을 추가하고 `<Keyboard>/leftShift`에 바인딩했다.
`PlayerInputReader`는 다른 액션과 같은 방식으로 `Awake`에서 액션을 찾아 캐시한다. 조준은 누르고 있는 동안 계속 유지되는 상태라 버퍼 없이 바로 읽는다.

```csharp
public bool IsAimHeld => aimAction.IsPressed();
```

---

## 2단계. 조준 표시 컴포넌트 `ThrowAimIndicator`

### 핵심 아이디어: 실제 던지기 계산을 그대로 재사용
던질 때와 똑같은 순서로 계산해서 선을 그린다. 별도 공식을 만들면 둘이 어긋나기 때문이다.

```
마우스 지면 위치 (PlayerInputReader.TryGetMouseWorldPoint)
 → 사거리 제한   (PlayerThrower.ClampToThrowRange)
 → 발사 위치     (PlayerThrower.GetThrowOrigin)
 → 초기 속도·체공 시간·중력 (PlayerThrower.CalculateLaunchVelocity)
 → x(t) = origin + v·t + ½·g·t² 를 구간별로 찍어 LineRenderer에 넣음
```

### 동작 요약
- `LateUpdate`에서 갱신한다. 그 프레임의 입력과 상태가 반영된 뒤에 그리기 위해서다.
- 아래 경우에는 선을 숨긴다.
  - `IsMine`이 아님
  - 사망 상태
  - Shift를 누르지 않음
  - 마우스 지면 위치를 못 얻음
- `IsMine` 확인은 `Awake`가 아니라 **매 프레임** 한다. `Awake` 시점에는 소유권이 정해지지 않았을 수 있다.
- 범위 원의 반지름은 지금 던질 투사체 프리팹의 `ThrownProjectile.ImpactRadius`를 쓴다.

### 함께 추가한 공용 메서드
조준선과 실제 던지기가 **같은 판단**을 하도록, 두 곳에서 쓰던 로직을 메서드로 뺐다.

```csharp
// PlayerThrower — 포션에 맞는 투사체 프리팹. 포션이 없거나 프리팹이 비어 있으면 빈 병
public ThrownProjectile GetProjectilePrefab(PotionData potion)
    => potion != null && potion.ProjectilePrefab != null ? potion.ProjectilePrefab : emptyBottlePrefab;

// PlayerFSM — 지금 던지면 나갈 포션. 선택한 포션이 인벤토리에 없으면 null(빈 병)
public PotionData GetThrowablePotion()
{
    PotionData potion = Loadout.SelectedPotion;
    return potion != null && Inventory.GetItemCount(potion.Id) > 0 ? potion : null;
}
```
- `SpawnProjectile`은 `GetProjectilePrefab`을 쓴다.
- `ConsumeAttack`은 `GetThrowablePotion`을 쓴다.

---

## 3단계. 조준선 머티리얼 `AimIndicatorLine.mat`

기존 테스트용 `parabolaTest/ParabolaLine.mat`은 URP `Unlit`(불투명) 셰이더를 쓴다. 그래서 두 가지가 안 된다.
- LineRenderer의 **Color / Gradient(버텍스 컬러)가 무시**된다.
- 반투명 처리가 안 된다.

그래서 새 머티리얼을 만들었다.

| 항목 | 값 | 이유 |
|---|---|---|
| Shader | `Universal Render Pipeline/Particles/Unlit` | 버텍스 컬러를 곱해 LineRenderer 색·알파 그라데이션이 그대로 반영됨 |
| Surface Type | Transparent (Alpha 블렌드, ZWrite Off) | 반투명 선 |
| Render Face | Both (Cull Off) | 바닥에 눕힌 원(Alignment = Transform Z)이 각도에 따라 뒷면이 되어 사라지는 것 방지 |
| Color Mode | Multiply, Base Color 흰색 | 색은 각 LineRenderer의 Color에서 정함 |

궤적 선과 범위 원이 이 머티리얼 하나를 같이 쓰고, 색만 각 LineRenderer에서 따로 정한다.

> **LineRenderer 굵기 설정법**: Inspector의 LineRenderer 맨 위 `Width` 곡선 그래프 왼쪽 위 숫자가 굵기 배율이다(코드로는 `widthMultiplier`). 곡선에 점을 추가하면 위치별로 굵기를 다르게 줄 수 있다.

---

## 4단계. 궤적 불일치 — 원인 분석

조준선과 실제 포션이 "살짝" 어긋났다. 코드와 프리팹을 보고 원인 후보를 정리했다.

| # | 원인 | 내용 | 증상으로 구분하는 법 |
|---|---|---|---|
| A | **발사 위치가 FBX 본** | `PlayerThrower.throwOrigin`이 캐릭터 모델의 본 Transform을 가리킨다. 조준 중에는 대기·이동 자세의 손에서 선을 그리지만, 실제 발사는 몸을 목표 쪽으로 돌린 뒤(`PlayerAttackState.Enter`의 `RotateTowards`) 던지기 애니메이션 `ThrowReleaseTime` 시점의 손 위치에서 나간다. | 출발점부터 벌어지고, 옆쪽을 조준할수록 심하다 |
| B | **물리 적분 오차** | 아래 설명 참고 | 방향은 맞는데 항상 조금 짧게 떨어지고, 멀리 던질수록 심하다 |
| C | **조준선이 적을 통과** | `obstacleLayers`가 Default + Ground뿐이다. 실제 포션은 적(Enemy 레이어)에 닿으면 터진다. | 적이 사이에 있을 때만 다르다 |

### B. 물리 적분 오차 (semi-implicit Euler)
- 조준선은 해석해(공식) `x(t) = x0 + v·t + ½·g·t²`로 그린다.
- Rigidbody는 고정 스텝(`Fixed Timestep` 0.02s)마다 **속도를 먼저 갱신하고, 그 속도로 위치를 옮긴다**(semi-implicit Euler).
- n번째 스텝 후 위치(t = n·dt)를 계산하면 다음과 같다.

```
x_n = x0 + v0·t + ½·g·t² + ½·g·dt·t
                           └─ 공식보다 항상 이만큼 더 아래
```

체공 1.5초 기준으로 약 15cm 낮아서 목표보다 조금 앞에 떨어진다.

---

## 5단계. 보정 ① — 적분 오차 (원인 B)

발사 속도에서 반 스텝만큼의 중력을 미리 빼면 `½·g·dt·t` 항이 상쇄되어, 물리 엔진이 공식 궤적을 정확히 따라간다.

```csharp
// PlayerThrower.ThrowProjectile
Vector3 velocity = CalculateLaunchVelocity(origin, target, out _, out Vector3 gravity);
velocity -= 0.5f * Time.fixedDeltaTime * gravity;   // 물리 스텝의 적분 오차 보정
```

- 이 보정은 **실제 발사(`ThrowProjectile`)에만** 넣는다.
- `CalculateLaunchVelocity`에 넣으면 조준선 계산도 같이 바뀌어 다시 어긋난다.
- 조준선(`ThrowAimIndicator`)과 테스트용 `ParabolaPreview`는 수정하지 않아도 된다.

---

## 6단계. 보정 ② — 랜덤 회전 때문에 착탄이 틀어짐

### 문제
`ThrownProjectile`은 던질 때 랜덤 회전(`ApplyRandomSpin`)을 준다. 그런데 빈 병의 콜라이더가 **BoxCollider(0.3³)** 여서 문제가 생겼다.
- 박스는 회전 방향에 따라 바닥·벽에 닿는 범위가 달라진다.
- 그래서 같은 곳을 조준해도 매번 조금씩 다른 곳에서 터졌다.

### 해결
`EmptyPotion_base.prefab`의 콜라이더를 **SphereCollider(Radius 0.15)** 로 바꿨다.
- 구는 어느 방향으로 돌아도 모양이 같다. 회전은 보이는 모습에만 영향을 주고 충돌에는 영향을 주지 않는다.
- Rigidbody의 무게중심은 자동 계산(`Automatic Center Of Mass`)이라 구 중심으로 따라간다.

---

## 7단계. 보정 ③ — 벽에 막힘/안 막힘 경계에서 어긋남

### 문제
벽 모서리를 아슬아슬하게 스치는 궤적에서 자주 어긋났다. 조준선은 벽을 넘어가는데, 실제 포션은 벽 끝에 걸려 터졌다.

### 원인
| 원인 | 내용 |
|---|---|
| 두께 차이 | 조준선은 `Physics.Linecast`(두께 0)로 장애물을 검사했다. 실제 포션은 반지름 0.15인 구라서 선이 벽을 15cm 이내로 스치면 포션만 걸린다. |
| 높이 차이 | 병 모델(`PotionBottle_BottomFixed.fbx`)의 피벗이 **병 바닥**이라 SphereCollider Center가 `(0, 0.13, 0)`이었다. 발사는 피벗 기준이므로 실제 충돌 구는 조준선보다 **13cm 위**를 날아갔다. |

### 해결 1 — 조준선을 같은 크기의 구로 검사
`ThrownProjectile`에 충돌 구 정보를 꺼내는 메서드를 추가했다.

```csharp
// 미리보기용: 충돌 구의 반지름과, 피벗에서 구 중심까지의 오프셋(회전 없는 상태 기준)
public bool TryGetCollisionSphere(out Vector3 centerOffset, out float radius)
```
- 프리팹 에셋에 대해서도 바로 호출할 수 있다.
- 루트 위치를 빼서 계산하므로 프리팹 루트에 위치값이 남아 있어도 문제없다.

`ThrowAimIndicator.DrawTrajectory`는 구간마다 `Linecast` 대신 `SphereCast`를 쓴다.
- 선은 포션이 멈추는 위치(구 중심)에서 끊는다.
- 범위 원은 벽과 맞닿은 지점(`hit.point`)에 그린다. 실제 착탄 이펙트와 범위 판정도 접촉점(`collision.GetContact(0).point`)을 기준으로 하므로 서로 일치한다.
- 시작부터 겹쳐 있으면 `SphereCast`가 `hit.point`를 `(0,0,0)`으로 돌려준다. 이 경우를 따로 처리한다.

### 해결 2 — 빈 병 투사체의 콜라이더 중심을 피벗에 맞춤
피벗, 충돌 구 중심, 무게중심을 한 점에 모았다.

| 대상 | 변경 |
|---|---|
| `EmptyPotion_Thrown`의 모델 자식 `Bottle_Cork`, `Bottle_Glass`, `Bottle_Liquid`(비활성) | Local Position Y `0` → `-0.13` |
| `EmptyPotion_Thrown`의 SphereCollider | Center `(0, 0.13, 0)` → `(0, 0, 0)` |

- **Thrown 베리언트에만 오버라이드로 저장**했다. 같은 Base를 쓰는 `EmptyPotion_Pickup`은 바닥에 놓이는 아이템이라 바닥 피벗이 맞으므로 그대로 둔다(Center `(0, 0.13, 0)` 유지).
- 그래서 `EmptyPotion_base.prefab`과 FBX는 이 단계에서 수정하지 않았다.
- 적용은 실행 중인 Unity Editor에 Unity CLI(`unity command eval`)로 아래 C#을 실행해서 했다. Editor에서 직접 하려면 위 표대로 값을 바꾸면 결과가 같다.

```csharp
const string path = "Assets/KMS/Prefabs/Potion/EmptyPotion/EmptyPotion_Thrown.prefab";
GameObject root = PrefabUtility.LoadPrefabContents(path);
SphereCollider sphere = root.GetComponent<SphereCollider>();
Vector3 offset = sphere.center;                       // 루트 로컬 기준 (0, 0.13, 0)
foreach (Transform child in root.transform)           // 비활성 자식(Liquid)도 포함
    child.localPosition -= offset;                    // 모델을 내려 구 중심을 피벗으로
sphere.center = Vector3.zero;
PrefabUtility.SaveAsPrefabAsset(root, path);          // 베리언트의 오버라이드로 저장됨
PrefabUtility.UnloadPrefabContents(root);
```

콜라이더 중심이 피벗과 같아졌으므로, 빈 병에서는 `TryGetCollisionSphere`의 `centerOffset`이 0이 된다.
`SphereCast` 코드는 오프셋이 있는 다른 포션에도 그대로 동작하도록 오프셋 처리를 남겨 두었다.

---

# Part 2. 현재 구조와 설정 방법

## Player 프리팹 설정

```
Player
 └ AimIndicator          ← ThrowAimIndicator
    ├ TrajectoryLine     ← LineRenderer (궤적)
    └ RangeCircle        ← LineRenderer (범위 원)
```

| 항목 | TrajectoryLine | RangeCircle |
|---|---|---|
| Material | `AimIndicatorLine` | `AimIndicatorLine` |
| Width | 0.1 | 0.05 |
| Alignment | View | Transform Z |
| Loop | 끔 | 켬 |
| Use World Space | 켬 | 켬 |
| Color (권장) | 노랑 계열, 끝으로 갈수록 알파 감소 | 흰색·주황색, 알파 0.6~0.7 |

| `ThrowAimIndicator` 필드 | 현재 값 | 설명 |
|---|---|---|
| `trajectorySegmentCount` | 30 | 궤적을 나누는 구간 수 |
| `circleSegmentCount` | 40 | 원을 그리는 점 개수 |
| `obstacleLayers` | Default, Ground | 조준선을 끊을 레이어. 플레이어 레이어는 넣지 않는다(손에서 바로 자기 몸에 걸림) |
| `circleHeightOffset` | 0.05 | 원이 바닥에 묻히지 않게 띄우는 높이 |

## 새 포션 Thrown 프리팹을 만들 때 체크리스트
조준선과 실제 궤적을 맞추려면 아래를 지킨다. 프리팹 구조는 `PotionPrefabVariant.md`를 따른다.

1. Base 콜라이더는 **SphereCollider**를 쓴다. BoxCollider는 회전에 따라 착탄이 틀어진다.
2. **Thrown 베리언트에서만** 모델 자식들을 Center Y만큼 내리고, SphereCollider Center를 `(0, 0, 0)`으로 맞춘다. Pickup은 그대로 둔다.
3. Thrown의 `ImpactRadius`를 정한다. 조준선의 범위 원이 이 값을 그대로 쓴다.

---

# Part 3. 남은 작업

현재 코드와 프리팹을 확인한 결과다.

| 항목 | 현재 상태 | 권장 조치 |
|---|---|---|
| 발사 위치 (원인 A) | `throwOrigin`이 아직 FBX 본을 가리킴 | Player 루트 바로 아래에 빈 오브젝트 `ThrowOrigin`을 `(0, 1.4, 0)`에 두고 연결한다. 몸 중심축 위라 회전해도 위치가 변하지 않는다. 앞쪽(z) 오프셋은 주지 않는다. |
| 적 충돌 (원인 C) | `obstacleLayers` = Default + Ground | Enemy 레이어를 추가한다. 실제 충돌과 맞추려면 Potion 레이어의 Layer Collision Matrix와 같게 맞춘다. |
| 화염 포션 콜라이더 | `FirePotion_Base`가 아직 BoxCollider (center y 0.15) | 빈 병과 같은 방식(6·7단계)으로 Sphere 전환 + Thrown 중심 맞춤 |
| `PotionPrefabVariant.md` | 4장의 "BoxCollider 추가" 안내가 이번 결정과 다름 | SphereCollider와 Thrown 중심 맞춤 단계를 반영해 갱신한다 |
| 코드 정리 | `ThrowAimIndicator.GetImpactRadius`는 더 이상 호출되지 않음 | 삭제 |
| 코드 정리 | `PlayerFSM.ConsumeAttack`이 `GetThrowablePotion` 결과를 `hasPotion`으로 다시 검사함 | `AttackPotion = GetThrowablePotion();`으로 단순화 |
| 코드 정리 | `PlayerThrower.ThrowProjectile` 첫 줄에 빈 줄 | 삭제 |
