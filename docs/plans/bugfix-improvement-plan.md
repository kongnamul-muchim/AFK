# AFK Idle RPG — 주요 버그 및 개선 방안

> **작성일:** 2026-05-22
> **대상:** Unity C# 버전 (assets/Scripts/)
> **기준:** Web JavaScript 버전 (src/) 동작

---

## 목차

1. [🚨 문제 1: 전투 승리 후 처리 — StageSystem 우회](#-문제-1-전투-승리-후-처리--stagesystem-우회)
2. [🚨 문제 2: 오프라인 보상 시간 추적 버그](#-문제-2-오프라인-보상-시간-추적-버그)
3. [🚨 문제 3: UpgradeUI가 RebirthSystem/시스템을 우회하여 직접 GameState 조작](#-문제-3-upgradeui가-rebirthsystem시스템을-우회하여-직접-gamestate-조작)

---

## 🚨 문제 1: 전투 승리 후 처리 — StageSystem 우회

### 🔍 현황

`CombatSystem.cs`의 VICTORY 페이즈 처리에서 **StageSystem을 완전히 우회**하고 있음.

```csharp
// CombatSystem.cs - VICTORY 페이즈 (Update 메서드, ~L377)
case CombatPhase.VICTORY:
    if (_phaseTimer >= 2f && !_victoryNextStageCalled)
    {
        _victoryNextStageCalled = true;
        _gameState.Player.currentHP = _gameState.GetTotalHealth();
        AdvanceKillCounter();        // ← 직접 killsInStage 증가 + stage 증가
        // ... (전투 데이터 초기화)
        _eventBus.Emit(GameEvents.STAGE_ENTERED);
        ChangePhase(CombatPhase.MOVING);
    }
```

`AdvanceKillCounter()` → `AdvanceToNextStage()`에서 **직접 `stageData.currentStage++`** 를 수행.

### ❌ 문제점

| 항목 | Web (정상) | Unity (버그) |
|------|-----------|--------------|
| 스테이지 진행 | `StageSystem.EnterStage()` 경유 | 직접 `currentStage++` |
| 이벤트 발생 | `STAGE_ENTERED`, `STAGE_CLEAR` 순서 보장 | 순서 불일치 |
| 스테이지 기록 | `StageSystem`에서 maxStage 관리 | `ProcessVictory` + `AdvanceToNextStage`에서 분산 관리 |
| 재진입 로직 | StageSystem에서 초기화 담당 | 중복 초기화 가능성 |

**파생 버그:**
- `STAGE_CLEAR` 이벤트가 `STAGE_ENTERED` 이전에 발생할 수 있음
- `StageSystem`에 향후 로직 추가 시 적용 안 됨
- 스테이지 전환 시 필요한 추가 처리(버프 초기화, 상태 리셋 등) 누락 가능

### ✅ 개선 방안

**StageSystem을 경유하도록 리팩토링:**

```csharp
// 1. StageSystem.cs에 다음 스테이지 진입 메서드 통합
public class StageSystem : MonoBehaviour
{
    public void EnterNextStage()
    {
        var stage = _gameState.Stage;
        int prevStage = stage.currentStage;
        
        stage.killsInStage = 0;
        stage.currentStage++;
        stage.maxStage = Mathf.Max(stage.maxStage, stage.currentStage);
        _gameState.Stage = stage;
        
        _eventBus.Emit(GameEvents.STAGE_CLEAR);
        _eventBus.Emit(GameEvents.STAGE_ENTERED);
    }
}

// 2. CombatSystem.cs VICTORY 처리에서 StageSystem 호출
case CombatPhase.VICTORY:
    if (_phaseTimer >= 2f && !_victoryNextStageCalled)
    {
        _victoryNextStageCalled = true;
        _gameState.Player.currentHP = _gameState.GetTotalHealth();
        
        // StageSystem 경유 (직접 증가하지 않음)
        StageSystem.Instance.EnterNextStage();
        
        ChangePhase(CombatPhase.MOVING);
    }
```

**변경 대상 파일:**
- `assets/Scripts/Systems/CombatSystem.cs` — VICTORY 처리, AdvanceKillCounter(), AdvanceToNextStage()
- `assets/Scripts/Systems/StageSystem.cs` — EnterNextStage() 메서드 추가

---

## 🚨 문제 2: 오프라인 보상 시간 추적 버그

### 🔍 현황

`OfflineRewardSystem.CalculateOfflineTime()`에서 **파일의 마지막 수정 시간**을 기준으로 오프라인 시간을 계산함.

```csharp
// OfflineRewardSystem.cs - CalculateOfflineTime()
public float CalculateOfflineTime()
{
    DateTime lastWrite = GetSaveFileLastWriteTime();  // File.GetLastWriteTimeUtc
    TimeSpan elapsed = now - lastWrite;
    return (float)elapsed.TotalHours * 3600f;
}
```

그러나 `SaveManager.Save()`가 호출되어도 **별도로 `_lastSaveTime`을 기록하지 않음**.

Web 버전에서는:
```javascript
// StorageManager.js - save()
save(key, value) {
    localStorage.setItem(key, json);
    this.lastSaveTime = Date.now();  // ← 저장 시 항상 갱신
}
```

### ❌ 문제점

| 시나리오 | 예상 | 실제 |
|---------|------|------|
| 게임 플레이 중 자동 저장 (5초마다) | `_lastSaveTime` 갱신 | 파일 시간만 갱신되고 내부 시간 기록 없음 |
| 게임 재시작 | 직전 저장 시간 기준으로 오프라인 시간 계산 | 마지막 파일 수정 시간 사용 |
| 게임 강제 종료 후 재시작 | 오프라인 시간 정상 계산 | OS 파일 시간 정밀도에 의존 (초 단위 손실 가능) |

**파생 버그:**
- 오프라인 보상이 0으로 계산되거나 부정확함
- Web 버전 대비 오프라인 보상 경험치/골드/아이템 누락

### ✅ 개선 방안

**두 가지 방식 중 하나 선택:**

#### 방안 A (권장): GameState에 마지막 저장 시간 기록

```csharp
// 1. GameState.cs에 필드 추가
[Serializable]
public class GameState
{
    public long lastSaveTimestamp;  // DateTime.UtcNow.Ticks
    // ...
}

// 2. SaveManager.Save()에서 갱신
public void Save(GameState state)
{
    state.lastSaveTimestamp = DateTime.UtcNow.Ticks;
    // ...
}

// 3. OfflineRewardSystem.CalculateOfflineTime()에서 사용
public float CalculateOfflineTime()
{
    long lastTicks = _gameState.lastSaveTimestamp;
    
    // 이전 저장 데이터가 없으면 파일 시간으로 폴백
    if (lastTicks == 0)
    {
        DateTime lastWrite = GetSaveFileLastWriteTime();
        lastTicks = lastWrite.Ticks;
    }
    
    TimeSpan elapsed = DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc);
    return Mathf.Clamp((float)elapsed.TotalHours, 0, GameConfig.MaxOfflineTime) * 3600f;
}
```

#### 방안 B: SaveManager에서 OfflineRewardSystem 직접 호출

```csharp
// SaveManager.cs
public void Save(GameState state)
{
    // ... (기존 저장 로직)
    File.WriteAllText(savePath, json);
    
    // 오프라인 시간 기록 갱신
    OfflineRewardSystem.Instance?.RecordSaveTime();
    
    EventBus.Instance.Emit(GameEvents.GAME_SAVED);
}
```

**변경 대상 파일:**
- `assets/Scripts/Core/GameState.cs` — `lastSaveTimestamp` 필드 추가
- `assets/Scripts/Core/SaveManager.cs` — 저장 시 타임스탬프 갱신
- `assets/Scripts/Systems/OfflineRewardSystem.cs` — `CalculateOfflineTime()` 수정

---

## 🚨 문제 3: UpgradeUI가 RebirthSystem/시스템을 우회하여 직접 GameState 조작

### 🔍 현황

`UpgradeUI.cs`에서 `RebirthSystem`, `ShopSystem` 등의 **시스템 클래스를 거치지 않고** UI에서 직접 `_gameState`를 조작함.

```csharp
// UpgradeUI.cs (현재)
public void PerformRebirth()
{
    // ❌ 직접 GameState 조작
    _gameState.Player.level = 1;
    _gameState.Player.experience = 0;
    _gameState.Player.statPoints = 0;
    _gameState.Stage.currentStage = 1;
    _gameState.Player.gold = 0;
    // ...
}

public void UpgradeGem(string gemType)
{
    // ❌ 직접 GameState 조작
    _gameState.Player.gems -= cost;
    _gameState.gemUpgrades[gemType] += 1;
    // ...
}
```

### ❌ 문제점

| 문제 | 영향 |
|------|------|
| **SRP 위반** | UI(View)가 게임 로직(Controller)을 겸함 |
| **로직 중복** | RebirthSystem.cs에 같은 로직이 있을 경우 불일치 발생 |
| **이벤트 누락** | `REBIRTH_PERFORMED` 등 필요한 이벤트가 발생하지 않을 수 있음 |
| **유효성 검사 누락** | 시스템 레벨의 조건 검사(최소 레벨, 재화 체크 등)가 적용 안 됨 |
| **디버깅 어려움** | GameState 변경을 추적하기 어려움 (단일 진입점 없음) |

Web 버전에서는:
```javascript
// UpgradeUI.js (정상)
performRebirth() {
    RebirthSystem.performRebirth();  // ← 시스템 경유
}

upgradeGem(type) {
    RebirthSystem.upgradeGem(type);  // ← 시스템 경유
}
```

### ✅ 개선 방안

**UI의 모든 시스템 호출을 해당 시스템 클래스로 위임:**

```csharp
// 1. RebirthSystem.cs에 메서드 통합
public class RebirthSystem : MonoBehaviour
{
    public bool PerformRebirth()
    {
        // 조건 검사 (레벨, 재화 등)
        if (_gameState.Player.level < GameConfig.MinRebirthLevel)
            return false;
        
        // 환생 처리
        _gameState.Rebirth.count++;
        _gameState.Rebirth.bonusPoints += CalculateBonusPoints();
        _gameState.Player.level = 1;
        _gameState.Player.experience = 0;
        _gameState.Player.gold = 0;
        _gameState.Stage.currentStage = 1;
        // ...
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.REBIRTH_PERFORMED);
        return true;
    }
    
    public bool UpgradeGem(string gemType)
    {
        int cost = GetGemUpgradeCost(gemType);
        if (_gameState.Player.gems < cost)
            return false;
        
        _gameState.Player.gems -= cost;
        // gemUpgrades 증가...
        _eventBus.Emit(GameEvents.GEM_CHANGED);
        return true;
    }
}

// 2. UpgradeUI.cs에서 시스템 호출로 변경
public class UpgradeUI : MonoBehaviour
{
    public void OnClickRebirth()
    {
        bool success = RebirthSystem.Instance.PerformRebirth();
        if (success) UpdateAllUI();
    }
    
    public void OnClickGemUpgrade(string gemType)
    {
        bool success = RebirthSystem.Instance.UpgradeGem(gemType);
        if (success) UpdateAllUI();
    }
}
```

**변경 대상 파일:**
- `assets/Scripts/Systems/RebirthSystem.cs` — `PerformRebirth()`, `UpgradeGem()` 메서드 구현/보강
- `assets/Scripts/UI/Views/UpgradeUI.cs` — 직접 조작 코드를 시스템 호출로 변경

---

## 📋 우선순위 요약

| 순위 | 문제 | 난이도 | 영향도 | 예상 작업량 |
|------|------|--------|--------|-----------|
| 🥇 | **#1 StageSystem 우회** | ⭐⭐ 중 | 코어 게임플레이 | 2파일, ~30줄 |
| 🥇 | **#2 오프라인 보상 시간 추적** | ⭐ 하 | 핵심 보상 시스템 | 2~3파일, ~20줄 |
| 🥈 | **#3 UpgradeUI 시스템 우회** | ⭐⭐⭐ 중상 | 유지보수성 | 2파일, ~80줄 |

---

*기준: Web JS 버전 (src/) — 코드 리뷰 문서 (docs/reports/unity-code-review.md) 기반 분석*
