# 포션 슬롯 UI MVP 리팩토링 개발 기록

> 포션 퀵슬롯 UI를 **MVP(Model–View–Presenter)** 구조로 옮긴 과정을 정리했습니다. 함께 등록 방식을 "인벤토리에서 포션 클릭 → HUD 슬롯의 **+ 버튼**으로 장착"으로 바꾼 기록입니다.
> 장착·선택한 포션을 **플레이어 저장 데이터(`PlayerFSM.SaveData`/`LoadData`)에 넣기 위한 준비** 작업이기도 합니다.
> - **Part 1**: 시간순 작업 기록 (무엇이 문제였고, 어떻게 바꿨는지)
> - **Part 2**: 현재 구조와 설정 방법
> - **Part 3**: 남은 작업
>
> 기준: 2026-11-12, `feat/kwon/player` 브랜치
> 관련 커밋: `f12b370` → `338a3c0` → `662a06f`, 그리고 그 이후의 작업 트리(커밋 전)

---

## 0. 개요

### 조작
| 입력 | 동작 |
|---|---|
| 인벤토리에서 포션 좌클릭 | 장착 대기 시작. 인벤토리 위에 어두운 패널이 뜨고, HUD 슬롯 3개에 **+** 버튼이 나타난다 |
| HUD 슬롯의 **+** 클릭 | 대기 중인 포션을 그 슬롯에 장착. + 버튼과 어두운 패널이 사라진다 |
| 인벤토리 닫기 | 장착 대기 취소 |
| 1 / 2 / 3 | 던질 슬롯 선택. 선택 프레임이 이동한다 |

- 슬롯에는 포션 아이콘과 **인벤토리 보유 수량**이 표시된다. 포션을 던지거나 주우면 수량이 바로 갱신된다.
- 포션 슬롯 상태는 로컬 플레이어의 `PotionLoadout` 하나에만 저장된다.

### 관련 파일
| 파일 | 역할 | 변경 |
|---|---|---|
| `KMS/Scripts/Player/PotionLoadout.cs` | **Model**. 장착 슬롯 3개, 선택 슬롯, 장착 대기 상태 | 수정 |
| `KMS/Scripts/UI/PotionSlot/IPotionSlotView.cs` | View 인터페이스 | 이동·수정 |
| `KMS/Scripts/UI/PotionSlot/PotionSlotView.cs` | **View**. 아이콘·수량·선택 프레임·+ 버튼 표시 | 이동·수정 |
| `KMS/Scripts/UI/PotionSlot/PotionSlotPresenter.cs` | **Presenter**. Model·Inventory 이벤트 → View 갱신, + 클릭 → Model | 이동·수정 |
| `KMS/Scripts/UI/PlayerHUDBinder.cs` | HUD Presenter 생성. Inventory 전달 추가 | 수정 |
| `GameData/Scripts/Inventory/PotionSlotSelector.cs` | 인벤토리 쪽 "장착 대기 시작·취소"와 어두운 패널만 담당 | 축소 |
| `GameData/Scripts/Inventory/LocalPlayerUIBinder.cs` | `PotionSlotSelector.Bind(inventory, loadout)` 호출 | 수정 |
| `KMS/Scripts/Player/PotionLoadoutSync.cs` | UI 데이터를 매 프레임 Loadout에 복사하던 컴포넌트 | **삭제** |
| `KMS/Prefabs/UI/PotionSlot.prefab` | 슬롯 3칸, 각 칸에 `EquipButton`(+) 추가 | 수정 |
| `KMS/Prefabs/UI/PlayerHUD.prefab` | `SelectFrame`, Canvas sortingOrder 1 | 수정 |
| `GameData/Canvas.prefab` | 옛 PotionSlot, `RegisterPotion` 버튼, `PotionLoadoutSync` 제거 | 수정 |

---

# Part 1. 시간순 작업 기록

## 1단계. 왜 리팩토링했나

