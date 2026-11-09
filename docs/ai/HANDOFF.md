# 프로젝트 인수인계

현재 상태를 실제 파일과 대조하고 요청한 작업부터 진행한다. 관련 [계획](plans/)에 상세 근거를 두고 여기에는 재개에 필요한 요약만 남긴다.

<!-- harness:status:start -->
## 하네스 적용 상태
- 적용한 원본 버전: `1.1.9`
- 기본 경로·버전·직접 패키지·주요 폴더를 확인했다.
- 파일 배치·구조 검사와 실제 훅 신뢰·새 세션 스킬/역할 로딩은 구분한다.
- 게임 컴파일·실행은 이 적용 작업의 확인 대상이 아니다.
<!-- harness:status:end -->
- Claude Code 겸용 레이어를 이 프로젝트에만 추가했다(UnityHarness 원본 미반영). `CLAUDE.md`, `.claude/`, `scripts/ai/sync-claude.ps1`이 해당하며 Codex 파일은 바꾸지 않았다. 스킬·역할은 Codex 원본을 고친 뒤 `sync-claude.ps1`로 다시 생성한다.

## 상태이상 틱 규칙 변경 — 2026-11-09

- 사용자 확정 규칙(즉시 첫 틱, 횟수 보장, 렉 시 몰아서 실행, 재부여 시 틱 타이머 유지)으로 `Tick`을 바꿨다. 기준은 [상태 효과 틱 규칙](PROJECT.md#상태-효과-틱-규칙)이다. 화상 에셋(5초·1초·중첩당 5)의 1중첩 총 피해가 20(4회)에서 25(5회)로 늘었고, 수치 조정은 기획 판단으로 남겼다.
- `dotnet build Assembly-CSharp.csproj`는 오류 0개였고 SJT 경고는 없었다. 독립 정적 리뷰에서 확정 문제는 없었다. PlayMode·실제 틱 로그는 미검증이다.
- 사용자가 `StatusEffectController.OnDisable` 끝에 `modifiers = StatusModifiers.Default;`를 추가했다(비활성 후 재활성 시 이전 수치가 남는 문제).
- 남은 SJT 작업 순서: 복원 시 걸림 연출 재생 억제(Controller의 `IsRestoring` 값을 Visualizer만 확인하는 방식 제안) → `ActiveStatusEffects` 읽기 전용 노출, Behavior 베이스 클래스, Database의 빈 검증 루프 정리. LoadData 중복 방지와 틱 중 비활성화 방어는 현재 호출 경로에서 발생하지 않아 보류했다. 네트워크 전파(입장 후 걸린 상태이상의 동기화, 클라이언트별 화상 피해)는 네트워크 담당 팀원의 결정 사항이다.
- 화상·냉기·빙결 연출 프리팹은 tickFeedbacks가 연결돼 있어 걸리는 프레임에 Registered·WhileActive·Tick 연출이 함께 재생된다. 겹침 여부는 PlayMode에서 확인한다.

## 상태이상 파티클 연출 — 2026-11-06

- 사용자와 정한 1차 범위는 화상·냉기·빙결의 파티클 연출이다. 모델 색·재질 변경과 커스텀 Feedback은 제외한다. 기존 구조를 유지하며 Visualizer에 생성 시 모델 bounds 자동 맞춤만 추가했다. [현재 구조](PROJECT.md#상태-효과-feel-연출)와 현재 계획을 따른다.
- 템플릿은 Registered·Reapplied·Tick·Removed·WhileActive 다섯 Player의 Feedback 목록이 모두 비어 있다. 상태 시작 시 Registered+WhileActive, 재부여·틱은 각 Player, 종료 시 유지 연출 정지 후 Removed를 재생한다.
- 사용할 때 Label이 Target인 MMF Reference Holder를 필요한 Player에 추가하고 개별 Feedback의 대상 선택을 구성한다. Initialize(target)가 대상을 주입하며, 다른 Label은 건드리지 않는다. feedbackTarget이 비어 있으면 대상 자신의 GameObject를 전달한다.
- modelRoot와 bodyColorFeedbacks 필드는 FormerlySerializedAs로 이전한다. 템플릿 GUID와 기존 Player fileID는 보존했다.
- 값 복원·상태 간 연출 충돌 처리는 외부 연출 구성의 책임이다. 종료 Player는 유한하게 구성하고 비활성화·파괴 정리가 필요하면 외부 컴포넌트에서 처리한다.
- SEFeedback_Burn·Chill·Freeze와 대응 VFX를 만들고 세 상태 데이터에 연결했다.
- StatusEffectScene의 기존 Player·Capsule만 fitToModelBounds를 켰다. 원본 플레이어·몬스터 프리팹과 다른 씬은 변경하지 않았다.
- 최초 컴파일·독립 정적 리뷰와 최종 보강 자산의 Play 검사 30개를 통과했다. 최초 부여·재부여·겹침·만료·비활성/복구·모델 재질 불변을 확인했고, 시각 튜닝과 증거는 현재 계획에 있다. 원본 몬스터 적용·다수 대상 성능 측정은 후속이다. 이전 대상 Holder·필드 이전 검증은 기존 범용화 기록을 참고한다.

## 상태이상 작업 인수인계 — 2026-11-03

### 반드시 유지할 사용자 확정 사항

- 같은 대상에게 같은 상태를 다시 부여하면 기존 상태 인스턴스를 사용해 중첩을 증가시키고, **남은 지속 시간을 해당 상태의 전체 지속 시간으로 다시 설정한다.** 예: 화상 1스택·5초 → 만료 전 재부여 → 화상 2스택·남은 시간 다시 5초. 5초는 설명용 예시이며 모든 상태의 고정값이 아니다.
- 화상·냉기·독 등 서로 다른 상태는 각각 자기 `TickSec`와 타이머로 진행한다. 같은 상태의 재부여를 별도 인스턴스로 쌓는 방식이나, 재부여해도 지속 시간을 갱신하지 않는 앞선 에이전트 설명을 구현 기준으로 사용하지 않는다.
- A안인 실행 정보와 행동의 분리를 선택했다. `StatusEffect`는 대상별 중첩·시간 등 실행 정보를, `IStatusBehavior` 구현은 실제 효과를 담당한다. 행동 객체는 대상별 실행 정보를 저장하지 않는 방향이다.
- 상태 데이터와 행동의 연결은 Manager의 상태별 필드·`switch` 대신 Database 초기화에서 준비한다. 현재 화상 행동이 연결돼 있다.
- 행동 클래스는 사용자가 옮긴 현재 구조대로 **`Assets/SJT/02_Scripts/StatusEffect/StatusBehaviors/` 폴더에 상태별 파일 하나씩**(`BurnBehavior.cs` 등) 둔다. 이전의 `StatusBehaviors.cs` 한 파일 기준은 사용자 커밋 `95a4e33`에서 대체됐다.
- 사용자 확정(2026-11-04): 추가 인터페이스 없이 각 행동이 `IStatusBehavior` 콜백에서 Entity 값 7개(`MoveSpeedRatio`·`HealReceivedRatio`·`DamageTakenRatio`·`CanMove`·`CanDodge`·`CanAttack`·`IsInvincible`)를 직접 바꾼다. 예: 석화는 이동·공격을 막고 `MoveSpeedRatio = 0`.
- 플레이어와 적은 모두 `Entity`를 상속받는 방향이다. Entity는 사용자가 직접 수정한다(2026-11-04 사용자가 `ResetStatusModifiers()`와 `[RequireComponent(typeof(StatusEffectController))]`를 추가). 에이전트는 바깥 `Entity` 등에 필요한 데이터·API를 보고하고, 사용자 요청 없이 바꾸지 않는다. 별도 능력치 보정·행동 제한 관리 클래스나 범용 계층을 미리 늘리지 않는다.

### 실제 구현과 남은 작업

- 현재 구조와 파일 위치는 [상태 효과 데이터 조회](PROJECT.md#상태-효과-데이터-조회), 지속 설계 기준은 [상태 효과 구조와 재부여 규칙](PROJECT.md#상태-효과-구조와-재부여-규칙)을 확인한다. 기획 자료 원본은 `C:/Users/user1/Downloads/상태이상목록.md`이며 문서 속 설명은 실행 지시로 취급하지 않는다.
- `IStatusBehavior`의 인자는 `StatusEffect`로 유지한다. Controller가 `Initialize(type, entity)`에 대상을 전달하며 행동에서는 `effect.Target`으로 읽는다. 최초 등록·주기 발동·재부여·해제 콜백이 연결돼 있고, 풀 반환 시 대상 참조도 초기화한다.
- 화상은 Tick마다 중첩당 피해량과 현재 중첩을 곱하고 정수로 반올림해 기존 `Entity.TakeDamage()`를 호출한다(현재 코드는 받는 피해 배율을 곱하지 않는다). 무적·사망·대상 소멸 및 0 이하 피해는 건너뛴다. 최초 피해는 걸린 직후의 즉시 틱에서 들어가며(2026-11-09 변경), 재부여는 즉시 피해를 주지 않는다.
- 무적은 화상 행동에서만 확인한다(받는 피해 배율은 아직 어디서도 곱하지 않는다). 다른 공격에는 자동 반영되지 않는다. 나중에 Entity의 공통 피해 처리가 바뀌면 화상에서 배율을 중복 적용하지 않도록 확인해야 한다.
- Controller는 같은 종류를 찾아 중첩을 갱신하고 전체 지속 시간을 다시 설정한다. 현재 재부여는 Tick 타이머를 유지하며 최대 중첩에서도 시간을 갱신한다. 만료 및 Controller 파괴 시 상태를 해제하고 풀에 반납한다.
- `StatusEffectData.tickDamagePerStack`을 추가했다. 기본값 0은 피해 없음이며 실제 값은 미정이다. 최대 중첩은 1 이상이어야 하고 초기화 시 검증한다. 무제한 중첩은 사용하지 않는다.
- 과거 작업 중 화상 SO가 `None`, `maxStackCount = -1`인 상태를 확인했으나, 2026-11-05의 실제 파일은 `StatusEffects/StatusEffect_Burn.asset`이며 Burn, 최대 중첩 5, 지속 5초, Tick 1초, 중첩당 피해 5다. 에셋 수치는 이번 Feel 연동에서 변경하지 않았다. Manager의 Database 참조와 대상의 Entity 연결도 확인한다.
- Database는 데이터와 행동을 조회한다. 현재 `None` 항목은 진단 후 건너뛰며 빈 참조·중복 종류는 전체 초기화를 실패시킨다. 행동 누락 검사는 주석 상태다.
- Entity 값 변경형 11종을 구현했다(2026-11-04): 냉기·독·점액·젖음·기절·빙결·구속·취약·헤이스트·석화·무적. 값 적용 방식은 아래 겹침 처리로 대체됐다. 수치는 새 `StatusEffectData.valuePerStack`(v): 냉기 이속 1−v×스택, 독 회복 1−v×스택, 점액 이속 1−v·구르기 불가, 젖음 이속 1−v, 기절 이동·공격·구르기 불가, 빙결 기절+이속 0, 구속 이동·구르기 불가+이속 0(공격 허용), 취약 받는 피해 1+v×스택, 헤이스트 이속 1+v×스택, 석화 빙결+무적, 무적. 감소값은 0 미만이면 0으로 맞춘다. `StatusEffectType`은 명시 정수값(None=0 … Invincible=24, 기존 주석 순서)을 쓴다.
- 겹침 처리(사용자 승인 2026-11-04): 위 값 적용을 `IStatusBehavior.ApplyModifier`로 옮겼다. Controller의 `RecalculateModifiers`가 걸림·중첩·만료 때마다 Entity 값 7개를 기본값으로 되돌린 뒤 활성 상태 전체의 `ApplyModifier`를 호출한다. 행동은 배율을 곱하고(`*=`, 빙결·구속·석화의 이속은 `= 0`), 제한은 막기만(`= false`), 무적은 켜기만(`= true`) 하며 `OnRemove`에서 값을 되돌리지 않는다. 그래서 같은 값을 쓰는 상태가 겹쳐도 먼저 끝난 쪽이 남은 효과를 풀지 않는다(예: 냉기 x2 + 헤이스트 x1 → 이속 0.8×1.2). 기본값 복원은 `Entity.ResetStatusModifiers()`(사용자 추가)를 Controller가 호출한다. 이 7개 값은 Controller가 관리하므로 다른 시스템이 직접 쓰면 다음 재계산에서 덮인다. 기존 4개 콜백은 1회성 이벤트 용도로 남겨 두었다.
- 이동·회복·공격 코드는 이 값들을 아직 읽지 않고, `Entity.TakeDamage`는 무적·받는 피해 배율을 반영하지 않는다. 사망 후에도 상태는 진행된다.
- 젖음과 화상의 상쇄, 냉기 최대 중첩→빙결, 저주·저항·침식·부스트·폭파, 중첩의 자연 감소는 구현하지 않았다. 독은 회복 감소이며 지속 피해로 바꾸지 않는다. 구체 수치는 미정이다.

### 검증 상태

- 겹침 재계산(2026-11-04): 임시 csproj 방식 `dotnet build`로 컴파일을 확인한다. Unity PlayMode는 미검증이다. 테스트용 SO 12개(`StatusEffects/StatusEffect_<종류>.asset`, 기존 데이터는 `StatusEffect_Burn`으로 이름 변경)와 Database 목록은 디스크 참조 검사만 통과했다. Controller에 걸림·중첩·틱·종료 디버그 로그(`logStatusEffects`)와 컨텍스트 메뉴가 있다.

- 11종 구현(2026-11-04): Unity 생성 csproj에 이전에 삭제한 `IStatusModifier.cs`가 남아 있어, 그 줄만 뺀 임시 csproj로 `dotnet build`를 실행했다. 오류 0개, SJT 경고 0개였고 임시 파일은 삭제했다. Unity Console·PlayMode·실제 값 변화는 미검증이다.

- 화상 예시 최종 소스의 독립 정적 검토에서 수정이 필요한 확정 오류는 발견하지 못했다. `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet`는 오류 0개·경고 49개로 완료했다. 경고는 이번 수정 밖의 PackageCache·Photon·Imports·abcdcode 경로에서 나왔고 SJT 변경 4개에는 없었다. 검증 후 4개 소스와 Entity 해시가 동일함을 확인했다.
- 첫 컴파일은 검증 캐시 부재로 중단됐다. 루트의 csproj 77개에 PackageReference가 없음을 확인하고, `dotnet restore Assembly-CSharp.csproj --source . --ignore-failed-sources`로 로컬 산출물만 준비한 뒤 재검증했다. 패키지 설치·프로젝트 설정 변경은 하지 않았다. 부모 훅 토큰이 제공되지 않아 자식 검증 영수증은 승계하지 못했고 실제 명령 완료 결과를 근거로 보고한다.
- 이번 요청에는 PlayMode·게임 실행이 포함되지 않아 실제 체력 감소, Inspector 연결 및 Unity에서의 동작은 미검증이다. 이 시점의 시간 처리는 만료를 먼저 검사하고 Tick 초과 시간을 버렸으며, 2026-11-09 틱 규칙 변경으로 대체됐다.

## 다음 작업과 남은 확인

- 샘플 맵은 기존 4개 테마의 모듈러 에셋을 각 지역 씬에서 프리팹으로 조립한다. [확정 방향과 참고 이미지](PROJECT.md#샘플-맵-제작-방향-쿼터뷰)를 따른다. 팀원 카메라 구도를 기준으로 소폭 시점 변화에도 동선·전투 공간이 잘 읽히게 구성한다.
- 새 맵 기획·재디자인은 Plan 모드에서 함께 질문·답변으로 정한 뒤 초안 그림을 보여주고 피드백을 반영한다. 맵 조립 스킬에 해당 절차를 추가했으며, 기존에 확정한 기획과 실행 요청을 다시 논의하도록 되돌리지는 않는다.
- [지역 연결 구조](PROJECT.md#샘플-맵-지역-연결-구조)는 중앙 숲과 Snowy Mountain·Swamp·Volcano의 양방향 연결이며, 문짝 없이 뚫린 석조 문을 통과해 전환한다. 바깥 세 지역 사이의 직접 연결은 없다. 씬은 분리됐으나 실제 로드 로직은 미정이다.
- 통합 `MainGameScene.unity`의 숲 서쪽과 화산 사이 접합부를 연결했다. 기존 지역별 NavMesh는 후속 재베이크 대상이다. 배치와 검증은 숲·화산 연결부 기록을 참고한다.
- [지역 내부 구성과 제작 범위](PROJECT.md#샘플-맵-지역-내부-구성과-제작-범위)는 평지 위 여러 공터와 연결길, 2~4인 전투 공간, 바깥 지역의 조건 없이 접근하는 계단 위 보스 공간이다. 이번 샘플은 공간·장식·배치 표식까지이다.
- `Forest.unity`에 중앙 허브·작은 전투 공터 2개·채집 표식·돌문 3개·탐험 동선의 초안을 배치했다. 모듈은 `Assets/SJT/03_Prefabs/Map/Forest/`에 정리했다. 현재 상태와 검증은 Forest 초안 계획을 확인한다. 자갈 무늬 바닥의 교체와 절차적 생성은 논의만 했고 Forest 자산은 그대로 유지했다.
- `Volcano.unity`의 최신 결과는 `Volcano_Assembled`이며 참고 이미지 기반 제작 기록을 따른다. 연속 암반·폐허·일반 용암 3곳, 중앙 28단 계단 위 보스 광장·구멍 2곳·균열 2곳·뿔 10개, 귀환 아치 1개와 표식 24개를 배치했다. 새 자산은 SJT Volcano의 `Assembled/`에 있고 기존 Main Camera를 보존했다.
- 제작 도중 NUL 손상으로 `VolcanoMapAuthoring.cs` 오류가 발생했다. 사용자 승인으로 손상된 도구와 `ReferenceBuild`를 삭제하고 제작 전 백업에서 복구한 뒤 Unity 컴파일 통과를 확인했다. 이후 프로젝트에 제작용 C# 파일을 추가하지 않고 재제작했다. 복구 사본은 `Backups/Volcano_BeforeReferenceBuild_20261104.unity`이며 원래 씬 meta를 유지했다. 새 맵의 최종 참조·물리·저장 검사와 실제 렌더는 위 제작 기록을 따르며 이전 결과를 현재 검증으로 재사용하지 않는다. 실제 플레이어·NavMesh·용암 피해·지역 전환은 후속 범위이다.
- 상태이상은 위 인수인계의 확정 규칙·현재 구현·미정 사항을 기준으로 이어간다. 데이터 에셋 등록·씬 연결이 완료됐다고 가정하지 않는다.
- `Swamp.unity`에 승인한 늪 숲과 거목 광장을 저장했다. 루트는 `Swamp_Assembled`이며 공터 3곳과 순환길, 얕은 웅덩이 4곳, 남쪽 돌문 하나, 판자 계단과 높이 4m의 보스 광장, 배치 표식 20개가 있다. 실제 플레이 이동 시간, 스폰, 물 이동 효과, 지역 전환은 후속 범위이다.
- `Snowy Mountain.unity`에 설원과 암봉 광장을 저장했다. 루트는 `SnowyMountain_Assembled`이며 돌문 하나, 설원 공터 3곳, 좌우 길과 가로 연결로, 18단 계단 위 4.5m 광장과 표식 20개가 있다. 실제 이동 시간, 지역 전환, 눈의 게임 효과는 후속 범위이다.
- Snowy Mountain의 URP 셰이더·연기·별·눈 파티클 수정과 관련 컴파일·대표 렌더 검증을 완료했다. 과거 Synty Dungeon Map의 기반 팩 누락 조사는 작업 계획에 남아 있지만 이번 Volcano 경로에는 해당 팩이 없고 초안에도 사용하지 않았다.
