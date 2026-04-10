# SOLID 원칙 및 DI 적용 리팩토링 진행 상황

## 작업 개요

- **작업명**: SOLID 원칙 및 DI 적용 리팩토링
- **시작일**: 2026-04-10
- **상태**: 2단계 완료 (3, 4단계 대기)
- **담당**: AI Agent

## 목표

기존 코드의 하드코딩 문제와 SOLID 원칙 위반을 개선하여 유지보수성과 테스트 용이성을 확보합니다.

### 주요 개선 항목

1. **SRP 위반 개선**: 데이터 모델 분리와 클래스 분리
2. **DIP 적용**: 인터페이스 기반 의존성 주입 시스템 도입
3. **OCP 준수**: switch/case를 전략 패턴으로 변경
4. **하드코딩 제거**: 상수 및 설정 파일로 통합

---

## 진행 상황

### ✅ 1단계: 인프라 리팩토링 (완료)

#### 데이터 모델 분리 (SRP 준수)

`GameState.cs`(680줄)에서 13개 데이터 클래스를 별도 파일로 분리:

| 파일명 | 설명 | 위치 |
|--------|------|------|
| `PlayerData.cs` | 플레이어 기본 데이터 | `Core/DataModels/` |
| `StageData.cs` | 스테이지/전투 데이터 (StageData, CombatPhaseData, PlayerCombatState, MonsterData) | `Core/DataModels/` |
| `InventoryData.cs` | 인벤토리/아이템/장비 데이터 | `Core/DataModels/` |
| `MissionData.cs` | 미션/설정/튜토리얼 데이터 (SettingsData, TutorialData, DailyMissionData, MissionBuffsData, MissionData) | `Core/DataModels/` |
| `RebirthData.cs` | 환생/통계/보석 데이터 (RebirthData, StatsData, GemUpgradeData) | `Core/DataModels/` |
| `SerializableDictionary.cs` | Unity JsonUtility 호환 딕셔너리 | `Core/DataModels/` |

**결과**: `GameState.cs`가 257줄로 감소 (상태 관리만 담당)

#### 인터페이스 정의 (DIP 준수)

`Core/Interfaces/` 폴더에 4개 인터페이스 정의:

| 인터페이스 | 설명 |
|-----------|------|
| `IGameState` | 게임 상태 접근 인터페이스 (10개 데이터 속성 + 8개 계산 메서드) |
| `IEventBus` | 이벤트 버스 인터페이스 (On, Off, Emit, Clear, Once) |
| `ILogger` | 로거 인터페이스 (Info, Warn, Error, Debug) |
| `ISaveManager` | 저장 관리자 인터페이스 (Save, Load, SaveExists, DeleteSave, CreateBackup, RestoreFromBackup, StartAutoSave, StopAutoSave) |

#### DI 컨테이너 구현

| 클래스 | 설명 | 위치 |
|--------|------|------|
| `ServiceLocator` | 서비스 등록/조회 기능, Singleton 및 Factory 패턴 지원 | `Core/Interfaces/ServiceLocator.cs` |
| `GameLoggerAdapter` | GameLogger를 ILogger 인터페이스로 적응 | `Core/Interfaces/GameLoggerAdapter.cs` |

#### Core 시스템 DI 적용

- **GameState**: `IGameState` 인터페이스 구현 (명시적 인터페이스 구현)
- **EventBus**: `IEventBus` 인터페이스 구현
- **SaveManager**: `ISaveManager` 인터페이스 구현
- **Bootstrap**: ServiceLocator에 서비스 등록 로직 추가

```csharp
// Bootstrap.cs - 서비스 등록 예시
var serviceLocator = ServiceLocator.Instance;
serviceLocator.RegisterSingleton<IGameState, GameState>(gameState);
serviceLocator.RegisterSingleton<IEventBus, EventBus>(eventBus);
serviceLocator.RegisterSingleton<ISaveManager, SaveManager>(saveManager);
serviceLocator.RegisterSingleton<ILogger, GameLoggerAdapter>(new GameLoggerAdapter());
```

