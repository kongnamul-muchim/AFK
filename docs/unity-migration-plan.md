# AFK 웹 게임 → Unity 이식 계획서

**작성일**: 2026-04-09  
**대상 프로젝트**: Idle RPG - Tower Climber (웹 → Unity 2D)  
**Unity 버전**: 6000.3.9f1 (Unity 6, 2022 LTS 기반)

---

## 1. 이식 개요

### 1.1 현재 프로젝트 상태

| 항목 | 내용 |
|------|------|
| **플랫폼** | 웹 (HTML/CSS/JavaScript) |
| **JS 모듈 수** | 27개 (core 5, systems 8, ui 7, adapters 2, data-parser 2, audio 1, config 1, test 1) |
| **CSV 데이터** | 6개 (items, monsters, stages, game_config, tutorial, audio_definitions) |
| **렌더링** | Canvas 2D (WebRenderer.js) |
| **저장 방식** | localStorage (JSON 직렬화) |
| **UI** | DOM 기반 HTML/CSS |

### 1.2 이식 목표

- **완전한 Unity 2D 게임으로 이관**
- 웹 빌드(WebGL) + 네이티브 빌드(Android/iOS) 동시 지원
- 기존 게임 로직과 데이터 구조 최대한 유지
- Unity의 네이티브 기능(Sprite, Animator, UI Toolkit, ScriptableObject) 활용

### 1.3 예상 기간 및 우선순위

| Phase | 기간(예상) | 우선순위 |
|-------|-----------|---------|
| Phase 1: 코어 시스템 | 3-5일 | **최상** |
| Phase 2: 게임 시스템 | 7-10일 | **최상** |
| Phase 3: UI 이식 | 5-7일 | 상 |
| Phase 4: 데이터 이관 | 2-3일 | 상 |
| Phase 5: 사운드/이펙트 | 3-5일 | 중 |
| Phase 6: 최적화/폴리싱 | 3-5일 | 중 |
| **총계** | **23-35일** | |

---

## 2. 아키텍처 매핑

### 2.1 웹 시스템 → Unity 시스템 대응표