리팩토링 전에는 **같은 데이터를 두 곳이 따로 들고 있었다.**

```
[인벤토리 Canvas]                                   [플레이어]
PotionSlotSelector                                   PotionLoadout
 ├ registeredPotions[3]  ── PotionLoadoutSync ──▶    ├ slots[3]
 ├ ActiveSlotIndex          (LateUpdate마다 비교·복사)  └ selectedIndex
 ├ 숫자키 1~3 처리                                       ▲
 └ slotView(PotionSlotView) 직접 그림                    └ PlayerInputReader의 숫자키 1~3
```

| 문제 | 내용 |
|---|---|
| 데이터가 두 곳 | `PotionSlotSelector`와 `PotionLoadout`이 각각 장착 정보를 가짐 |
| 매 프레임 복사 | 변경 이벤트가 없어서 `PotionLoadoutSync`가 `LateUpdate`마다 비교 후 복사 |
| 그리는 경로가 두 개 | Selector가 View를 직접 그리고, HUD Presenter도 따로 그림 |
| 입력 중복 | 숫자키 1~3을 Selector의 `Update`와 `PlayerInputReader` 양쪽에서 처리 |
| 크래시 | 빈 슬롯 해제 시 `potion.Icon` NRE, 초기 선택 인덱스 -1로 View 인덱스 초과 |

목표는 **"포션 슬롯 상태가 궁금하면 `PotionLoadout`만 보면 된다"**는 구조였다.

### 목적 2. 장착·선택 포션의 저장/불러오기 준비
`PlayerFSM.SaveData`/`LoadData`에 지금 장착한 포션과 선택한 슬롯을 함께 저장하려고 했다. 하지만 기존 구조로는 할 수 없었다.

| 문제 | 내용 |
|---|---|
| 데이터가 플레이어에 없음 | `PlayerFSM.SaveData`는 플레이어 자신의 데이터(`PlayerWorldData`: `CurrentHp`, `CurrentStamina`, `Inventory.Slots`)만 저장한다. 장착 정보는 씬의 인벤토리 Canvas에 있는 `PotionSlotSelector.registeredPotions`에 있어서 플레이어가 저장할 수 없었다 |
| 불러와도 덮어써짐 | `PotionLoadout`에 불러온 값을 넣어도 `PotionLoadoutSync`가 다음 `LateUpdate`에 UI의 값(빈 슬롯)으로 덮어썼다. 복사가 UI → Loadout 한 방향뿐이기 때문이다 |

리팩토링 후에는 장착 정보와 선택 슬롯이 **플레이어의 `PotionLoadout` 한 곳**에만 있다. `PlayerFSM`이 이미 `Loadout` 프로퍼티로 들고 있어서 `Inventory.Slots`와 같은 방식으로 저장할 수 있다.
불러온 뒤 `PotionLoadout`의 이벤트가 발생하면 Presenter가 HUD를 자동으로 다시 그린다. HUD가 불러오기보다 늦게 연결돼도 Presenter의 첫 갱신이 Loadout 값을 그대로 읽으므로 순서와 상관없다.

---

## 2단계. UI 폴더 정리 (`f12b370`)

`KMS/Scripts/UI/`에 섞여 있던 패널 스크립트를 패널별 폴더로 나눴다.

```
UI/
 ├ PotionSlot/   IPotionSlotView, PotionSlotPresenter, PotionSlotView
 ├ StatPanel/    IStatPanelView, StatPanelPresenter, StatPanelView
 └ StatusPanel/  IStatusPanelView, StatusEffectIconView, StatusPanelPresenter, StatusPanelView
```

- `.cs.meta`를 함께 옮겼으므로 **GUID가 유지**된다. 프리팹에 붙은 스크립트 참조도 끊기지 않는다.
- PotionSlot의 View, Presenter, 인터페이스는 이 커밋에서 1차 수정됐다(4·5단계 내용 일부).

---