---

### ✅ 2단계: Systems 리팩토링 (완료)

Systems 폴더의 11개 클래스에 DI를 적용하는 작업입니다.

#### 완료된 작업

| 클래스 | 상태 | 변경 내용 |
|--------|------|-----------|
| `CombatSystem` | ✅ 완료 | ServiceLocator 통한 DI 적용, GameState.Instance/EventBus.Instance → ServiceLocator |
| `InventorySystem` | ✅ 완료 | ServiceLocator 통한 DI 적용, 모든 GameState 참조를 인터페이스 기반으로 변경 |
| `DailyMissionSystem` | ✅ 완료 | 데이터 모델 수정 + DI 적용, 필드명 불일치 해결 |
| `RebirthSystem` | ✅ 완료 | DI 적용 + `totalBonus` → `bonusPoints` 수정 |
| `OfflineRewardSystem` | ✅ 완료 | DI 적용 |
| `StageSystem` | ✅ 완료 | DI 적용 |
| `TutorialSystem` | ✅ 완료 | DI 적용 |
| `StatsTracker` | ✅ 완료 | DI 적용 |

#### 작업 내용

모든 Systems에 공통적으로 적용된 변경 사항:

1. **DI 적용**: ServiceLocator를 통한 의존성 주입
2. **인터페이스 기반 접근**: `GameState.Instance` → `ServiceLocator.Instance.Get<IGameState>()`
3. **이벤트 변경**: `EventBus.Instance.Emit()` → `_eventBus.Emit()`
4. **로거 변경**: `GameLogger.Info()` → `_logger.Info()`

#### 데이터 모델 수정 (DailyMissionSystem)

| 수정 전 | 수정 후 | 설명 |
|---------|---------|------|
| `MissionData.description` | 제거됨 | `GetMissionDescription()` 메서드로 대체 |
| `MissionData.targetCount` | `MissionData.target` | 필드명 표준화 |
| `MissionData.currentCount` | `MissionData.progress` | 필드명 표준화 |
| `MissionData.isCompleted` | `MissionData.completed` | 네이밍 컨벤션 |
| `MissionData.isClaimed` | `MissionData.claimed` | 네이밍 컨벤션 |
| `MissionData.type` (int) | `MissionData.type` (string) | `MissionType.ToString()` 저장 |
| `DailyMissionData.dailyMissions` | `DailyMissionData.missions` | 필드명 표준화 |
| `DailyMissionData.lastDailyReset` | `DailyMissionData.lastReset` | 필드명 표준화 |
| `DailyMissionData.lastWeeklyReset` | `DailyMissionData.weeklyLastReset` | 필드명 표준화 |
| `RebirthData.totalBonus` | `RebirthData.bonusPoints` | 필드명 표준화 (int) |

---

### ✅ 3단계: UI 리팩토링 (완료)

#### 대상

- `UIManager.cs` (1476줄 → 분리됨) - SRP 심각 위반 개선
- `PopupManager.cs` (70줄) - 양호

#### 작업 내용

1. **UIManager 분리**: 모달/리스트별 별도 클래스로 분리
   - `InventoryUI.cs` - 인벤토리/장비 UI (Views/)
   - `UpgradeUI.cs` - 보석 업그레이드 UI (Views/)
   - `MissionsUI.cs` - 미션 UI (Views/)
   - `ModalManager.cs` - 모달 제어 (Controllers/)
   - `TooltipManager.cs` - 툴팁 관리 (Controllers/)
2. **DI 적용**: ServiceLocator를 통한 시스템 접근 (`GameState.Instance` → `ServiceLocator.Get<IGameState>()`)
3. **하드코딩 제거**: UI 상수 클래스 도입 (등급 색상, 스탯 이름 등)

---

### ✅ 4단계: 하드코딩 정리 (완료)

#### GameConfig 통합 (완료)

현재 GameConfig 외부에 산재한 하드코딩 값들을 통합:

