# Unity 이식 Phase 1: 코어 시스템 이식 (3-5일)

**목표**: 게임의 기반 인프라 구축  
**우선순위**: 최상 (다른 모든 Phase의 기반)

---

## Day 1: 프로젝트 구조 설정

### 초기 설정
- [x] Unity Hub에서 새 2D 프로젝트 생성 (이름: AFK-Unity)
- [x] Unity 버전: 6000.3.9f1 (Unity 6, 2022 LTS)
- [x] 2D 패키지 설치 확인
- [x] Git 저장소 초기화 (기존 웹 프로젝트와 별도 브랜치 관리)

### 폴더 구조 생성
- [x] `Assets/Scripts/Core/` - 코어 시스템
- [x] `Assets/Scripts/Systems/` - 게임 시스템
- [x] `Assets/Scripts/UI/` - UI 스크립트
- [x] `Assets/Scripts/Data/` - 데이터 클래스
- [x] `Assets/Scripts/Tests/` - 단위 테스트
- [x] `Assets/Data/` - ScriptableObject 에셋
- [x] `Assets/Sprites/` - 그래픽 에셋
- [x] `Assets/Audio/` - 오디오 에셋
- [x] `Assets/UI/` - UI 패널/템플릿
- [x] `Assets/Animations/` - Animator Controller/Clip

### 패키지 매니저 설정
- [x] UI Toolkit 설치
- [x] Addressable Asset System 설치
- [x] Unity Test Framework 설치
- [x] TextMeshPro 설치
- [x] (선택) UniTask 설치 (비동기 처리)
- [x] (선택) System.Text.Json 설치 (고성능 JSON)

### Git LFS 설정
- [x] `.gitattributes` 파일 생성
- [x] `.unitypackage`, `.png`, `.jpg`, `.mp3`, `.wav` 등 대용량 파일 LFS 등록

---

## Day 2: GameState 이식

### GameState.cs 작성
- [x] `GameState.cs` Singleton MonoBehaviour 생성
- [x] 인스턴스 접근 패턴 구현 (`GameState.Instance`)
- [x] `DontDestroyOnLoad` 설정

### 필드 정의 (웹 버전과 동일하게)
- [x] `PlayerData player` - 플레이어 정보
- [x] `StageData stage` - 스테이지 정보
- [x] `CombatPhaseData combatPhase` - 전투 페이즈
- [x] `InventoryData inventory` - 인벤토리
- [x] `SettingsData settings` - 게임 설정
- [x] `TutorialData tutorial` - 튜토리얼
- [x] `DailyMissionData dailyMissions` - 일일/주간 미션
- [x] `RebirthData rebirth` - 환생 데이터
- [x] `StatsData stats` - 통계
- [x] `GemUpgradeData gemUpgrades` - 보석 업그레이드

### 데이터 구조체 정의
- [x] `PlayerData` struct/class (레벨, 경험치, HP, 공격력, 방어력, 체력, 골드, 보석 등)
- [x] `StageData` struct/class (현재 스테이지, 최대 스테이지, 클리어 여부)
- [x] `CombatPhaseData` struct/class (phase, playerState, monsterState, 타이머 등)
- [x] `InventoryData` class (items[], equipment[], discoveredItems)
- [x] 기타 데이터 구조체 정의

### JSON 직렬화/역직렬화
- [x] `Save()` 메서드: GameState → JSON 문자열
- [x] `Load(string json)` 메서드: JSON 문자열 → GameState
- [x] `JsonUtility` 사용
- [x] List 직렬화 지원 (HashSet → List 변환)

### 초기화 메서드
- [x] `Initialize()` 메서드: 새 게임 시작 시 초기값 설정
- [x] `ResetForRebirth()` 메서드: 환생 시 초기화
- [x] 기본값 설정 (레벨 1, 골드 0, 등)

### 테스트
- [x] GameStateTests.cs 작성
- [x] 인스턴스 생성/접근 테스트
- [x] JSON 직렬화/역직렬화 테스트
- [x] 인벤토리 테스트
- [x] 환생 초기화 테스트
- [x] 총 공격력 계산 테스트
- [x] 레벨업 필요 경험치 테스트

---

## Day 3: EventBus 이식

### EventBus.cs 작성
- [x] `EventBus.cs` Singleton 클래스 생성
- [x] 정적 인스턴스 접근 (`EventBus.Instance`)
- [x] `Dictionary<string, List<Action>>` 이벤트 저장소

### 이벤트 메서드
- [x] `On(string eventName, Action callback)` - 이벤트 등록
- [x] `Off(string eventName, Action callback)` - 이벤트 해제
- [x] `Emit(string eventName)` - 이벤트 발생
- [x] `Once(string eventName, Action callback)` - 1회용 이벤트
- [x] `HasListeners(string eventName)` - 리스너 존재 여부 확인
- [x] `GetListenerCount(string eventName)` - 리스너 수 확인