## 3단계. 설계 결정

| 결정 | 이유 |
|---|---|
| **Model은 `PotionLoadout` 하나** | 던지기(`PlayerFSM.GetThrowablePotion`)가 이미 Loadout을 읽는다. 데이터를 한 곳에 모으면 동기화가 필요 없다 |
| **View는 HUD의 PotionSlot 하나** | 인벤토리 Canvas 안의 PotionSlot은 폐기한다 |
| **등록은 HUD의 + 버튼** | 인벤토리에서 포션을 고르면 HUD 슬롯에 +가 나타나고, 누른 슬롯에 장착된다. 평소에는 슬롯이 버튼이 아니다 |
| **장착 대기 상태도 `PotionLoadout`에** | 대기 시작은 인벤토리 UI가 하고, 대기 종료는 HUD가 한다. 두 UI는 다른 Canvas에서 다른 경로로 연결되므로, **둘 다 닿을 수 있는 곳은 로컬 플레이어(= `PotionLoadout`)뿐이다** |
| **`PotionSlotSelector`는 남김** | 툴팁 닫기, 어두운 패널, 인벤토리를 닫을 때의 취소는 Model이 아니라 인벤토리 화면의 일이다. 팀원 코드(`InventorySlotUI`, `InventoryUI`)도 계속 Selector만 호출하면 되어 수정 범위가 작다 |

> 대기 상태가 커지면(중복 장착 검사, 취소 이유 등) `PotionLoadout`이 들고 있는 별도 순수 C# 클래스로 뺄 수 있다. 접근 경로가 같아서 나중에 분리해도 바꿀 곳이 적다.

---

## 4단계. Model — `PotionLoadout` (`338a3c0` 이후)

### 이벤트 분리
하나뿐이던 `OnChanged`를 용도별로 나눴다. Model은 `Sprite` 같은 **화면 정보를 넘기지 않고 데이터(`PotionData`)만 넘긴다.**

```csharp
public event Action<int> OnSelectChanged;               // 슬롯 선택 변경시
public event Action<int, PotionData> OnEquipChanged;    // 포션 장착 및 해제시
public event Action<bool> OnEquipModeChanged;           // 장착 대기 시작(true) / 종료(false)
```

- 처음에는 `OnEquipChanged(int, Sprite)`에 `potion.Icon`을 넘겼다. 그래서 슬롯을 비울 때(`null`) NRE가 났다. `PotionData`를 넘기도록 바꿔 해결했다.

### 장착 대기 상태 추가
```csharp
public PotionData PendingPotion { get; private set; }
public bool IsEquipping => PendingPotion != null;

// 인벤토리에서 고른 포션으로 장착 대기를 시작한다
public void BeginEquip(PotionData potion)
{
    if (potion == null) return;
    PendingPotion = potion;
    OnEquipModeChanged?.Invoke(true);
}

// 대기 중인 포션을 슬롯에 장착하고 대기를 끝낸다
public void EquipToSlot(int slotIndex)
{
    if (!IsEquipping) return;
    SetSlot(slotIndex, PendingPotion);
    CancelEquip();
}

// 장착 대기를 취소한다
public void CancelEquip()
{
    if (!IsEquipping) return;
    PendingPotion = null;
    OnEquipModeChanged?.Invoke(false);
}
```

---

## 5단계. View / Presenter

### `IPotionSlotView`
```csharp
public interface IPotionSlotView
{
    void SetSlotIcon(int slotIndex, PotionData potion);
    void SetSelectedSlot(int slotIndex);
    void SetSlotCount(int slotIndex, int amount, bool visible);
    void SetEquipButtonsVisible(bool visible);
    event Action<int> OnEquipButtonClicked;
}
```

### View — + 버튼 클릭을 슬롯 번호와 함께 알림
View는 "몇 번 버튼이 눌렸다"는 이벤트만 내보낸다. 그걸로 무엇을 할지는 Presenter가 정한다. 인스펙터 OnClick에 대상을 직접 연결하지 않는 이유는 View가 누구를 호출하는지 프리팹에 박히지 않게 하기 위해서다.