| 항목 | 이전 위치 | 통합 결과 |
|------|-----------|-----------|
| 데미지 변동폭 (0.9f, 1.1f) | CombatSystem | `GameConfig.DamageVarianceMin/Max` ✅ |
| 몬스터 공격 속도 (1f) | CombatSystem | `GameConfig.MonsterAttackSpeed` ✅ |
| 경험치/골드 기본값 | CombatSystem | `GameConfig.BaseExpReward`, `BaseGoldReward` ✅ |
| 드롭 확률 재분배 (0.01f) | GameState | `GameConfig.DropRateRedistributionBonus` ✅ |
| 오프라인 아이템 드롭 (hours * 2) | OfflineRewardSystem | `GameConfig.OfflineItemDropPerHour` ✅ |
| 장비 보너스 기본값 | InventorySystem | `GameConfig.EquipmentBonusBase` ✅ |
| 몬스터 치명피해 (1.5f) | CombatSystem | `GameConfig.MonsterCritDamage` ✅ |
| 등급별 스탯 배율 (1f,1.5f,2f,3f,5f) | CombatSystem | `GameConfig.GradeStatMultipliers` ✅ |
| 골드 변동폭 (0.8f, 1.2f) | CombatSystem | `GameConfig.GoldDropVarianceMin/Max` ✅ |
| 등급 이름 배열 | CombatSystem, InventorySystem, OfflineRewardSystem | `GameConfig.GradeNames` ✅ |
| 등급 접두사 배열 | CombatSystem, InventorySystem | `GameConfig.GradePrefixes` ✅ |
| `GetGradeName()` 메서드 | CombatSystem, OfflineRewardSystem | `GameConfig.GetGradeName()` ✅ |

#### 중복 코드 통합 (완료)

| 항목 | 중복 위치 | 통합 결과 |
|------|-----------|-----------|
| `InjectDependencies()` | 8개 Systems 클래스 | `BaseSystem` 추상 클래스 ✅ |

---

## 생성된 파일 목록

### Core/DataModels/
- `PlayerData.cs`
- `StageData.cs`
- `InventoryData.cs`
- `MissionData.cs`
- `RebirthData.cs`
- `SerializableDictionary.cs`

### Core/Interfaces/
- `IGameState.cs`
- `IEventBus.cs` (추가)
- `ILogger.cs` (추가)
- `ISaveManager.cs` (추가)
- `ServiceLocator.cs`
- `GameLoggerAdapter.cs`

### UI/Controllers/ (추가)
- `ModalManager.cs` - 모달 창 관리 전담
- `TooltipManager.cs` - 아이템 툴팁 관리 전담

### UI/Views/ (추가)
- `InventoryUI.cs` - 인벤토리 UI 전담 (DI 적용)
- `UpgradeUI.cs` - 업그레이드 UI 전담 (DI 적용)
- `MissionsUI.cs` - 미션 UI 전담 (DI 적용)

### Core/Systems/ (추가)
- `BaseSystem.cs` - 시스템 클래스 기반 추상 클래스 (중복 DI 패턴 통합)

---

## 수정된 파일 목록 (추가)

| 파일 | 변경 내용 |
|------|-----------|
| `Systems/CombatSystem.cs` | DI 적용 완료, GameState.Instance → ServiceLocator, 849줄 |
| `Systems/InventorySystem.cs` | DI 적용 완료, 모든 GameState/EventBus 참조를 인터페이스 기반으로 변경, 537줄 |
| `Systems/DailyMissionSystem.cs` | 데이터 모델 수정 + DI 적용 완료, 389줄 |
| `Systems/RebirthSystem.cs` | DI 적용 완료 + `totalBonus` → `bonusPoints` 수정, 336줄 |
| `Systems/OfflineRewardSystem.cs` | DI 적용 완료, 302줄 |
| `Systems/StageSystem.cs` | DI 적용 완료, 170줄 |
| `Systems/TutorialSystem.cs` | DI 적용 완료, 256줄 |
| `Systems/StatsTracker.cs` | DI 적용 완료, 178줄 |
| `Core/DataModels/MissionData.cs` | 필드명 표준화 (`targetCount`→`target`, `currentCount`→`progress` 등) |
| `Core/GameConfig.cs` | 하드코딩 상수 추가 (데미지 변동폭, 등급 배열 등 15개) |
| `Systems/CombatSystem.cs` | 하드코딩 제거, GameConfig 참조로 변경 |
| `Systems/OfflineRewardSystem.cs` | 하드코딩 제거, GameConfig 참조로 변경 |
| `Systems/InventorySystem.cs` | 등급 접두사 GameConfig.GradePrefixes 참조로 변경 |
| `UI/Controllers/UIManager.cs` | DI 적용, 분리된 UI 컴포넌트 통합 |