### GAME_EVENTS 상수 클래스
- [x] `GameEvents.cs` 생성 (EventBus.cs에 통합)
- [x] 모든 이벤트명 상수 정의 (웹 버전과 동일하게):
  - `PLAYER_LEVEL_UP`, `STAGE_CLEAR`, `MONSTER_KILL`
  - `ITEM_ACQUIRED`, `ITEM_SYNTHESIZED`, `ITEM_EQUIPPED`
  - `GOLD_CHANGED`, `GEM_CHANGED`, `STATS_CHANGED`
  - `COMBAT_PHASE_CHANGED`, `COMBAT_ENCOUNTER`, `COMBAT_VICTORY`
  - `DAILY_MISSION_PROGRESS`, `DAILY_MISSION_COMPLETED`, `DAILY_MISSION_CLAIMED`
  - `WEEKLY_MISSIONS_RESET`, `WEEKLY_MISSION_COMPLETED`, `WEEKLY_MISSION_CLAIMED`
  - `OFFLINE_REWARDS_CLAIMED`, `REBIRTH_PERFORMED`
  - `TUTORIAL_STEP_COMPLETED`, `SETTINGS_CHANGED`, `UI_PANEL_OPENED`, `UI_PANEL_CLOSED`
  - 등 (웹 버전의 모든 이벤트)

### 메모리 관리
- [x] 이벤트 리스너 제거 메커니즘 검증
- [x] `OnDestroy`에서 자동 해제
- [x] 예외 처리 포함

### 테스트
- [x] EventBusTests.cs 작성
- [x] 이벤트 등록/해제/발생 테스트
- [x] 여러 리스너 등록 테스트
- [x] 1회용 이벤트 테스트
- [x] 리스너 수 확인 테스트

---

## Day 4: StorageManager (SaveManager) 이관

### SaveManager.cs 작성
- [x] `SaveManager.cs` Singleton 클래스 생성
- [x] 저장 경로: `Application.persistentDataPath`
- [x] 파일명: `savegame.json`

### 저장 메서드
- [x] `Save(GameState state)` - GameState를 JSON 파일로 저장
- [x] `Load()` - JSON 파일에서 GameState 로드
- [x] `SaveExists()` - 저장 파일 존재 여부 확인
- [x] `DeleteSave()` - 저장 파일 삭제

### 자동 저장
- [x] Coroutine 기반 5초 주기 자동 저장
- [x] `StartAutoSave()` / `StopAutoSave()` 메서드
- [x] GameState 변경 시 즉시 저장 옵션

### 버전 마이그레이션
- [x] `CURRENT_SAVE_VERSION` 상수 추가
- [x] `ImportWebSave()` 메서드로 웹 세이브 이관 지원

### 내보내기/가져오기
- [x] `ExportSave()` - 파일을 텍스트로 내보내기 (웹 세이브 이관용)
- [x] `ImportSave(string json)` - 텍스트에서 파일로 가져오기
- [x] 파일 브라우저 연동 (WebGL에서는 파일 업로드 UI)

### 백업/복원
- [x] `BackupSave()` - 백업 저장
- [x] `RestoreFromBackup()` - 백업에서 복원

### 암호화 (선택)
- [ ] 간단한 XOR 암호화 또는 AES (필요시)
- [ ] `SecurePlayerPrefs` 패키지 사용 검토

### 테스트
- [x] SaveManagerTests.cs 작성
- [x] 저장/로드 테스트
- [x] 저장 파일 존재 여부 테스트
- [x] 삭제 테스트
- [x] 내보내기/가져오기 테스트
- [x] 자동 저장 시작/중지 테스트
- [x] 백업/복원 테스트

---

## Day 5: GameLogger + GameConfig 이식

### GameLogger.cs 작성
- [x] `GameLogger.cs` 정적 클래스 생성
- [x] `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()` 래퍼
- [x] `[Conditional("DEBUG")]` 속성으로 빌드 시 자동 제거
- [x] 로그 레벨 설정 (DEBUG, INFO, WARN, ERROR, NONE)
- [x] 파일 로그 출력 옵션 (선택)

### 로깅 메서드
- [x] `Log(string message)` - 일반 로그 (DEBUG 빌드 전용)
- [x] `Info(string message)` - 정보 로그
- [x] `Warn(string message)` - 경고 로그
- [x] `Error(string message)` - 에러 로그
- [x] `DebugLog(string message)` - 디버그 로그 (DEBUG 빌드에서만)
- [x] `Exception(System.Exception)` - 예외 로그
- [x] `LogIf(bool, string)` - 조건부 로그