```csharp
private void Awake()
{
    for (int i = 0; i < equipButtons.Length; i++)
    {
        int index = i;   // 반드시 복사
        equipButtons[i].onClick.AddListener(() => OnEquipButtonClicked?.Invoke(index));
    }
}
```

> **`int index = i;`가 필요한 이유**: 람다는 `i`의 값이 아니라 **변수 자체**를 기억한다. 반복문이 끝나면 `i`는 3이 되므로, 복사하지 않으면 어느 버튼을 눌러도 `Invoke(3)`이 된다.

### Presenter — 구독 5개
| 구독 | 처리 |
|---|---|
| `model.OnEquipChanged` | `RefreshSlot(i)`: 아이콘과 수량을 다시 그림 |
| `model.OnSelectChanged` | `view.SetSelectedSlot` |
| `model.OnEquipModeChanged` | `view.SetEquipButtonsVisible` |
| `inventory.Changed` | `RefreshAllSlots()`: 수량 갱신 |
| `view.OnEquipButtonClicked` | `model.EquipToSlot(i)` |

```csharp
// 슬롯 하나의 아이콘과 수량을 Model/Inventory 기준으로 다시 그린다
private void RefreshSlot(int slotIndex)
{
    PotionData potion = model.GetSlot(slotIndex);
    int amount = potion != null ? inventory.GetItemCount(potion.Id) : 0;

    view.SetSlotIcon(slotIndex, potion);
    view.SetSlotCount(slotIndex, amount, potion != null);
}
```

- 생성자에서 처음 한 번 `RefreshAllSlots()`, `SetEquipButtonsVisible(model.IsEquipping)`, `SetSelectedSlot(model.SelectedIndex)`를 호출한다.
- `Dispose()`에서 5개 구독을 모두 해제한다. `PlayerHUDBinder.Bind`가 다시 호출돼도 이전 Presenter가 클릭을 받지 않는다.
- `PlayerHUDBinder`는 `new PotionSlotPresenter(localPlayer.Loadout, localPlayer.Inventory, potionSlotView)`로 생성한다.

---

## 6단계. `PotionSlotSelector` 축소 (`662a06f`)

Selector를 **"포션 선택 시작/취소를 Model에 전달 + 어두운 패널(`SelectPotion`) 관리"**만 하도록 줄였다.

| 구분 | 항목 | 대신 담당하는 곳 |
|---|---|---|
| 삭제 | `slotView`, `RefreshSlots` | HUD의 `PotionSlotPresenter` |
| 삭제 | `registeredPotions`, `GetRegisteredPotion`, `selectedPotion`, `selectedPotionData` | `PotionLoadout` |
| 삭제 | `ActiveSlotIndex`, `SelectActiveSlot`, 숫자키 처리 | `PlayerInputReader` → `PlayerFSM` → `PotionLoadout.SelectSlot` |
| 삭제 | `RegisterPotion` | HUD + 버튼 → Presenter → `PotionLoadout.EquipToSlot` |
| 추가 | `Bind(Inventory, PotionLoadout)` | `public Inventory inventory` 필드 대신 주입받음 |
| 추가 | `OnEquipModeChanged` 구독 | HUD에서 장착이 끝나면 어두운 패널도 자동으로 닫힘 |

```csharp
// 인벤토리에서 클릭한 포션으로 장착 대기를 시작한다
public void BeginSelection(PotionData potion)
{
    if (potion == null || inventory == null || loadout == null) return;
    if (inventory.GetItemCount(potion.Id) <= 0) return;

    if (tooltip != null) tooltip.Hide();
    loadout.BeginEquip(potion);          // 패널은 HandleEquipModeChanged(true)에서 켜짐
}

// 장착 대기 상태가 바뀌면 선택 상태와 선택 화면을 맞춘다
private void HandleEquipModeChanged(bool isEquipping)
{
    isSelecting = isEquipping;
    if (selectPotion != null) selectPotion.SetActive(isEquipping);
}
```

