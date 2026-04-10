# SOLID 원칙 및 DI 적용 리팩토링 진행 상황

## 작업 개요

- **작업명**: SOLID 원칙 및 DI 적용 리팩토링
- **시작일**: 2026-04-10
- **상태**: 진행 중 (1단계 완료)
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

### 🔄 2단계: Systems 리팩토링 (진행 중)

Systems 폴더의 11개 클래스에 DI를 적용하는 작업입니다.

#### 대상 클래스

| 클래스 | 파일 크기 | 주요 문제 | 우선순위 |
|--------|-----------|-----------|----------|
| `CombatSystem` | 809줄 | SRP/OCP 위반, 하드코딩 많음 | 상 |
| `InventorySystem` | 537줄 | SRP 위반 (아이템/장비/합성 혼합) | 상 |
| `DailyMissionSystem` | 389줄 | OCP 위반 (switch/case) | 중 |
| `RebirthSystem` | 350줄 | OCP 위반 (switch/case) | 중 |
| `OfflineRewardSystem` | 260줄 | 하드코딩 | 중 |
| `StageSystem` | 150줄 | 양호 | 하 |
| `TutorialSystem` | 220줄 | 양호 | 하 |
| `StatsTracker` | 160줄 | 양호 | 하 |
| `DropTable` | 27줄 | 미구현 | - |
| `ItemFactory` | 36줄 | 미구현 | - |
| `MonsterFactory` | 27줄 | 미구현 | - |

#### 작업 내용

1. **DI 적용**: ServiceLocator를 통한 의존성 주입
2. **SRP 분리**: CombatSystem → DamageCalculator, MonsterSpawner, LootDropper
3. **OCP 개선**: switch/case → Dictionary/Strategy 패턴
4. **하드코딩 제거**: GameConfig로 통합

---

### ⏳ 3단계: UI 리팩토링 (대기)

#### 대상

- `UIManager.cs` (1283줄) - SRP 심각 위반
- `PopupManager.cs` (70줄) - 양호

#### 작업 내용

1. **UIManager 분리**: 모달/리스트별 별도 클래스로 분리
2. **DI 적용**: ServiceLocator를 통한 시스템 접근
3. **하드코딩 제거**: UI 상수 클래스 도입

---

### ⏳ 4단계: 하드코딩 정리 (대기)

#### GameConfig 통합

현재 GameConfig 외부에 산재한 하드코딩 값들을 통합:

| 항목 | 현재 위치 | 통합 대상 |
|------|-----------|-----------|
| 데미지 변동폭 (0.9f, 1.1f) | CombatSystem | GameConfig |
| 몬스터 공격 속도 (1f) | CombatSystem | GameConfig |
| 경험치/골드 기본값 | CombatSystem | GameConfig |
| 드롭 확률 재분배 (0.01f) | GameState | GameConfig |
| 오프라인 아이템 드롭 (hours * 2) | OfflineRewardSystem | GameConfig |
| 장비 보너스 기본값 | InventorySystem | GameConfig |

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
- `ServiceLocator.cs`
- `GameLoggerAdapter.cs`

---

## 수정된 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Core/GameState.cs` | 데이터 모델 분리, IGameState 구현, 680줄 → 257줄 |
| `Core/EventBus.cs` | IEventBus 구현, Clear() 인터페이스 추가 |
| `Core/SaveManager.cs` | ISaveManager 구현, 명시적 인터페이스 구현 추가 |
| `Core/Bootstrap.cs` | ServiceLocator 서비스 등록 로직 추가 |

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

---

## 다음 단계

1. **CombatSystem DI 적용**: ServiceLocator를 통한 GameState/EventBus 의존성 주입
2. **CombatSystem SRP 분리**: DamageCalculator, MonsterSpawner, LootDropper로 분리
3. **InventorySystem 리팩토링**: EquipmentManager, SynthesisService로 분리
4. **UIManager 분리**: InventoryUI, UpgradeUI, MissionsUI 등으로 분리

---

## 코멘트

- 1단계 인프라 리팩토링은 완료됨
- 데이터 모델 분리로 SRP가 크게 개선됨
- DI 컨테이너 도입으로 테스트 용이성 확보 가능
- 2단계 Systems 리팩토링부터는 파일당 작업 시간이 길어질 예상