| 웹 모듈 | Unity 대응 | 비고 |
|--------|-----------|------|
| `src/core/GameState.js` | `Scripts/Core/GameState.cs` (Singleton MonoBehaviour) | JSON 직렬화 유지 |
| `src/core/EventBus.js` | `Scripts/Core/EventBus.cs` (C# 이벤트/delegate) | C# Action/Func 활용 |
| `src/core/StorageManager.js` | `Scripts/Core/SaveManager.cs` | `PlayerPrefs` + JSON 파일 |
| `src/core/Logger.js` | `Scripts/Core/GameLogger.cs` | `Debug.Log` 래퍼 |
| `src/core/ImageLoader.js` | Unity `Sprite` + `Addressables` | |
| `src/systems/CombatSystem.js` | `Scripts/Systems/CombatSystem.cs` | MonoBehaviour Tick 기반 |
| `src/systems/InventorySystem.js` | `Scripts/Systems/InventorySystem.cs` | ScriptableObject 연동 |
| `src/systems/StageSystem.js` | `Scripts/Systems/StageSystem.cs` | |
| `src/systems/DailyMissionSystem.js` | `Scripts/Systems/DailyMissionSystem.cs` | |
| `src/systems/RebirthSystem.js` | `Scripts/Systems/RebirthSystem.cs` | |
| `src/systems/OfflineRewards.js` | `Scripts/Systems/OfflineRewards.cs` | `Time.realtimeSinceStartup` |
| `src/systems/TutorialSystem.js` | `Scripts/Systems/TutorialSystem.cs` | |
| `src/systems/StatsTracker.js` | `Scripts/Systems/StatsTracker.cs` | |
| `src/ui/UIManager.js` | Unity UI Toolkit / uGUI | UI Toolkit 권장 |
| `src/ui/InventoryUI.js` | `Scripts/UI/InventoryUI.cs` | |
| `src/ui/UpgradeUI.js` | `Scripts/UI/UpgradeUI.cs` | |
| `src/ui/GemShopUI.js` | `Scripts/UI/GemShopUI.cs` | |
| `src/ui/DailyMissionUI.js` | `Scripts/UI/DailyMissionUI.cs` | |
| `src/adapters/WebRenderer.js` | Unity `SpriteRenderer` + `Animator` | 완전 대체 |
| `src/adapters/UnityBridge.js` | 제거 (이미 Unity 내부) | |
| `src/audio/AudioManager.js` | Unity `AudioSource`/`AudioMixer` | |
| `src/data-parser/CSVParser.js` | ScriptableObject + TextAsset | CSV → SO 변환 |
| `src/data-parser/DataLoader.js` | ScriptableObject Database | |
| `src/config/GameConfig.js` | ScriptableObject `GameConfigSO` | |

### 2.2 JavaScript 클래스 → C# 매핑

```
JavaScript                          C#
─────────────────────────────────────────────────────
class GameState              →      public class GameState : MonoBehaviour
class EventBus               →      public class EventBus (static instance)
class CombatSystem           →      public class CombatSystem : MonoBehaviour
class StorageManager         →      public class SaveManager
class InventorySystem        →      public class InventorySystem
new Map()                    →      Dictionary<string, ItemData>
new Set()                    →      HashSet<string>
JSON.stringify / parse       →      JsonUtility / System.Text.Json
localStorage                 →      PlayerPrefs / Application.persistentDataPath
performance.now()            →      Time.realtimeSinceStartupAsDouble
requestAnimationFrame        →      MonoBehaviour.Update()
setTimeout / setInterval     →      Coroutine / Invoke
```

### 2.3 CSV 데이터 → ScriptableObject 매핑

| CSV 파일 | ScriptableObject | 저장 위치 |
|---------|-----------------|----------|
| `items.csv` | `ItemDataSO` (List) | `Assets/Data/Items/` |
| `monsters.csv` | `MonsterDataSO` (List) | `Assets/Data/Monsters/` |
| `stages.csv` | `StageDataSO` (List) | `Assets/Data/Stages/` |
| `game_config.csv` | `GameConfigSO` (Singleton) | `Assets/Data/Config/` |
| `tutorial.csv` | `TutorialStepSO` (List) | `Assets/Data/Tutorial/` |
| `audio_definitions.csv` | `AudioDefinitionSO` (List) | `Assets/Data/Audio/` |

**이관 전략**: 기존 CSV 파일을 TextAsset으로 로드하는 어댑터를 먼저 만들고, 점진적으로 ScriptableObject 에셋으로 변환.

---

## 3. 이식 단계 (Phase별)

### Phase 1: 코어 시스템 이식 (3-5일)

**목표**: 게임의 기반 인프라 구축

- [ ] **Day 1**: 프로젝트 구조 설정
  - [ ] Unity 2D 템플릿 생성
  - [ ] 폴더 구조 생성 (`Scripts/Core`, `Scripts/Systems`, `Scripts/UI`, `Scripts/Data`, `Assets/Data/`)
  - [ ] NuGet/Package Manager 설정 (System.Text.Json, UniTask 등)
  - [ ] Git LFS 설정 (이미 적용됨)

- [ ] **Day 2**: GameState 이식
  - [ ] `GameState.cs` 작성 (Singleton MonoBehaviour)
  - [ ] JSON 직렬화/역직렬화 구현 (`JsonUtility` 또는 `System.Text.Json`)
  - [ ] 모든 필드 마이그레이션 (player, stage, combatPhase, inventory, settings, tutorial, dailyMissions, rebirth, stats, gemUpgrades)
  - [ ] `Set → HashSet`, `Map → Dictionary` 변환

- [ ] **Day 3**: EventBus 이식
  - [ ] `EventBus.cs` 작성 (C# delegate/event 기반)
  - [ ] `GAME_EVENTS` 상수 클래스 정의
  - [ ] `on`, `off`, `emit`, `once` 메서드 구현
  - [ ] 기존 JS 이벤트명 1:1 매핑

- [ ] **Day 4**: StorageManager 이식
  - [ ] `SaveManager.cs` 작성
  - [ ] `Application.persistentDataPath` 기반 JSON 파일 저장
  - [ ] 자동 저장 (Coroutine 기반 5초 주기)
  - [ ] 버전 마이그레이션 시스템
  - [ ] 내보내기/가져오기 (파일 브라우저 연동)

- [ ] **Day 5**: GameLogger + GameConfig 이식
  - [ ] `GameLogger.cs` 작성 (Debug.Log 래퍼, 빌드 시 자동 제거)
  - [ ] `GameConfigSO` ScriptableObject 작성
  - [ ] CSV → ScriptableObject 변환 툴 작성 (Editor 스크립트)

### Phase 2: 게임 시스템 이식 (7-10일)

**목표**: 핵심 게임 로직 완전 이관

- [ ] **Day 6-7**: CombatSystem 이식
  - [ ] `CombatSystem.cs` MonoBehaviour 작성
  - [ ] `Update()` 기반 게임 루프 (고정 timestep 100ms)
  - [ ] 전투 페이즈 머신 (IDLE → MOVING → ENCOUNTERING → COMBAT → VICTORY/DEFEATED)
  - [ ] 몬스터 스폰/데미지/처치 로직
  - [ ] 아이템/보석 드롭 시스템
  - [ ] HP 재생 시스템

- [ ] **Day 8**: StageSystem + InventorySystem 이식
  - [ ] `StageSystem.cs`: 스테이지 진행, 보스 처리
  - [ ] `InventorySystem.cs`: 아이템 관리, 합성, 장비 장착

- [ ] **Day 9**: DailyMissionSystem + RebirthSystem 이식
  - [ ] `DailyMissionSystem.cs`: 일일/주간 미션, 버프 시스템
  - [ ] `RebirthSystem.cs`: 환생 로직, 보너스 포인트, 업그레이드

- [ ] **Day 10**: 기타 시스템 이식
  - [ ] `OfflineRewards.cs`: 오프라인 보상 계산
  - [ ] `TutorialSystem.cs`: 튜토리얼 진행
  - [ ] `StatsTracker.cs`: 통계 추적

- [ ] **Day 11-12**: 통합 테스트
  - [ ] 모든 시스템 간 이벤트 연동 검증
  - [ ] 세이브/로드 테스트
  - [ ] 환생 → 데이터 초기화 → 재진행 테스트

### Phase 3: UI 이식 (5-7일)

**목표**: Unity UI로 완전 재구현

- [ ] **Day 13-14**: UI 프레임워크 설정
  - [ ] **UI Toolkit** 선택 (권장: 런타임 UI + Editor 확장 통일)
    - 대안: uGUI (기존 Unity UI, 더 많은 에셋 호환)
  - [ ] 메인 캔버스/패널 구조 설계
  - [ ] HUD (레벨, HP, 골드, 스테이지)
  - [ ] 토스트 알림 시스템
  - [ ] 콤뱃 로그 패널

- [ ] **Day 15-16**: 게임 뷰 렌더링
  - [ ] `GameRenderer.cs`: SpriteRenderer + Animator 기반 캐릭터/몬스터
  - [ ] 플레이어 8프레임 애니메이션 (Animator Controller)
  - [ ] 몬스터 8프레임 애니메이션
  - [ ] 배경 전환 (일반/보스)
  - [ ] 파티클 이펙트 (공격, 피격, 처치)

- [ ] **Day 17-18**: UI 패널 이식
  - [ ] 인벤토리 패널 (아이템 그리드, 장비 슬롯, 합성 UI)
  - [ ] 업그레이드 패널 (골드/스탯 업그레이드)
  - [ ] 일일 미션 패널
  - [ ] 보석 상점/업그레이드 패널
  - [ ] 환생/업그레이드 패널
  - [ ] 설정 패널 (사운드, 진동)
  - [ ] 오프라인 보상 모달

- [ ] **Day 19**: UI 통합 테스트
  - [ ] 모든 UI ↔ 시스템 이벤트 연동 검증

### Phase 4: 데이터 이관 (2-3일)

**목표**: CSV → ScriptableObject 완전 전환

- [ ] **Day 20**: ScriptableObject 에셋 생성
  - [ ] `ItemDataSO`, `MonsterDataSO`, `StageDataSO` 등 클래스 정의
  - [ ] CSV 파싱 → ScriptableObject 일괄 생성 Editor 툴
  - [ ] 기존 CSV 파일을 TextAsset로 로드하는 어댑터 동시 유지

- [ ] **Day 21**: 데이터 로드 시스템
  - [ ] `DataLoader.cs`: ScriptableObject 기반 데이터 조회
  - [ ] `gameDataLoader.filter()`, `get()` 메서드 이식
  - [ ] Addressables 연동 (대규모 데이터용)

- [ ] **Day 22**: 데이터 검증
  - [ ] 모든 아이템/몬스터/스테이지 데이터 무결성 검증
  - [ ] 기존 웹 데이터와 수치 비교

### Phase 5: 사운드/이펙트 추가 (3-5일)

**목표**: 오디오/비주얼 효과 강화

- [ ] **Day 23-24**: 오디오 시스템
  - [ ] `AudioManager.cs`: Unity AudioSource/AudioMixer 기반
  - [ ] BGM, SFX, UI 사운드 분류
  - [ ] 사운드 볼륨/뮤트 설정
  - [ ] 오디오 정의 ScriptableObject (`AudioDefinitionSO`)

- [ ] **Day 25-26**: 이펙트/애니메이션
  - [ ] 파티클 시스템 (공격 이펙트, 레벨업, 아이템 드롭)
  - [ ] UI 애니메이션 (패널 등장/소실, 버튼 피드백)
  - [ ] 캐릭터/몬스터 애니메이션 블렌딩

- [ ] **Day 27**: 사운드/이펙트 통합 테스트

### Phase 6: 최적화 및 폴리싱 (3-5일)

**목표**: 성능 최적화 + 플랫폼 빌드

- [ ] **Day 28-29**: 최적화
  - [ ] Object Pooling (몬스터, 파티클, UI 요소)
  - [ ] Sprite Atlas 설정 (배치 드로우콜 최소화)
  - [ ] Addressables 설정 (애셋 온디맨드 로딩)
  - [ ] 모바일 최적화 (터치 입력, 해상도 대응)

- [ ] **Day 30**: 크로스플랫폼 빌드
  - [ ] WebGL 빌드 설정
  - [ ] Android 빌드 설정 (SDK/NDK)
  - [ ] iOS 빌드 설정 (선택사항)

- [ ] **Day 31-32**: 최종 테스트 및 버그 수정
  - [ ] 플랫폼별 호환성 테스트
  - [ ] 성능 프로파일링 (Unity Profiler)
  - [ ] 메모리 누수 검사

---

## 4. 기술적 고려사항

### 4.1 Unity 버전

- **권장**: Unity 6000.3.x LTS (현재 프로젝트 버전: 6000.3.9f1)
- **최소**: Unity 2022.3 LTS
- **패키지 매니저**: 최신 Stable 버전 사용

### 4.2 2D 렌더링

| 항목 | 권장 방식 |
|------|----------|
| **스프라이트** | `SpriteRenderer` + `Sprite Atlas v2` |
| **애니메이션** | `Animator` + `Animation Clip` (8프레임 스프라이트시트) |
| **파티클** | `ParticleSystem` (VFX Graph는 오버킬) |
| **카메라** | Orthographic, Size 조정으로 해상도 대응 |
| **정렬** | `Sorting Layer` (Background, Characters, Effects, UI) |

### 4.3 UI 시스템

**권장: UI Toolkit**

| 장점 | 단점 |
|------|------|
| UXML/USS로 선언적 UI | 학습 곡선 (CSS 유사) |
| 런타임/Editor 통일 | 레거시 에셋 호환성 낮음 |
| 반응형 레이아웃 강력 | uGUI보다 런타임 오버헤드 약간 높음 |

**대안: uGUI**

| 장점 | 단점 |
|------|------|
| 기존 에셋/튜토리얼 풍부 | Inspector 기반 수동 설정 |
| Canvas 기반 렌더링 최적화 | 반응형 레이아웃 구현 어려움 |

### 4.4 데이터 저장

| 방식 | 용도 | 비고 |
|------|------|------|
| `PlayerPrefs` | 설정값 (볼륨, 진동 등) | 암호화 필요시 `SecurePlayerPrefs` |
| JSON 파일 (`persistentDataPath`) | 세이브 데이터 | `System.Text.Json` 권장 |
| ScriptableObject | 게임 데이터 (아이템, 몬스터) | 빌드 시 포함, 런타임 수정 불가 |
| Addressables | 대규모 에셋/데이터 | 온디맨드 로딩, DLC 지원 |

### 4.5 어셋 관리

**권장: Addressable Asset System**

```
Assets/
├── Data/
│   ├── Items/          (ScriptableObject)
│   ├── Monsters/       (ScriptableObject)
│   ├── Stages/         (ScriptableObject)
│   └── Config/         (ScriptableObject)
├── Sprites/
│   ├── Characters/     (Addressable Group: "characters")
│   ├── Monsters/       (Addressable Group: "monsters")
│   └── Backgrounds/    (Addressable Group: "backgrounds")
├── Audio/
│   ├── BGM/            (Addressable Group: "bgm")
│   └── SFX/            (Addressable Group: "sfx")
└── UI/
    └── Panels/         (Addressable Group: "ui-panels")
```

---

## 5. 위험 요소 및 대응 방안

### 5.1 데이터 호환성 문제

| 위험 | 대응 방안 |
|------|----------|
| CSV → ScriptableObject 변환 시 데이터 손실 | 1. CSV 파싱 어댑터로 점진적 전환 2. 변환 툴에서 무결성 검증 3. 기존 JSON 세이브 데이터와 별도 버전 관리 |
| JavaScript Map/Set → C# Dictionary/HashSet 직렬화 | `JsonUtility` 대신 `System.Text.Json` 사용 (Dictionary 직렬화 지원) |
| 구버전 세이브 데이터 마이그레이션 | `SaveManager`의 버전 마이그레이션 시스템에 v1→v2→v3(Unity) 체인 구현 |

### 5.2 성능 이슈

| 위험 | 대응 방안 |
|------|----------|
| 모바일에서 프레임 드롭 | 1. Object Pooling 2. Sprite Atlas 3. UI Toolkit의 `VisualElement` 재사용 4. 고정 timestep(100ms) 게임 로직 |
| 메모리 누수 (이벤트 리스너) | EventBus의 `off` 메서드 필수 호출, `IDisposable` 패턴 도입 |
| 대량 아이템 드롭 시 렉 | 아이템 UI 가상화 (Unity UI Toolkit `ListView` 재사용) |

### 5.3 기존 세이브 데이터 이관

**전략**:
1. 웹 localStorage JSON을 파일로 내보내기 기능 제공
2. Unity에서 파일 업로드 UI 제공
3. `SaveManager.ImportWebSave()` 메서드에서 JSON 역직렬화
4. 필드명/구조 차이 자동 매핑 (예: `goldUpgrades` 숫자 → `{unlocked, level}` 객체)

---

## 6. 테스트 계획

### 6.1 단위 테스트 (Unity Test Framework)

| 테스트 대상 | 내용 |
|------------|------|
| `GameState` | 직렬화/역직렬화, 레벨업, 환생, 스탯 계산 |
| `EventBus` | 이벤트 등록/해제/발생, 메모리 누수 없음 |
| `CombatSystem` | 데미지 계산, 몬스터 처치, 드롭 확률 |
| `InventorySystem` | 아이템 추가/제거/합성, 장비 장착 |
| `SaveManager` | 저장/로드/마이그레이션 |

**기존 JS 테스트 이관**:
- `tests/unit/test-ui-consistency.js` → C# 단위 테스트
- `tests/integration/test-synthesis.js` → 통합 테스트

### 6.2 통합 테스트

| 시나리오 | 검증 항목 |
|---------|----------|
| 게임 시작 → 전투 → 레벨업 → 저장 → 재로드 | 전체 루프 데이터 일관성 |
| 환생 실행 → 데이터 초기화 → 재진행 | 환생 후 상태 정확성 |
| 오프라인 보상 계산 → 지급 → 저장 | 오프라인 시간/보상 정확성 |
| 일일 미션 진행 → 완료 → 보상 청구 | 미션 상태 전이 |

### 6.3 크로스플랫폼 테스트

| 플랫폼 | 테스트 항목 |
|--------|------------|
| **WebGL** | 브라우저 호환성, localStorage 대체, 로드 시간 |
| **Android** | 터치 입력, 해상도, 백그라운드/포그라운드 전환, 저장 |
| **iOS** | 터치 입력, 해상도, 백그라운드/포그라운드 전환, 저장 |

---

## 7. 체크리스트

### 7.1 이식 전 준비사항

- [ ] Unity 6000.3.x LTS 설치 및 프로젝트 생성
- [ ] 2D 패키지, UI Toolkit, Addressables, Test Framework 패키지 설치
- [ ] 폴더 구조 생성 (`Scripts/`, `Assets/Data/`, `Assets/Sprites/`, `Assets/Audio/`)
- [ ] Git 저장소 초기화 (기존 Unity 프로젝트는 별도 브랜치로)
- [ ] 기존 웹 프로젝트의 모든 CSV 데이터 백업
- [ ] 기존 웹 프로젝트의 세이브 데이터 샘플 확보 (테스트용)
- [ ] 스프라이트시트 에셋 확인 (플레이어 8프레임, 몬스터 8프레임, 배경)

### 7.2 이식 중 확인사항

**Phase 1 완료 시**:
- [ ] GameState 직렬화/역직렬화 테스트 통과
- [ ] EventBus로 모든 이벤트 송수신 검증
- [ ] 세이브/로드 10회 연속 성공

**Phase 2 완료 시**:
- [ ] 전투 루프 100회 반복 시 메모리 누수 없음
- [ ] 몬스터 처치 시 아이템/골드/경험치 정확 지급
- [ ] 환생 실행 시 데이터 초기화 정확
- [ ] 오프라인 보상 계산값이 웹 버전과 ±5% 이내

**Phase 3 완료 시**:
- [ ] 모든 UI 패널 열기/닫기 정상
- [ ] HUD 수치 실시간 업데이트
- [ ] 캐릭터/몬스터 애니메이션 자연스러움
- [ ] 터치/마우스 입력 동시 지원

**Phase 4 완료 시**:
- [ ] 모든 ScriptableObject 에셋 생성 완료
- [ ] CSV 데이터와 ScriptableObject 수치 100% 일치
- [ ] DataLoader 조회 성능 (1000회 조회 < 10ms)

### 7.3 이식 후 검증사항

- [ ] WebGL 빌드 정상 동작 (Chrome, Firefox, Safari)
- [ ] Android APK 빌드 및 기기 설치 정상
- [ ] iOS 빌드 정상 (선택사항)
- [ ] Unity Profiler에서 프레임 60fps 유지 (모바일 기준 30fps)
- [ ] 메모리 사용량 200MB 이하 (모바일)
- [ ] 기존 웹 세이브 데이터 불러오기 성공
- [ ] 1시간 연속 플레이 시 크래시 없음
- [ ] 백그라운드 → 포그라운드 전환 시 데이터 복구 정상
- [ ] 모든 상용 에셋 라이선스 확인 완료

---

## 부록: 폴더 구조 (권장)

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameState.cs
│   │   ├── EventBus.cs
│   │   ├── SaveManager.cs
│   │   ├── GameLogger.cs
│   │   └── Game.cs
│   ├── Systems/
│   │   ├── CombatSystem.cs
│   │   ├── StageSystem.cs
│   │   ├── InventorySystem.cs
│   │   ├── DailyMissionSystem.cs
│   │   ├── RebirthSystem.cs
│   │   ├── OfflineRewards.cs
│   │   ├── TutorialSystem.cs
│   │   └── StatsTracker.cs
│   ├── UI/
│   │   ├── UIManager.cs
│   │   ├── InventoryUI.cs
│   │   ├── UpgradeUI.cs
│   │   ├── GemShopUI.cs
│   │   ├── DailyMissionUI.cs
│   │   ├── GameRenderer.cs
│   │   └── AudioManager.cs
│   ├── Data/
│   │   ├── ItemDataSO.cs
│   │   ├── MonsterDataSO.cs
│   │   ├── StageDataSO.cs
│   │   ├── GameConfigSO.cs
│   │   └── DataLoader.cs
│   └── Tests/
│       ├── GameStateTest.cs
│       ├── EventBusTest.cs
│       └── CombatSystemTest.cs
├── Data/
│   ├── Items/
│   ├── Monsters/
│   ├── Stages/
│   ├── Config/
│   ├── Tutorial/
│   └── Audio/
├── Sprites/
│   ├── Characters/
│   ├── Monsters/
│   └── Backgrounds/
├── Audio/
│   ├── BGM/
│   └── SFX/
├── UI/
│   └── Panels/
└── Animations/
    ├── Characters/
    └── Monsters/
```