- 이 파일은 GameData(팀원 담당) 코드다. 그래서 지운 코드는 바로 삭제하지 않고 `// [MVP 삭제] 이유` 주석으로 남겨 두었다.
- `selectPotion`은 인벤토리 Canvas의 반투명 검은 전체 화면 Image다. 켜져 있는 동안 화면을 어둡게 하고, 뒤쪽 인벤토리 클릭을 막는다.

---

## 7단계. 바인딩 정리와 `PotionLoadoutSync` 삭제

- `LocalPlayerUIBinder.TryBindPlayer`
  - 내 플레이어의 `Inventory`와 `PotionLoadout`을 함께 가져온다.
  - `potionSlotSelector.Bind(inventory, loadout)`를 호출한다.
  - 기존처럼 `enabled`를 껐다 켜는 패턴을 유지한다.
- 데이터가 `PotionLoadout` 한 곳에만 있으므로 **`PotionLoadoutSync`(+ `.meta`)를 삭제**했다.
  - `GameData/Canvas.prefab`에 붙어 있던 컴포넌트도 제거했다. 남겨 두면 Missing Script가 된다.

---

## 8단계. 프리팹 작업

### + 버튼 추가 (`PotionSlot.prefab`)
`PotionIcon_1~3` 아래 마지막 자식으로 `EquipButton`을 추가했다.

| 항목 | 값 |
|---|---|
| 이미지 | `Imports/Bamao/BamaoUIPack/Sprites/Character sheet/Button_plus.png` (테두리 있는 + 버튼, 47×52) |
| RectTransform | 중앙 앵커·피벗, 위치 (0, 0), 크기 47×52 (Native Size) |
| 컴포넌트 | Image (Raycast Target 켬) + Button (Target Graphic = 자기 Image, OnClick 비움) |
| 기본 상태 | **비활성**. Presenter가 장착 대기 중에만 켠다 |

- 후보였던 `icon_plus.png`는 테두리가 없어 배경이 따로 필요했고, `icon white_plus.png`는 흰색이라 제외했다.
- 적용은 실행 중인 Unity Editor에 Unity CLI(`unity command script eval_file`)로 아래 C#을 실행해서 했다.

```csharp
var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(".../Character sheet/Button_plus.png");
var root = PrefabUtility.LoadPrefabContents("Assets/KMS/Prefabs/UI/PotionSlot.prefab");
for (int i = 1; i <= 3; i++)
{
    var slot = root.transform.Find("PotionIcon_" + i);
    var go = new GameObject("EquipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
    // 부모 지정 → 중앙 앵커 → sprite 지정 + SetNativeSize → targetGraphic 지정 → SetActive(false)
}
PrefabUtility.SaveAsPrefabAsset(root, path);
PrefabUtility.UnloadPrefabContents(root);
```

### 인벤토리 Canvas 정리 (`GameData/Canvas.prefab`)
- 옛 PotionSlot 오브젝트(`PotionSlotView` + `RegisterPotion`을 호출하던 버튼 3개)를 삭제했다.
- `PotionLoadoutSync` 컴포넌트를 제거했다.
- `SelectPotion` 어두운 패널은 유지한다.

### Canvas 정렬 순서
- `PlayerHUD`와 `GameData/Canvas`는 둘 다 Screen Space Overlay에 sortingOrder 0이었다.
- 순서가 같으면 어느 쪽이 위에 그려질지 보장되지 않는다. 그러면 `SelectPotion` 패널이 HUD의 +를 덮고 클릭을 막을 수 있다.
- **`PlayerHUD` Canvas의 sortingOrder를 1로** 올렸다.

---

## 9단계. 리뷰 중 발견해 고친 문제