### GameConfig 이식
- [x] `GameConfig.cs` 정적 클래스 생성
- [x] 웹 버전의 `game_config.csv` 값들을 상수로 정의:
  - `BaseMonsterHP`, `BaseMonsterAttack`, `BaseMonsterDefense`
  - `ExpToLevelUp`, `GoldDropRate`, `ItemDropRate`
  - `OfflineRewardMultiplier`, `AutoBattleDamageBonus`
  - 등 (모든 게임 밸런스 상수)
  - **SOLID 리팩토링 추가**: 데미지 변동폭, 등급 배열, 골드 변동폭 등 15개 이상

### GameConfigSO ScriptableObject (선택)
- [ ] `GameConfigSO.cs` ScriptableObject 클래스 생성
- [ ] Inspector에서 값 수정 가능하도록
- [ ] `Resources.Load<GameConfigSO>("GameConfig")`로 로드
- [ ] CSV → ScriptableObject 변환 툴 (Editor 스크립트)

### CSV → ScriptableObject 변환 툴
- [ ] `Editor/CSVToScriptableObjectConverter.cs` 생성
- [ ] CSV 파일 파싱 → ScriptableObject 에셋 일괄 생성
- [ ] 메뉴 항목: `Tools > Convert CSV to ScriptableObject`
- [ ] 중복 ID 검사, 필수 필드 검증

### Bootstrap.cs
- [x] `Bootstrap.cs`로 게임 초기화 자동화
- [x] 싱글톤 인스턴스 초기화 순서 보장
- [x] ServiceLocator에 서비스 등록 (IGameState, IEventBus, ISaveManager, ILogger)
- [x] 저장/로드 자동 처리
- [x] 자동 저장 시작

### 테스트
- [ ] GameLogger 로그 출력 테스트 (Console 확인)
- [ ] GameConfig 상수 참조 테스트
- [ ] CSV → SO 변환 툴 실행 테스트
- [ ] 생성된 ScriptableObject 에셋 확인

---

## Phase 1 완료 체크리스트

### 필수 항목
- [x] GameState 직렬화/역직렬화 테스트 통과 (모든 필드 포함)
- [x] EventBus로 모든 이벤트 송수신 검증
- [x] 세이브/로드 10회 연속 성공
- [x] 자동 저장 Coroutine 정상 동작
- [x] GameLogger 로그 출력 확인

### 코드 품질
- [x] 모든 클래스에 XML 문서 주석 (`/// <summary>`)
- [x] 네이밍 컨벤션 일관성 (PascalCase, _privateField)
- [x] 예외 처리 (null 체크, Try-Catch)
- [x] MonoBehaviour Singleton 패턴 정확 구현

### Git 커밋
- [x] Day 1: `feat: initialize Unity project structure`
- [x] Day 2: `feat: implement GameState with JSON serialization`
- [x] Day 3: `feat: implement EventBus with C# delegates`
- [x] Day 4: `feat: implement SaveManager with auto-save`
- [x] Day 5: `feat: implement GameLogger and GameConfig`
- [x] Phase 1 완료: `feat: complete Phase 1 - core systems`

### SOLID 리팩토링 (Phase 1 완료 후 추가 수행)
- [x] 인터페이스 정의 (IGameState, IEventBus, ILogger, ISaveManager)
- [x] ServiceLocator 구현 및 서비스 등록
- [x] 데이터 모델 분리 (PlayerData, StageData, InventoryData 등 6개 파일)
- [x] GameLoggerAdapter로 ILogger 인터페이스 적응
- [x] Git 커밋: `feat: complete SOLID refactoring - UI separation and hardcoded values cleanup`

### 다음 Phase 준비
- [x] Phase 2 (게임 시스템 이식)을 위한 스크립트 템플릿 준비
- [x] CombatSystem.cs, StageSystem.cs 등 빈 클래스 미리 생성
- [ ] 웹 버전의 시스템 코드 리뷰 (어떤 로직을 이식할지)

---

## 📝 메모

- **GameState 직렬화**: `System.Text.Json`이 `Dictionary`/`HashSet`을 기본 지원하므로 권장
- **EventBus 메모리**: 이벤트 리스너 제거를 잊지 말 것 (메모리 누수 주의)
- **SaveManager**: 웹 세이브 이관을 위해 `ImportWebSave()` 메서드 미리 준비
- **GameConfig**: ScriptableObject로 만들면 Inspector에서 밸런스 조정 가능 (추천)
- **테스트**: 각 Day마다 단위 테스트 작성 (Unity Test Framework)
- **SOLID 리팩토링 완료**: 인터페이스, ServiceLocator, 데이터 모델 분리, DI 적용 완료
- **UIManager 분리**: InventoryUI, UpgradeUI, MissionsUI, ModalManager, TooltipManager 분리 완료

---

**Phase 1이 전체 프로젝트의 기반이 됩니다. 철저하게 테스트하고 진행하세요.**
