# PopupUI 사용 가이드

> 인벤토리, 제작창, 레시피북처럼 **켰다 껐다 하는 창(팝업)** 을 한 곳에서 관리하는 방법을 정리한 문서입니다.
> 팝업 스크립트가 `PopupUI`를 상속하면, ESC로 맨 위 창부터 닫기와 마지막에 연 창을 맨 앞에 그리기가 자동으로 됩니다.
>
> 기준: 2026-11-13, `feat/kwon/player` 브랜치

---

## 1. 왜 쓰나요?

지금은 팝업마다 `gameObject.SetActive(...)`로 각자 켜고 끕니다. 그래서 **어떤 창이 어떤 순서로 열려 있는지 아는 곳이 없습니다.** 그 결과 다음과 같은 일을 할 수 없습니다.

- ESC를 누르면 가장 마지막에 연 창만 닫기
- 팝업이 하나라도 열려 있으면 ESC 메뉴 대신 팝업 닫기
- 새로 연 창을 다른 창 위에 그리기

`PopupUIManager`가 열린 팝업을 **스택처럼** 기억하고, 팝업은 `PopupUI`를 상속해 매니저를 통해서만 열고 닫습니다.

### 관련 파일
| 파일 | 역할 |
|---|---|
| `KMS/Scripts/UI/PopupUI.cs` | 모든 팝업의 부모 클래스. `Open` / `Close` / `Toggle`, 열기·닫기 훅 |
| `KMS/Scripts/UI/PopupUIManager.cs` | 열린 팝업 목록(스택) 관리. 맨 위 닫기, 모두 닫기 |
| `KMS/Scenes/PopupUITestScene.unity` | 동작 확인용 테스트 씬 |
| `KMS/Scripts/UI/Test/PopupUITestPopup.cs`, `PopupUITester.cs` | 테스트 씬 전용 스크립트 (참고용 예시) |

---

## 2. 빠른 시작 (3단계)

### ① 씬에 매니저 배치
1. Hierarchy에 **빈 GameObject**를 만들고 이름을 `PopupUIManager`로 짓습니다.
2. `PopupUIManager` 컴포넌트를 붙입니다.

> ⚠️ **Canvas나 다른 오브젝트에 붙이지 마세요.** 매니저는 `SingletonBehavior`를 상속합니다. 씬에 매니저가 두 개 생기면 나중 것을 **GameObject째로 Destroy**하므로, Canvas에 붙어 있으면 UI 전체가 사라집니다.

### ② 팝업 스크립트가 `PopupUI`를 상속
```csharp
public class RecipeBookUI : PopupUI   // MonoBehaviour 대신 PopupUI
{
    ...
}
```
`PopupUI`가 `MonoBehaviour`를 상속하고 있어서 기존 코드는 그대로 동작합니다.

