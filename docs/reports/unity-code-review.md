# 🎯 Unity ↔ Web 포팅 차이점 분석

> 기준: **Web JS 버전이 정답**
> Unity는 Web과 다른 동작을 하는 부분을 수정해야 함

---

## 🔴 P0 — Web에 있는데 Unity에 없거나 틀린 기능

### 1. 몬스터가 플레이어를 공격하지 않음

| Web | Unity |
|-----|-------|
| `CombatSystem.update()`에 몬스터 공격 타이머 있음 | `MonsterAttack()` 정의만 있고 **호출 코드 없음** |
| 몬스터가 `attackCooldown` 기반으로 플레이어 공격 | 플레이어는 반동 데미지(`ConsumePlayerHP`)로만 HP 깎임 |

**수정**: `Web Systems/CombatSystem.js`의 몬스터 공격 타이머 로직을 Unity `CombatSystem.Update()`에 추가. `ConsumePlayerHP()`는 Web에 없는 개념이므로 제거.

---

### 2. UpgradeUI가 RebirthSystem/시스템을 우회

| Web | Unity |
|-----|-------|
| `UpgradeUI.js` → `RebirthSystem.js` 메서드 호출 | `UpgradeUIClass.PerformRebirth()`가 직접 `GameState` 조작 (라인 1144) |
| 보석 업그레이드도 `RebirthSystem.js` 경유 | 보석 업글도 직접 `_gameState.Player.gems -= cost` (라인 1100) |

**수정**: UpgradeUI의 모든 시스템 호출을 해당 `.Instance.PerformRebirth()` / `.UpgradeGem()` 등으로 변경.

---

### 3. 오프라인 시간 추적 버그

| Web | Unity |
|-----|-------|
| `StorageManager.save()` 마지막에 `lastSaveTime = Date.now()` | `_lastSaveTime`이 `GAME_LOADED`에서만 갱신 (저장 시 안 함) |

**수정**: `SaveManager.Save()` 호출 시 `OfflineRewardSystem._lastSaveTime`도 함께 갱신.

---

### 4. 전투 승리 후 처리 불일치

| Web | Unity |
|-----|-------|
| `CombatSystem.js` victory 처리 → StageSystem/GameState 메서드 호출 | `CombatSystem.Update()` VICTORY에서 직접 stage 증가, `StageSystem` 완전 우회 |

**수정**: VICTORY 처리에서 `StageSystem.Instance.NextStage()` 또는 `EnterStage()` 사용. StageSystem의 `STAGE_ENTERED`/`STAGE_CLEAR` 이벤트가 정상 발생하도록.

---

### 5. CSV JSON stats 파싱

| Web | Unity |
|-----|-------|
| `CSVParser.js`가 stats JSON을 자동 파싱 (타입 변환 내장) | `Bootstrap.cs` + `DropTable.cs`에서 `Substring`/`IndexOf`로 직접 파싱 |

**수정**: `JsonUtility` 또는 `Newtonsoft.Json`으로 stats JSON 정규 파싱.

---

## 🟡 P1 — Web과 동작이 다른 부분

### 6. GameConfig 이중 관리

| Web | Unity |
|-----|-------|
| `GameConfig.js` 단일 파일 | `GameConfig.cs` (static) + `GameConfigSO.cs` (ScriptableObject) 존재, **값도 다름** |

**수정**: `GameConfigSO` 제거하거나, `GameConfig` 통일. Web의 값과 일치시킬 것.

---

### 7. StageData 등 struct 사용

| Web | Unity |
|-----|-------|
| 모든 데이터가 객체 참조 (직접 `.필드 = 값`) | `StageData`, `CombatPhaseData`, `ItemData` 등이 **struct** → 매번 `var stage = _gameState.Stage; stage.x = y; _gameState.Stage = stage;` 패턴 |

**수정**: 직렬화 문제만 없다면 `class`로 변경하거나, 현재 struct 패턴을 유지하되 일관성 있게 사용. (Unity JSON 직렬화 한계로 struct일 가능성 높음 — 확인 필요)

---

### 8. StatsTracker 이중 카운트

| Web | Unity |
|-----|-------|
| `CombatSystem.js`가 stats 증가 + `StatsTracker.js`가 별도로 증가하는지 확인 필요 | `CombatSystem.ProcessVictory()`에서 `stats.totalKills++` + `StatsTracker.OnMonsterKill()`에서 또 `stats.totalKills++` |