| # | 증상 | 원인 | 해결 |
|---|---|---|---|
| 1 | HUD 연결 시 NRE | `SetSlotIcon`이 `data.Icon`을 null 확인 없이 읽음. 게임 시작 시 슬롯이 비어 있음 | `Sprite icon = data != null ? data.Icon : null;` |
| 2 | `Awake`에서 NRE, + 버튼 미동작 | 필드명을 `equipButton` → `equipButtons`로 바꿔서 **직렬화된 연결이 끊김**(프리팹에는 옛 이름으로 저장돼 있었음) | 인스펙터에서 다시 연결 후 프리팹 저장 (`[FormerlySerializedAs]`로도 해결 가능) |
| 3 | 수량 갱신 시 NRE | `countBadges` 미연결 | 프리팹에서 연결 |
| 4 | 시작 직후 수량 미표시 | Presenter의 첫 갱신이 아이콘만 그림 | 첫 갱신을 `RefreshAllSlots()`로 변경 |
| 5 | 재연결 시 이전 Presenter가 계속 반응 | `Dispose`가 일부 구독만 해제 | 5개 모두 해제 |
| 6 | **게임 시작 직후 선택 프레임이 1번 슬롯 왼쪽 아래에 표시됨** | 아래 설명 참고 | 프레임을 선택된 **셀의 자식**으로 붙임 |
| 7 | 선택된 슬롯의 +가 안 눌림 | 프레임을 셀 마지막 자식으로 붙였는데 `SelectFrame`의 Raycast Target이 켜져 있어 클릭을 가로챔 | Raycast Target 끔 + `SetAsFirstSibling()`으로 아이콘 아래에 그림 |

### 6번 상세 — 선택 프레임 위치 오류
1. `LocalPlayerHUD.Start()`가 `Instantiate(hudPrefab)` 직후 **같은 프레임**에 `Bind`를 호출한다. 그 안에서 Presenter가 `SetSelectedSlot(0)`을 부른다.
2. 이전 구현은 `potionIcon[0].transform.position`을 읽어서 프레임을 옮겼다.
3. 하지만 GridLayoutGroup은 **그 프레임의 렌더링 직전**에야 자식을 배치한다. 그 전까지 `PotionIcon_N`은 프리팹에 저장된 값, 즉 앵커 (0,0), 위치 (0,0), 크기 0에 있다. 레이아웃이 조종하는 값이라 0으로 저장되기 때문이다. 그래서 세 아이콘이 모두 PotionSlot의 **왼쪽 아래 모서리**에 겹쳐 있다.
4. 틀린 위치가 바로잡히지도 않는다. `PotionLoadout.selectedIndex`의 초기값이 0이라 1번 키를 눌러도 `SelectSlot`이 early return하고, 이벤트가 발생하지 않는다.

**해결**: 위치를 계산하지 않고 프레임을 셀(`PotionIcon_N`)의 자식으로 붙였다. 레이아웃 타이밍이나 해상도와 상관없이 항상 슬롯을 따라다닌다.

```csharp
// 선택된 슬롯 위로 선택 프레임을 옮긴다
public void SetSelectedSlot(int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= potionIcon.Length || selectedFrame == null) return;
    Image icon = potionIcon[slotIndex];
    if (icon == null) return;

    RectTransform frame = selectedFrame.rectTransform;
    frame.SetParent(icon.transform.parent, false);   // GridLayout 셀(PotionIcon_N) 아래
    frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
    frame.anchoredPosition = Vector2.zero;
    frame.SetAsFirstSibling();                       // 아이콘·+ 버튼 아래에 그림
}
```

- `PotionSlot` 자체(GridLayoutGroup이 붙은 오브젝트) 아래에 붙이면 안 된다. 프레임이 네 번째 칸으로 배치된다.
- `SetParent(..., false)`는 로컬 scale을 유지한다. 그래서 실제 크기는 `PotionSlot`의 scale(1.2) × 프레임 scale이 된다.