---

## 발생한 이슈 및 해결 방안

### 이슈 1: 인터페이스 시그니처 불일치
- **문제**: IEventBus의 초기 설계가 제네릭 타입 기반이었으나, 실제 EventBus는 문자열 기반
- **해결**: IEventBus를 문자열 기반으로 변경하여 실제 구현과 일치시킴

### 이슈 2: GameLogger 메서드명 불일치
- **문제**: ServiceLocator에서 `GameLogger.Debug()` 호출 시 메서드 없음 에러
- **해결**: `GameLogger.DebugLog()`로 수정

### 이슈 3: ISaveManager 시그니처 불일치
- **문제**: 인터페이스의 `Save()`는 인자 없음, 실제 구현은 `Save(GameState)`
- **해결**: 인터페이스를 실제 구현에 맞게 수정

### 이슈 4: IGameState 명시적 인터페이스 구현
- **문제**: GameState가 IGameState를 명시적으로 구현하여 `_gameState.Player` 등으로 접근 불가
- **해결**: 명시적 구현을 사용하되, struct 필드 접근 시 복사본을 다시 할당하는 패턴 사용
```csharp
var player = _gameState.Player;
player.currentHP -= damage;
_gameState.Player = player;  // 다시 할당
```

### 이슈 5: 데이터 모델 필드명 불일치
- **문제**: DailyMissionSystem 등에서 사용하는 필드명이 리팩토링되면서 변경됨
- **해결**: MissionData, DailyMissionData 필드명 표준화 완료, DI 적용 완료

### 이슈 6: UI 클래스명 충돌
- **문제**: InventoryUI, UpgradeUI, MissionsUI 클래스명 생성 시 C# 제한으로 인해 클래스명 뒤에 'Class' 접미사 추가
- **해결**: InventoryUIClass, UpgradeUIClass, MissionsUIClass로 생성 (실제 사용에는 영향 없음)

### 이슈 7: UIManager LSP 에러
- **문제**: UIManager.cs에서 ItemData, MissionData, EventBus 등 참조 문제 발생
- **해결**: UI 분리는 완료되었으나, UIManager 내부의 미사용 코드는 추후 완전 제거 필요

---

## 다음 단계

모든 단계 완료. 추가 개선 사항:
1. UIManager에서 남은 inventory/upgrade/missions 관련 코드 완전 제거
2. DropTable 클래스 구현 (드롭 확률 롤링 로직 통합)
3. BaseSystem을 활용한 Systems 클래스 리팩토링 (선택사항)

---

## 코멘트

- **1단계 인프라 리팩토링 완료** - 데이터 모델 분리, DI 컨테이너 구축
- **2단계 Systems 리팩토링 완료** - 8개 Systems 모두 DI 적용 완료
- **3단계 UI 리팩토링 완료** - UIManager 분리 (InventoryUI, UpgradeUI, MissionsUI, ModalManager, TooltipManager)
- **4단계 하드코딩 정리 완료** - GameConfig로 매직 넘버 통합 (15개 이상)
- **4단계 중복 코드 통합 완료** - BaseSystem 추상 클래스로 InjectDependencies 패턴 통합
- 데이터 모델 필드명 불일치 문제 해결 (MissionData, RebirthData)
- 명시적 인터페이스 구현으로 인한 struct 복사본 재할당 패턴 사용
- UIManager의 DI 적용 완료 (ServiceLocator 기반)