**수정**: Web과 동일하게 한 곳에서만 카운트하도록 통일.

---

### 9. TutorialSystem 미이식

| Web | Unity |
|-----|-------|
| CSV 기반 20단계 튜토리얼 (조건/보상/메시지) | `Debug.Log`만 있음 |

**수정**: Web `TutorialSystem.js` 로직을 `TutorialSystem.cs`로 이식.

---

### 10. ItemFactory / MissionSystem 미구현

| Web | Unity |
|-----|-------|
| 아이템 생성/미션 시스템 동작함 | `Debug.LogError("not implemented yet")` |

**수정**: Web 기준으로 구현.

---

### 11. Audio 미구현

| Web | Unity |
|-----|-------|
| `AudioManager.js`도 스텁 (TODO만 있음) | `AudioManager.cs`도 `Debug.Log`만 있음 |

**수정**: 양쪽 다 미구현이므로 보류 가능.

---

### 12. Bootstrap.GiveStarterItems 로직 차이

| Web | Unity |
|-----|-------|
| `GameState.js` 초기화에서 아이템 목록 관리 | CSV 모든 아이템 순회하며 `count=0`으로 추가 |

**수정**: Web의 초기화 방식과 비교해서 동일한지 확인. Web은 모든 아이템을 slots로 관리하나? 아니면 실제 드롭으로만 획득하나?

---

## 🟢 P2 — 구조 차이 (기능상 문제는 아님)

### 13. ServiceLocator vs 직접 Instance

| Web | Unity |
|-----|-------|
| 모듈 직접 `import` / `window.game` 전역 | `ServiceLocator` + `XxxSystem.Instance` 이중 접근 |

**판단**: Web처럼 단순하게 갈 거면 Instance 기반으로 통일하고 ServiceLocator 제거. DI가 필요하면 Web 기준엔 없으니 일단 보류.

---

### 14. GameRenderer + UIGameRenderer 공존

| Web | Unity |
|-----|-------|
| `WebRenderer.js` 단일 | `GameRenderer.cs` (SpriteRenderer) + `UIGameRenderer.cs` (UI Toolkit) |

**판단**: UI Toolkit 기반 `UIGameRenderer`만 사용할지 결정. `GameRenderer`는 정리.

---

### 15. MissionsUI 자체 MissionData 클래스

| Web | Unity |
|-----|-------|
| `DailyMissionUI.js`는 UI만 담당 | `MissionsUI.cs` 내부에 자체 `MissionData` 클래스 (라인 500)가 `DataModels/MissionData.cs`와 중복 |

**수정**: `DataModels/MissionData.cs`를 사용하도록 통일.

---

## 📋 수정 우선순위 (Web 기준)

| 순위 | 작업 | 대상 파일 |
|------|------|----------|
| 🔴 P0 | 몬스터 공격 추가 (MonsterAttack 호출) | `CombatSystem.cs` |
| 🔴 P0 | UpgradeUI → 시스템 호출로 변경 | `UpgradeUI.cs`, `RebirthSystem.cs` |
| 🔴 P0 | 오프라인 저장 시간 갱신 | `OfflineRewardSystem.cs`, `SaveManager.cs` |
| 🔴 P1 | VICTORY 처리 StageSystem 경유 | `CombatSystem.cs`, `StageSystem.cs` |
| 🟡 P1 | CSV stats JSON 파싱 개선 | `Bootstrap.cs`, `DropTable.cs` |
| 🟡 P1 | GameConfig 일원화 | `GameConfig.cs`, `GameConfigSO.cs` |
| 🟡 P1 | StatsTracker 중복 카운트 정리 | `CombatSystem.cs`, `StatsTracker.cs` |
| 🟡 P1 | TutorialSystem 이식 | `TutorialSystem.cs` + CSV |
| 🟡 P1 | ItemFactory / MissionSystem 구현 | 해당 파일들 |
| 🟢 P2 | MissionsUI MissionData 통일 | `MissionsUI.cs` |
| 🟢 P2 | ServiceLocator 정리 (또는 제거) | 전역 |
| 🟢 P2 | GameRenderer / UIGameRenderer 통합 | `Rendering/` |