---

# Part 2. 현재 구조와 설정 방법

## 데이터 흐름
```
인벤토리 포션 클릭 ─ InventorySlotUI → PotionSlotSelector.BeginSelection → loadout.BeginEquip(potion)
                                          └ OnEquipModeChanged(true) ─┬→ Presenter → View: + 표시
                                                                      └→ Selector: 어두운 패널 켬
HUD + 클릭 ─ View.OnEquipButtonClicked(i) → Presenter → loadout.EquipToSlot(i)
                                          ├ OnEquipChanged(i, potion) → Presenter → 아이콘·수량 갱신
                                          └ OnEquipModeChanged(false) → + 숨김, 패널 끔
인벤토리 닫기 ─ InventoryUI.OnDisable → PotionSlotSelector.CancelSelection → loadout.CancelEquip()
숫자키 1~3 ─ PlayerInputReader → PlayerFSM → loadout.SelectSlot → OnSelectChanged → 프레임 이동
인벤토리 변화(투척·획득) ─ inventory.Changed → Presenter → 수량 갱신
던지기 ─ PlayerFSM.GetThrowablePotion → loadout.SelectedPotion
```

## 역할 분담
| 역할 | 클래스 | 하는 일 | 하지 않는 일 |
|---|---|---|---|
| Model | `PotionLoadout` | 장착 슬롯, 선택 슬롯, 장착 대기 상태 보관과 변경 이벤트 | 화면 표시 |
| View | `PotionSlotView` | 받은 값 표시, + 클릭을 슬롯 번호로 알림 | 데이터 보관, 판단 |
| Presenter | `PotionSlotPresenter` | Model·Inventory 이벤트 → View, View 입력 → Model | Unity 오브젝트 직접 조작 |
| 인벤토리 어댑터 | `PotionSlotSelector` | 장착 대기 시작·취소 전달, 어두운 패널 | 슬롯 데이터 보관, 슬롯 그리기 |
| 연결 | `PlayerHUDBinder` / `LocalPlayerUIBinder` | 로컬 플레이어의 Model을 HUD / 인벤토리 UI에 연결 | |

## 프리팹 구성
```
PlayerHUD (Canvas, sortingOrder 1)  ← PlayerHUDBinder
 ├ SelectFrame                      ← 실행 중에는 선택된 PotionIcon_N 아래로 옮겨짐
 └ PotionSlot (GridLayoutGroup, cell 100×100, scale 1.2)  ← PotionSlotView
    └ PotionIcon_1~3
       ├ Potion        (Image)      → potionIcon[i]
       ├ Hotkey / Text              (단축키 숫자)
       ├ Count         (수량 뱃지)   → countBadges[i], amountTexts[i]
       └ EquipButton   (+, 비활성)   → equipButtons[i]
```

| 필드 | 연결 위치 | 비고 |
|---|---|---|
| `potionIcon`, `countBadges`, `amountTexts`, `equipButtons` | `PotionSlot.prefab` 원본 | 프리팹 내부 오브젝트라 원본에서 연결 |
| `selectedFrame` | `PlayerHUD.prefab`의 오버라이드 | HUD에 있는 오브젝트. scale 1.1, **Raycast Target 끔** |

## 설정 시 주의
- **HUD Canvas의 sortingOrder는 인벤토리 Canvas보다 높게** 둔다. 그래야 `SelectPotion` 패널이 +를 막지 않는다.
- **`SelectFrame`의 Raycast Target은 끈다.** 켜면 선택된 슬롯의 + 클릭을 가로챈다.
- View 필드명을 바꿀 때는 `[FormerlySerializedAs("옛이름")]`을 붙인다. 붙이지 않으면 프리팹 연결이 끊긴다(9단계 2번).

---

# Part 3. 남은 작업

현재 코드와 프리팹을 확인한 결과다.