### ③ 열고 닫을 때는 `Open()` / `Close()` / `Toggle()`만 사용
```csharp
recipeBook.Open();
recipeBook.Close();
recipeBook.Toggle();
```
**`gameObject.SetActive(true/false)`로 직접 켜고 끄지 마세요.** 이유는 [5. 꼭 지켜야 할 규칙](#5-꼭-지켜야-할-규칙)에 있습니다.

---

## 3. API 정리

### `PopupUI` (팝업이 상속하는 클래스)
| 멤버 | 설명 |
|---|---|
| `void Open()` | 팝업을 엽니다. 이미 열려 있으면 맨 위(맨 앞)로 올리기만 합니다 |
| `void Close()` | 팝업을 닫습니다. 스택 중간에 있어도 그 창만 닫힙니다 |
| `void Toggle()` | 열려 있으면 `Close()`, 닫혀 있으면 `Open()`. 단축키에 사용합니다 |
| `bool IsOpen` | 지금 열려 있는지(`gameObject.activeSelf`) |
| `protected virtual void OnOpened()` | **열린 직후** 호출되는 훅. 화면 갱신 등에 사용합니다 |
| `protected virtual void OnClosed()` | **닫히기 직전** 호출되는 훅. 정리 작업(재료 반환, 툴팁 숨기기 등)에 사용합니다 |
| `bringToFrontOnOpen` (인스펙터) | 열 때 맨 앞에 그릴지 정합니다. 기본값은 켜짐. [4-6](#4-6-맨-앞으로-올리지-않을-팝업) 참고 |

### `PopupUIManager` (`PopupUIManager.Instance`로 접근)
| 멤버 | 설명 |
|---|---|
| `bool TryCloseTop()` | 맨 위 팝업을 닫습니다. 닫았으면 `true`, 열린 팝업이 없으면 `false`. **ESC 처리용** |
| `void CloseAll()` | 열린 팝업을 위에서부터 모두 닫습니다(씬 전환, 사망 등) |
| `bool HasOpenPopup` | 팝업이 하나라도 열려 있는지 |
| `PopupUI TopPopup` | 맨 위(가장 마지막에 연) 팝업. 없으면 `null` |

`PopupUIManager`의 `Open(popup)` / `Close(popup)`는 `PopupUI.Open()` / `Close()`가 내부에서 부르는 메서드입니다. **팝업 쪽 메서드를 쓰세요.**

### 호출 순서
```
Open()  : SetActive(true) → (OnEnable) → 맨 앞으로 올리기 → OnOpened()
Close() : OnClosed() → SetActive(false) → (OnDisable)
이미 열린 팝업에 Open() : 맨 앞으로 올리기만 함 (OnOpened 다시 안 불림)
```

---

## 4. 예시 코드

### 4-1. 가장 단순한 팝업
상속만 바꾸고, 기존 `Close()`는 **삭제**합니다. 부모의 `Close()`가 같은 이름이라 인스펙터의 닫기 버튼 연결도 그대로 유지됩니다.

```csharp
// Before
public class RecipeBookUI : MonoBehaviour
{
    private void OnEnable()
    {
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}

// After
public class RecipeBookUI : PopupUI
{
    // 열릴 때마다 페이지를 다시 그린다
    protected override void OnOpened()
    {
        Refresh();
    }

    // Close()는 삭제. PopupUI.Close()가 대신한다
}
```

> 기존 `OnEnable`에서 하던 갱신은 그대로 둬도 됩니다. `OnOpened`로 옮기면 "팝업이 열릴 때"라는 의도가 더 분명해집니다.

### 4-2. 닫힐 때 정리가 필요한 팝업 (`OnClosed`)
제작창처럼 닫을 때 **투입한 재료를 돌려줘야 하는** 팝업은 그 로직을 `OnClosed()`에 둡니다. 그러면 닫기 버튼, 단축키, ESC, 범위 이탈 중 **어떤 경로로 닫혀도** 재료가 돌아옵니다.

```csharp
public class PotionCrafting : PopupUI
{
    // 닫히기 직전, 투입한 재료를 인벤토리로 돌려준다
    protected override void OnClosed()
    {
        ReturnInsertedMaterials();
        RefreshUI();
    }

    // 투입한 재료를 인벤토리로 돌려주고 기운을 초기화한다
    private void ReturnInsertedMaterials()
    {
        if (insertedMaterials.Count > 0 && inventory == null)
        {
            Debug.LogWarning("재료를 반환할 인벤토리가 연결되지 않았습니다.", this);
            return;
        }

        List<MaterialData> materialsToReturn = new List<MaterialData>(insertedMaterials);
        insertedMaterials.Clear();
        // ... 기운 초기화 ...

        foreach (MaterialData material in materialsToReturn)
            inventory.AddItem(material.Id, 1);
    }

    // 기존 Close()는 삭제. ResetUi()도 ReturnInsertedMaterials()를 재사용하면 된다
}
```

> ⚠️ `OnClosed()`에서 `return`해도 **닫기는 취소되지 않습니다.** 훅이 끝나면 팝업은 항상 꺼집니다. 기존 `Close()`처럼 "조건이 안 맞으면 안 닫기"가 필요하면 `PopupUI` 담당자와 상의하세요(`CanClose()` 같은 훅 추가 필요).

### 4-3. 단축키로 열고 닫기 (`Toggle`)
필드 타입을 `GameObject`에서 **팝업 클래스**로 바꾸고 `Toggle()` / `Close()`를 호출합니다.

```csharp
public class OpenUI : MonoBehaviour
{
    [SerializeField] private RecipeBookUI recipeBook;      // GameObject → RecipeBookUI
    [SerializeField] private PotionCrafting craftingUI;
    [SerializeField] private InventoryUI inventoryUI;

    // 레시피북을 열고 닫는다
    public void ToggleRecipeBook()
    {
        if (recipeBook != null)
            recipeBook.Toggle();
    }

    // 제작창을 닫는다 (솥에서 멀어졌을 때 등)
    public void CloseCraftingUI()
    {
        if (craftingUI != null)
            craftingUI.Close();
    }
}
```

> 인스펙터에서 필드 타입이 바뀌면 기존 연결이 끊길 수 있습니다. 바꾼 뒤 **팝업 오브젝트를 다시 드래그해서 연결**하세요.

`Toggle()`은 **열려 있으면 닫습니다.** 인벤토리가 다른 창 아래에 깔려 있어도 `I`를 누르면 인벤토리만 닫힙니다(위에 있는 창은 그대로).

### 4-4. ESC 처리
ESC는 **한 곳에서만** 읽어야 합니다. 두 스크립트가 같은 프레임에 ESC를 처리하면, "팝업 닫기"와 "ESC 메뉴 열기"가 동시에 일어납니다.

```csharp
public class ESCPanel : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // 열린 팝업이 있으면 맨 위 하나만 닫고 끝
        if (PopupUIManager.Instance != null && PopupUIManager.Instance.TryCloseTop())
            return;

        // 열린 팝업이 없을 때만 ESC 메뉴를 토글
        Toggle();
    }
}
```

### 4-5. 버튼에서 열고 닫기 (인스펙터)
Button의 **On Click ()** 에 팝업 오브젝트를 드래그하고 함수를 고릅니다.

| 하고 싶은 것 | 선택할 함수 |
|---|---|
| 닫기(X) 버튼 | `PopupUI` → `Close ()` |
| 다른 팝업 열기 | 그 팝업 오브젝트의 `PopupUI` → `Open ()` |
| 열기/닫기 전환 | `PopupUI` → `Toggle ()` |

> 하위 클래스(예: `RecipeBookUI`)를 골라도 목록에 `Close ()`가 보입니다. 같은 함수입니다.
> **`GameObject` → `SetActive (bool)`은 고르지 마세요.**

### 4-6. 맨 앞으로 올리지 않을 팝업
`Open()`할 때 팝업을 형제 중 **맨 아래(= 맨 앞에 그려짐)** 로 옮깁니다(`SetAsLastSibling`). 그런데 Layout Group 안에 있는 패널처럼 **Hierarchy 순서가 곧 화면 위치**인 경우에는 순서가 바뀌면 안 됩니다. 이럴 때 인스펙터에서 **`Bring To Front On Open`을 끄세요.** 스택(ESC 순서)에는 그대로 들어가고, 그리는 순서만 바뀌지 않습니다.

### 4-7. 팝업이 열려 있는지 확인
```csharp
// 팝업이 열려 있으면 월드 클릭을 무시하는 예
if (PopupUIManager.Instance != null && PopupUIManager.Instance.HasOpenPopup)
    return;
```

---

## 5. 꼭 지켜야 할 규칙

### ❌ `SetActive`로 직접 켜고 끄지 않기
```csharp
inventoryUI.gameObject.SetActive(false);   // ❌ 매니저가 모름
inventoryUI.Close();                       // ✅
```
직접 끄면 매니저 목록과 실제 화면이 어긋납니다. 매니저가 다음 조회 때 꺼진 항목을 정리하긴 하지만, 그 경우 **`OnClosed()`가 불리지 않습니다.** 제작창이라면 재료가 반환되지 않습니다.

### ❌ 하위 클래스에서 `Open()` / `Close()`를 새로 정의하지 않기
```csharp
public class InventoryUI : PopupUI
{
    public void Close() { gameObject.SetActive(false); }   // ❌ 부모의 Close를 가림 → 매니저 우회
}
```
열기·닫기 때 할 일은 **`OnOpened()` / `OnClosed()`를 override** 해서 넣으세요.

### ❌ `Show()` / `Hide()` / `BringToFront()`를 직접 호출하지 않기
매니저 전용 메서드입니다. 프로젝트가 한 어셈블리라서 `internal`이어도 호출은 되지만, 부르면 목록이 어긋납니다.

### ✅ 팝업들은 같은 부모 아래에 두기
맨 앞으로 올리기(`SetAsLastSibling`)는 **같은 부모 아래 형제끼리만** 순서를 바꿉니다. 팝업들이 서로 다른 부모나 서로 다른 Canvas 아래에 있으면 겹쳐 그리는 순서가 바뀌지 않습니다.

```
Canvas
└─ PopupLayer          ← 팝업 전용 부모
   ├─ InventoryUI
   ├─ RecipeBookUI
   └─ CraftingUI
```

### ✅ 팝업은 꺼진 상태로 시작해도, 켜진 상태로 시작해도 됩니다
따로 등록하는 과정이 없습니다. 꺼진 팝업은 `Open()`할 때 목록에 들어가고, 씬에서 **켜진 채 시작한 팝업은 매니저의 `Start()`에서 자동으로 목록에 등록**됩니다(이때 `OnOpened()`는 불리지 않습니다). 자동 등록된 팝업들 사이의 순서는 정해져 있지 않습니다.

### ✅ `OnClosed()` 안에서 `Close()`를 불러도 안전합니다
닫는 중에 같은 팝업의 `Close()`가 다시 불리면 무시됩니다. `OnClosed()`에서 예외가 나도 팝업은 꺼집니다. `OnClosed()`에서 **다른** 팝업을 닫아도 `CloseAll()`은 문제없이 동작합니다.

### `OnEnable` / `OnDisable`은 하위 클래스에서 자유롭게 써도 됩니다
`PopupUI`는 `OnEnable` / `OnDisable`을 쓰지 않습니다. 기존 `InventoryUI`의 이벤트 구독·해제 코드는 그대로 두면 됩니다.

---

## 6. 스택 동작 (`Stack<T>`가 아니라 `List`인 이유)

ESC를 누르면 **가장 마지막에 연 창부터** 닫힙니다(LIFO). 다만 우리 팝업들은 여러 개가 동시에 열리고 각자 단축키로 꺼지므로, **스택 중간의 창을 빼야 하는 경우**가 있습니다. `Stack<T>`는 중간 원소를 뺄 수 없어서, 내부는 `List`로 두고 맨 뒤를 top으로 씁니다.

| 상황 | 목록 (오른쪽이 맨 위) | 결과 |
|---|---|---|
| I(인벤) → R(레시피) | `[인벤, 레시피]` | 레시피가 인벤 위에 그려짐 |
| 이어서 ESC | `[인벤]` | 레시피만 닫힘 |
| I → R → I | `[레시피]` | **중간의 인벤만** 닫힘 |
| 인벤, 레시피가 열린 상태에서 인벤을 다시 `Open()` | `[레시피, 인벤]` | 인벤이 맨 위로 올라옴 (`OnOpened` 다시 안 불림) |
| 제작 → 인벤 상태에서 솥과 멀어짐 | `[인벤]` | 제작창만 닫히고 재료 반환 |
| ESC를 계속 누름 | `[]` | 다 닫힌 뒤 ESC 메뉴가 열림 |

---

## 7. 테스트 씬으로 확인하기

`Assets/KMS/Scenes/PopupUITestScene.unity`를 열고 Play합니다.

| 입력 | 동작 |
|---|---|
| `1` ~ `4` | 팝업 A~D 토글 |
| `Esc` | 맨 위 팝업 닫기 |
| `X` | 모두 닫기 |
| 팝업 안 `닫기` | `Close()` |
| 팝업 안 `직접 끄기` | `SetActive(false)`로 매니저 우회 (규칙 위반 상황 재현) |
| 팝업 안 `X Open` | 다른 팝업 `Open()` (이미 열려 있으면 맨 앞으로) |

- 왼쪽 위 패널에 `HasOpenPopup`, `TopPopup`, 팝업별 열림 여부·sibling 순서·훅 호출 횟수가 표시됩니다.
- Popup D는 `Bring To Front On Open`이 꺼져 있습니다. 열어도 다른 창 뒤에 그려지지만, ESC를 누르면 먼저 닫힙니다.
- `PopupUITestPopup.cs`는 `OnOpened` / `OnClosed` override 예시로 참고할 수 있습니다.

---

## 8. 기존 팝업 적용 체크리스트

| 스크립트 | 할 일 |
|---|---|
| `GameData/Scripts/Inventory/InventoryUI.cs` | `: PopupUI` 상속, `Close()` 삭제. `OnEnable` / `OnDisable`은 그대로 |
| `GameData/Scripts/RecipeBookUI.cs` | `: PopupUI` 상속, `Close()` 삭제 |
| `GameData/Scripts/Crafting/PotionCrafting.cs` | `: PopupUI` 상속, `Close()`의 재료 반환을 `OnClosed()`로 이동한 뒤 `Close()` 삭제 ([4-2](#4-2-닫힐-때-정리가-필요한-팝업-onclosed)) |
| `GameData/Scripts/OpenUI.cs` | 필드 타입을 각 팝업 클래스로 바꾸고, `SetActive` 토글을 `Toggle()` / `Close()`로 교체 ([4-3](#4-3-단축키로-열고-닫기-toggle)) |
| `abcdcode/Script/ESCPanel.cs` | ESC 입력 시 `TryCloseTop()` 먼저 확인 ([4-4](#4-4-esc-처리)) |
| 씬 | 전용 GameObject에 `PopupUIManager` 배치, 팝업들을 같은 부모 아래로 정리 |
| 인스펙터 | 닫기 버튼 On Click이 `SetActive`로 연결돼 있으면 `Close ()`로 교체. `OpenUI` 필드 다시 연결 |

적용한 뒤에는 [6. 스택 동작](#6-스택-동작-stackt가-아니라-list인-이유)의 표대로 직접 눌러서 확인해 주세요.
