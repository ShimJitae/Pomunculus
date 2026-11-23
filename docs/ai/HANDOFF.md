# 프로젝트 인수인계

현재 상태를 실제 파일과 대조하고 요청한 작업부터 진행한다. 관련 [계획](plans/)에 상세 근거를 두고 여기에는 재개에 필요한 요약만 남긴다.

<!-- harness:status:start -->
## 하네스 적용 상태
- 적용한 원본 버전: `1.2.0`
- 기본 경로·버전·직접 패키지·주요 폴더를 확인했다.
- 파일 배치·구조 검사와 실제 훅 신뢰·새 세션 스킬/역할 로딩은 구분한다.
- 게임 컴파일·실행은 이 적용 작업의 확인 대상이 아니다.
<!-- harness:status:end -->
- Claude Code 겸용 레이어를 이 프로젝트에만 추가했다(UnityHarness 원본 미반영). `CLAUDE.md`, `.claude/`, `scripts/ai/sync-claude.ps1`이 해당하며 Codex 파일은 바꾸지 않았다. 스킬·역할은 Codex 원본을 고친 뒤 `sync-claude.ps1`로 다시 생성한다.

## 월드맵 UI 연출 — 2026-11-23

- WorldMapUI의 MapBG 이동·회전과 연속 입력 전환을 구현하고 공용 프리팹에 연결했다. PlayerFSM의 Tab 판단 한 곳은 사용자 승인으로 SJT 밖에서 수정했다.
- 실제 화면 확인은 수행하지 않으며 속도·기울기·화면 밖 퇴장은 수동 확인 대상이다. 진행과 검증 결과는 [작업 기록](plans/2026-11-23-worldmap-ui-transition.md)을 참고한다.

## 계절별 Forest 장식 레이어 — 2026-11-20

- `Assets/SJT/03_Prefabs/Map(New)/Seasonal/`에 Forest 장식 레이어 4개(봄·여름·가을·겨울)가 있다. 생성 도구는 `Assets/SJT/02_Scripts/Editor/Season/SeasonalForestLayerBuilder.cs`(메뉴 `Tools/Season`)다.
- 4차 요청을 반영했다
  - 외곽 암벽과 보스 고지는 네 계절 모두 봄(기본) 그대로 둔다.
  - 계절 덮개는 숲 바닥의 약 70%만 덮는다. 바닥을 약 22m 칸으로 나눠 무작위로 덮어 기본 숲과 골고루 섞인다(5차). 가을은 황토 땅만 쓴다.
  - 렌더 측정 결과 숲 바닥 변화는 봄 74%, 여름 69%, 가을 70%, 겨울 75%다.
  - 겨울 남색 판(Arctic_Ground 변형에 붙은 자식 수면 메시)을 제거했다.
  - 나무·돌은 채집 오브젝트용이라 장식에 넣지 않는다.
- `World_PurePoly.prefab`에서 숲 양쪽 연못을 없애고 땅으로 채웠다. Forest의 PondTrim 바닥 13개를 원래 메시로 되돌리고, Basin/PondBed, PondWater, 물결 FX를 삭제했다. 이후 사용자가 NavMesh를 다시 구웠다.
- 검사 결과는 Collider 0, Static 0, 마커 여백 위반 0이다. 커밋하지 않았다.
- 런타임 전환(2026-11-21): `World_PurePoly/SeasonalLayers`의 `SeasonalLayerController`(`Assets/SJT/02_Scripts/Season/`)가 계절 이벤트를 받아 레이어를 Addressables로 불러와 붙인다.
  - 처음에는 `IsReady`를 한 번 기다려 `CurSeason`을 적용한다. 페이드·미리 만들기는 넣지 않았다(사용자 결정). 이전 레이어는 끈 뒤 여러 프레임에 나눠 파괴하고 해제한다.
  - Addressables 기본 설정(`Assets/AddressableAssetsData/`)을 만들고 레이어 4개를 Default Local Group에 등록했다. 그 결과 `EditorBuildSettings.asset`에 config 1줄이 추가됐다.
  - PlayMode(에디터, 2026-11-21): 첫 적용(봄)과 봄→여름→가을→겨울→봄 전환, 같은 프레임 연속 변경(마지막 계절만 붙음)을 확인했다. 레이어는 항상 1개, 남은 Addressables 작업은 활성 레이어 1개뿐이다.
  - 처음 불러오는 계절은 에디터 Use Asset Database 모드에서 교체 직전 한 프레임이 약 0.6~1초 멈춘다.
  - Addressables 번들 기준(Use Existing Build)에서는 같은 경우 한 프레임이 약 100~120ms 늘어나는 정도였다. 번들은 4계절이 하나(약 18.6MB)로 묶인다. 측정 후 Play Mode 설정은 Use Asset Database로 되돌렸다. 실제 플레이어 빌드에서는 측정하지 않았다.
  - 확인 중 기존 문제 발견: `Assets/Resources/NetworkPrefabCatalog.asset`의 `m_entries` 7개가 에디터에서 로드되지 않는 프리팹(광맥 Variant, habinImports 아이템 등)을 가리켜 `GameManager.Awake`가 중간에 멈추고 월드가 시작되지 않는다. PlayMode 확인은 월드 데이터를 직접 넣어 진행했다.