| 항목 | 현재 상태 | 권장 조치 |
|---|---|---|
| **저장/불러오기 연결** | `PlayerFSM.SaveData`/`LoadData`가 장착·선택 포션을 아직 저장하지 않음 | `SaveData`의 `PlayerWorldData`에 슬롯 3개의 포션 Id와 `Loadout.SelectedIndex`를 추가하고, `LoadData`에서 `Loadout`에 복원한다 (아래 확인할 점 참고) |
| 사용하지 않는 코드 | `PotionLoadout`의 `EquipMode` enum과 `public EquipMode EquipMode` 필드. public 필드라 `Player.prefab`에 `EquipMode: 0`으로 직렬화되어 있음 | 저장 구조를 정리하기 전에 삭제 |
| null 확인 | `PotionSlotView`에서 `countBadges[i]`, `equipButtons[i]`를 확인 없이 사용 | 연결이 빠지면 바로 NRE가 나므로 null 확인 추가 |
| 수량 표시 기준 | 뱃지는 `amount > 0`, 텍스트는 `visible`(장착 여부) 기준 | 장착됐지만 수량 0인 슬롯을 어떻게 보일지 정해 한 기준으로 통일 |
| 주석 | View 메서드(`Awake`, `SetSlotIcon`, `SetSlotCount`, `SetEquipButtonsVisible`)와 `OnEquipModeChanged`에 주석 없음. Presenter 머리 주석 "(뼈대)", `Dispose` 주석, 인터페이스 주석이 현재 내용과 다름 | 팀 컨벤션대로 한 줄 주석 정리 |
| ESC 취소 | `PotionSlotSelector`에 `Update`가 없어 ESC로 취소할 수 없음 | 필요하면 `IsSelecting`일 때 ESC → `CancelSelection()` |
| 코드 정리 | `PotionSlotSelector`에 쓰이지 않는 `TMPro`, `InputSystem` using과 `[MVP 삭제]` 주석 블록 | 동작을 확인한 뒤 팀원과 협의해 삭제 |
| 슬롯 해제 | 장착을 해제할 방법이 없음 (다른 포션으로 덮어쓰기만 가능) | 필요하면 해제 입력 추가 (예: 슬롯 우클릭 → `SetSlot(i, null)`) |
| 중복 장착 | 같은 포션을 여러 슬롯에 넣을 수 있음 | 막으려면 `EquipToSlot`에서 기존 슬롯을 비움 |
| 테스트 씬 | `PotionLoadoutSync`의 `fallbackPlayer`(네트워크 스폰 없는 테스트용)가 사라짐 | 인벤토리 연결은 `LocalPlayerUIBinder`의 스폰 이벤트에만 의존함 |
| 팀 공유 | `PotionSlotSelector`, `LocalPlayerUIBinder`, `GameData/Canvas.prefab`은 팀원 담당 파일 | 머지 전에 변경 내용 공유 |

## 저장/불러오기 연결 시 확인할 점
- **Id로 `PotionData` 찾기**: 저장은 `PotionData.Id`로 하더라도, 불러올 때 Id에 해당하는 `PotionData`를 찾을 조회 수단이 필요하다. `Inventory.Slots`를 복원하는 방식을 확인해서 같은 방식을 쓴다.
- **선택 슬롯 복원**: `PotionLoadout.SelectSlot`은 같은 인덱스면 이벤트 없이 바로 반환한다. 불러오기 전용 메서드를 만들어 값이 같아도 `OnSelectChanged`를 발생시켜야 HUD 선택 프레임이 확실히 맞춰진다.
- **슬롯 복원**: `SetSlot`은 호출할 때마다 `OnEquipChanged`를 발생시키므로, 슬롯별로 호출하면 HUD 아이콘과 수량이 함께 갱신된다.
- **장착 대기 상태는 저장하지 않는다**: `PendingPotion`은 UI 진행 중에만 쓰는 임시 상태다.