- 아직 하지 않은 것
  - BaseCamp 레이어
  - PlayMode와 성능 확인(교체 프레임 끊김 포함), 빌드에서 Addressables 콘텐츠 포함 확인
- 과거 상세 기록 경로는 `plans/2026-11-20-seasonal-forest-layers.md`이며, 현재 작업 폴더에는 해당 파일이 없다.

### 계절 바닥 런타임 정적 배칭 — 2026-11-22

- `SeasonalLayerController`가 각 계절 생성 완료와 최신 요청 여부를 확인한 뒤 `Cover/Ground` 하위 바닥 모듈에 `StaticBatchingUtility.Combine`을 한 번 호출하도록 추가했다. 바닥 모듈은 봄 193개·여름 153개·가을 176개·겨울 171개다. 원본 FBX 16종의 Read/Write는 이미 켜져 있어 프리팹·모델 임포트 설정은 변경하지 않았다.
- 배칭 대상 경로가 없거나 읽을 수 없는 메시가 있으면 배칭을 생략하고, 배칭 예외가 계절 교체·이전 레이어 정리를 막지 않도록 처리했다. 상세 구조는 [프로젝트 안내](PROJECT.md#계절별-forest-장식과-바닥-배칭)를 따른다.
- 최종 수정본의 Unity 컴파일과 독립 코드 리뷰를 통과했다. AssetDatabase 조회에서 네 계절 바닥 693개의 메시 읽기 가능·활성 Renderer·배칭 허용 머티리얼을 확인했다. 이번 요청은 정적 배칭까지만이며 Occlusion 씬 설정·베이크와 페이드는 진행하지 않았다. PlayMode의 실제 배칭 여부와 성능·메모리 회수는 미검증이다.

### 계절 바닥 전환용 머티리얼 준비 — 2026-11-22

- 사용자가 만든 `Assets/SJT/01_Assets/Materials/`의 `Spring_Fade`, `Summer_Fade`, `Authom_Fade`, `Winter_Fade`를 보정했다. 봄·여름의 Base Map을 각 원본 계절 팔레트로 바꾸고, 네 머티리얼의 Metallic 0·Smoothness 0.2·알파 1과 반사광 보존을 끈 Alpha 블렌딩을 맞췄다. 이름과 GUID는 유지했다.
- 머티리얼 준비 단계에서는 파일만 수정했으며 이름과 GUID를 유지했다. 뒤의 직접 구현 요청에서 컨트롤러의 페이드 코드와 Inspector 연결을 추가했다. 연결 순서는 봄·여름·가을(`Authom_Fade`)·겨울이다.
- 준비 직후의 Editor 조회에서는 네 머티리얼이 미저장 상태였으나, 구현 전 재조회에서 모두 `IsDirty=false`이고 디스크와 텍스처·알파·질감·투명 설정이 일치함을 확인했다. 실제 페이드 외형은 아직 확인하지 않았다.

### 계절 바닥 교차 페이드 구현 — 2026-11-22

- 기존 `SeasonalLayerController`에 바닥만 2초 교차 페이드하는 코드와 `fadeDuration`, `groundFadeMaterials`를 추가하고 `World_PurePoly/SeasonalLayers`에 기존 머티리얼 4개를 연결했다. 새 스크립트·컴포넌트·ICullingTickable·Shader Graph는 추가하지 않았다.
- 첫 입장은 즉시 표시한다. 이후 전환에서는 바닥을 알파 0으로 준비한 뒤 새 장식을 표시하고, 바닥의 공유 머티리얼 복제본 두 개만 갱신한다. 원본 복구 후 바닥 배칭과 이전 레이어의 단계별 파괴·해제를 수행한다. Occlusion과 씬·ProjectSettings는 변경하지 않는다.
- 기존 월드 루트에도 동일 컨트롤러가 남아 있어 루트 쪽은 삭제하지 않고 비활성화했으며, SeasonalLayers 쪽 하나에만 시간과 머티리얼을 연결했다. 프리팹과 현재 씬의 실제 로드값도 일치한다. 최종 Unity 재컴파일·독립 리뷰·바닥과 머티리얼 조건 검증을 통과했다. PlayMode·성능·메모리 회수는 미검증이다. 상세 범위와 검증 상태는 [구현 계획](plans/2026-11-22-seasonal-ground-fade.md)을 따른다.
- 마무리 중 월드 프리팹과 IntegrationTestScene에 별도의 Occluder 추가 변경이 저장된 것을 확인했고 그대로 보존했다. 마지막 Editor 조회에서 씬은 `isDirty=true`이며 저장하지 않았다. 이 추가 변경의 실행·외형은 이번 페이드 검증 범위에 포함하지 않았다.

## World_PurePoly 지역·통로 외곽 암벽 MeshCollider 복구 — 2026-11-19

- 5개 지역 외곽 암벽 166개와 현재 Seam_Walls 23개에 이어 설산 보스 뒤 SnowyMountain/Map/08_정상_암봉의 큰 암벽 14개도 시각 메시와 같은 non-convex MeshCollider로 되돌렸다. World 내부의 Box 제거·Mesh 추가 Override만 저장했고 개별 Terrain Variant와 Imports는 유지한다. 설산 RimDecor의 나무 7개·작은 장식 바위 7개와 바닥 박스 9개·보스 고지 Mesh 4개는 변경하지 않았다.
- 현재 전체 수량은 Box 481 / Mesh 207 / Sphere 3개다. 이전 외곽 166개·통로 23개와 설산 정상 암벽 14개 모두 독립 검증을 마쳤다. 정상 암벽의 실제 Collider.Raycast 42건도 통과했고 나무 7개의 Collider·Renderer·배치와 작은 바위 7개는 기준과 동일하다. 채집·몬스터 마커 및 현재 통로 배치를 보존했고 씬·NavMesh는 저장하지 않았다. 실제 플레이 이동과 성능은 미검증이다. 대상·구현·검증 기록(`plans/2026-11-19-world-purepoly-boundary-mesh-colliders.md`, 로컬 전용)

## World_PurePoly 추가 겹침 수정 — 2026-11-19

- 숲 동·서 연못 시각 지면 5개만 ±1cm, 설산·화산 계단 +1cm, 늪 계단 약 +1.692cm 보정했다. 늪 수면 4개·화산 용암면 3개는 중앙 fan의 연결 인덱스만 수정했다. 기존 지역별 바닥 박스와 보스 고지 MeshCollider·기존 숲 계단 보정은 유지한다.
- 동일 평면 겹침 및 수면 내부 겹침 제거를 기하·독립 리뷰로 확인했다. 계단 사면 관통·투명한 숲 연못 아래 지형의 삼각형 모양은 남아 있으며, 실제 캐릭터 이동과 파도 애니메이션은 PlayMode 미검증이다. 대상·검증과 남은 범위는 수정 기록(`plans/2026-11-19-world-purepoly-overlap.md`, 로컬 전용)을 따른다.
- (2026-11-20 갱신: 숲 동·서 연못은 이후 제거됐다. 위의 숲 연못 지면 보정과 아래 연못 NavMesh 표본은 이제 해당하지 않는다. 연못 자리 NavMesh는 다시 구워야 한다.)
- 네 지역 계단 양방향 24경로·연못 NavMesh 30표본은 수정 전과 정확히 같고 NavMesh·씬 파일·meta 해시도 유지됐다. 최종 Editor의 미저장 씬 상태는 해제·저장하지 않았다.

## Forest 보스 계단 접합부 — 2026-11-19

- `World_PurePoly`의 `Forest/Map/07_FinalBossPlateau/Stair/Stair`만 위로 1cm 이동했다(local Y `0.751289` → `0.761289`). Collider 68개도 함께 이동했으며, Scene 뷰의 근거리·일반 거리·낮은 각도에서 마지막 단·상부참의 동일 평면 겹침이 분리된 것을 확인했다.
- `IntegrationTestScene`의 기존 NavMesh 데이터·참조와 씬 파일 해시는 유지됐다. 계단 좌·중·우에서 광장까지 양방향 경로 6개가 모두 `PathComplete`이고, 샘플 좌표와 경로 코너도 수정 전과 같다.
- 30~32번째 단의 고지 사면 관통은 승인한 범위에 따라 남겼다. NavMesh 재베이크와 씬 저장은 하지 않았으며, 실제 캐릭터 이동·발 위치·걸림은 PlayMode 미검증이다.

## World_PurePoly 지역별 바닥 박스·보스 고지 충돌 — 2026-11-19

- 일반 바닥·연결 통로의 Box 제거 Override를 유지하면서 `GroundCollision` 아래의 지역 5개·통로 4개별 얇은 BoxCollider로 바꿨다. 앞서 만든 통합 메시 Collider와 자산·meta는 제거했다. 사용자 요청에 따라 물 구멍도 받치며, 보스 고지 본체 4개는 시각 메시를 공유하는 MeshCollider다. 현재 수량은 Box 686 / Mesh 4 / Sphere 3개다.
- 개별 지형 Variant·Imports·시각 메시·씬 파일·NavMesh와 위 Forest 계단의 1cm 보정을 유지한다. 일반 바닥의 1m 격자 평균 오차 22.1cm·95백분위 60.8cm이며 정점·격자 표본 최대 차이는 약 3.94m다. 독립 통계 재계산과 실제 Box Raycast 1,089건이 일치했고 NavMesh 대표 552건에서 새 미검출은 없었다. 실제 이동·포션 착탄·성능은 미검증이다. 최종 구성·오차 기록(`plans/2026-11-19-world-purepoly-flat-box-floors.md`, 로컬 전용)

## World_PurePoly BoxCollider 전환 — 2026-11-19

- 지형 Variant 135개와 월드 프리팹에서 남아 있던 MeshCollider 779개를 BoxCollider로 전환했다. 전환 직후 수량은 Mesh 0 / Box 1,233 / 기존 Sphere 3개였고, 월드 내부 바닥·보스 고지는 위 후속 요청으로 변경했다. 계단·난간·돌문의 여러 박스와 개별 Variant는 유지한다.
- 실제 플레이 이동·비정형 지형의 충돌·NavMesh는 미검증이다. 대상·구성·독립 검사 근거는 전환 기록(`plans/2026-11-19-world-purepoly-box-colliders.md`, 로컬 전용)을 따른다.

## World_PurePoly 가림 페이드 — 2026-11-17

- `NewMapScene`에 [가림 페이드 구성](PROJECT.md#world_purepoly-원형-월드-맵)을 저장했다. 씬 자체에 `OccluderTarget`이 없으므로, 플레이어가 배치되는 환경에서 보스 고지 위·아래, 지역 외곽, 돌문 옆 통로의 페이드와 원본 재질 복귀를 확인해야 한다. 고지 측면과 전투 바닥은 한 메시이므로 함께 페이드된다.

## 디버프 장판과 통합 맵 배치 — 2026-11-10

- `World.prefab`의 네 지역 일반 필드의 `SESources`에 장판 48개를 배치했다. 지형에 맞춰 재배치한 결과와 검증 상태는 장판 작업 기록(`plans/2026-11-10-디버프-장판.md#지형에-맞춘-재배치`, 로컬 전용)을 참고한다. 플레이 진입·재부여·우회 동작은 후속 확인 대상이다.

## PolygonParticleFX URP 복구 — 2026-11-10

- 기존 프리팹 180개와 Demo를 대상으로 URP 머티리얼을 연결했다. Demo는 원본 프리팹의 내장 파티클 교체 재질을 상속한다. 이 팩은 Git에서 제외되므로 재임포트 전에 로컬 복구본과 복구 기록(`plans/2026-11-10-polygon-particle-fx-urp.md`, 로컬 전용)을 확인한다.

## 저주 비네트·저항 아우라 — 2026-11-09

- 열기·혹한·질병 저주와 저항에 Feel 프리팹을 연결했다. 테스트 씬의 단일 저주 비네트와 두 대상의 아우라를 Play Mode에서 확인했다. 연출 구성 및 검증(`plans/2026-11-09-저주-저항-연출.md`, 로컬 전용)

## 열기·혹한·질병 저주 틱 효과 — 2026-11-09

- 열기·혹한·질병은 상태를 수동 부여하면 각 틱에 화상·냉기·독을 부여한다. 초기 SO 수치와 동작은 [저주 상태의 틱 효과](PROJECT.md#저주-상태의-틱-효과)에 있다. 후속 연결은 실제 저주 부여 지점, 최대 체력 감소와 정화 포션이다.
- Unity 재컴파일 오류 0건. 실제 Database를 로드한 Edit Mode 검사에서 24건을 통과했다. 즉시 첫 틱, 재부여 후 기존 틱 주기 유지, 최대 중첩, 저주 만료 뒤 일반 상태 유지를 확인했다.

## 상태이상 8종 추가 연출 — 2026-11-09

- 독·점액·젖음·기절·구속·취약·헤이스트·석화 프리팹을 공용 상태 데이터에 연결했다. 무적은 캐릭터 보호막 에셋 확보 후 이어서 진행한다. 구성과 확인 항목은 추가 연출 계획(`plans/2026-11-09-상태이상-추가-연출.md`, 로컬 전용)에 있다.

## 상태이상 틱 규칙 변경 — 2026-11-09

- 사용자 확정 규칙(즉시 첫 틱, 횟수 보장, 렉 시 몰아서 실행, 재부여 시 틱 타이머 유지)으로 `Tick`을 바꿨다. 기준은 [상태 효과 틱 규칙](PROJECT.md#상태-효과-틱-규칙)이다. 화상 에셋(5초·1초·중첩당 5)의 1중첩 총 피해가 20(4회)에서 25(5회)로 늘었고, 수치 조정은 기획 판단으로 남겼다.
- `dotnet build Assembly-CSharp.csproj`는 오류 0개였고 SJT 경고는 없었다. 독립 정적 리뷰에서 확정 문제는 없었다. PlayMode·실제 틱 로그는 미검증이다.
- 사용자가 `StatusEffectController.OnDisable` 끝에 `modifiers = StatusModifiers.Default;`를 추가했다(비활성 후 재활성 시 이전 수치가 남는 문제).
- 남은 SJT 작업 순서: 복원 시 걸림 연출 재생 억제(Controller의 `IsRestoring` 값을 Visualizer만 확인하는 방식 제안) → `ActiveStatusEffects` 읽기 전용 노출, Behavior 베이스 클래스, Database의 빈 검증 루프 정리. LoadData 중복 방지와 틱 중 비활성화 방어는 현재 호출 경로에서 발생하지 않아 보류했다. 네트워크 전파(입장 후 걸린 상태이상의 동기화, 클라이언트별 화상 피해)는 네트워크 담당 팀원의 결정 사항이다.
- 화상·냉기·빙결의 Tick Player는 연결돼 있고 Feedback 목록은 비어 있다. 최초 등록 시 Tick 재생 요청이 들어오며, 화면에 추가 입자가 나오는 슬롯은 Registered와 WhileActive다.

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
- 당시 보류 범위는 젖음과 화상의 상쇄, 냉기 최대 중첩→빙결, 저주·저항·침식·부스트·폭파, 중첩의 자연 감소였다. 독은 회복 감소이며 지속 피해로 바꾸지 않는다.

### 검증 상태

- 겹침 재계산(2026-11-04): 임시 csproj 방식 `dotnet build`로 컴파일을 확인한다. Unity PlayMode는 미검증이다. 테스트용 SO 12개(`StatusEffects/StatusEffect_<종류>.asset`, 기존 데이터는 `StatusEffect_Burn`으로 이름 변경)와 Database 목록은 디스크 참조 검사만 통과했다. Controller에 걸림·중첩·틱·종료 디버그 로그(`logStatusEffects`)와 컨텍스트 메뉴가 있다.

- 11종 구현(2026-11-04): Unity 생성 csproj에 이전에 삭제한 `IStatusModifier.cs`가 남아 있어, 그 줄만 뺀 임시 csproj로 `dotnet build`를 실행했다. 오류 0개, SJT 경고 0개였고 임시 파일은 삭제했다. Unity Console·PlayMode·실제 값 변화는 미검증이다.

- 화상 예시 최종 소스의 독립 정적 검토에서 수정이 필요한 확정 오류는 발견하지 못했다. `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet`는 오류 0개·경고 49개로 완료했다. 경고는 이번 수정 밖의 PackageCache·Photon·Imports·abcdcode 경로에서 나왔고 SJT 변경 4개에는 없었다. 검증 후 4개 소스와 Entity 해시가 동일함을 확인했다.
- 첫 컴파일은 검증 캐시 부재로 중단됐다. 루트의 csproj 77개에 PackageReference가 없음을 확인하고, `dotnet restore Assembly-CSharp.csproj --source . --ignore-failed-sources`로 로컬 산출물만 준비한 뒤 재검증했다. 패키지 설치·프로젝트 설정 변경은 하지 않았다. 부모 훅 토큰이 제공되지 않아 자식 검증 영수증은 승계하지 못했고 실제 명령 완료 결과를 근거로 보고한다.
- 이번 요청에는 PlayMode·게임 실행이 포함되지 않아 실제 체력 감소, Inspector 연결 및 Unity에서의 동작은 미검증이다. 이 시점의 시간 처리는 만료를 먼저 검사하고 Tick 초과 시간을 버렸으며, 2026-11-09 틱 규칙 변경으로 대체됐다.

## 설산·늪 마젠타 수정

- 2026-11-09 `IntegrationTestScene`에서 설산 공유 머티리얼 10개를 URP Lit으로 전환하고 기존 기본색을 보존했다. 늪 모듈 프리팹 13개는 대상 슬롯만 초원용 `EA_M_Meadows`에서 늪 전용 `EA_Swamp`로 교체했다. 두 지역의 마젠타가 사라진 것을 Scene View에서 확인했고, 눈 파티클은 별도 URP 미리보기에서 정상 표시됐다.
- `Develop` 전환 후에도 설산 머티리얼 10개와 늪 프리팹 13개의 수정이 작업본에 남아 있음을 파일에서 확인했다. 이 중 Git에 표시되는 변경은 설산 머티리얼 5개와 늪 프리팹 13개이며, 아직 커밋되지 않았다.
- 다른 작업본에서 늪 모듈을 쓰려면 `EA_Swamp` 및 해당 텍스처가 포함된 임포트 에셋이 필요하다. 설산의 `Assets/Imports` 아래 머티리얼 5개 수정은 로컬에 남는다. 자세한 변경과 검증은 로컬 작업 기록(`plans/2026-11-09-설산-늪-마젠타.md`, 로컬 전용)을 참고한다.

## 숲과 설산·늪 연결

- `SJT-MapMaking`의 열린 `IntegrationTestScene`에 북쪽 설산길과 동쪽 늪길을 연결하고, 통로 양옆의 빈 경계도 비탈 메시로 메웠다. 지형·충돌은 확인했고 씬은 사용자 확인 후 저장할 수 있도록 미저장 상태로 뒀다. 배치와 검사 범위는 숲·설산·늪 연결 기록(`plans/2026-11-09-숲-설산-늪-연결.md`, 로컬 전용)을 참고한다.

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
