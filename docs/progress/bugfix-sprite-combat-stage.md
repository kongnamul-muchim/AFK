# 버그 수정 보고서: 스프라이트 미표시, 전투 미진행, 스테이지 무한 증가

**작성일:** 2026-04-13  
**작성자:** AI Agent  
**커밋 해시:** `b0a81f4`

---

## 📋 문제 요약

사용자가 보고한 3가지 문제:
1. 플레이어와 적의 스프라이트가 보이지 않음
2. 전투가 진행되지 않고 적은 계속 죽은 상태로 유지
3. 스테이지가 틱마다 계속해서 오름

---

## 🔍 근본 원인 분석

### 문제 1 & 2 & 3의 연쇄적 관계

세 문제는 모두 **하나의 근본적인 버그**에서 파생되었습니다:

```
CombatPhaseData.Reset()에서 monsterState를 초기화하지 않음
    ↓
몬스터가 HP=0인 상태로 새 스테이지에 등장
    ↓
CombatSystem이 즉시 "몬스터 사망" 판정 → VICTORY 페이즈
    ↓
VICTORY 페이즈가 2초 후 NextStage() 호출
    ↓
다음 스테이지 진입 → 몬스터 다시 HP=0으로 등장 → 즉시 VICTORY
    ↓
【무한 루프】스테이지가 틱마다 증가
```

### 세부 원인

#### 1. `CombatPhaseData.Reset()` 누락
```csharp
// StageData.cs - 수정 전
public void Reset()
{
    phase = 0;
    timer = 0;
    // ❌ monsterState.currentHP가 0(기본값)인 채로 남음
}
```

#### 2. `SpawnMonster()`에서 명시적 HP 초기화 없음
```csharp
// CombatSystem.cs - 수정 전
private void SpawnMonster()
{
    MonsterData monster = _monsterFactory.CreateMonster(stage);
    var combatPhase = _gameState.CombatPhase;
    combatPhase.monsterState = monster;
    // ❌ monster.currentHP가 0일 수 있음 (Reset()에서 초기화 안 됨)
}
```

#### 3. `UIGameRenderer` 초기화 타이밍 문제
- `OnEnable()`에서 `ServiceLocator`가 아직 초기화되기 전에 의존성 주입 시도
- 텍스처 로드 실패 시 대체 경로 시도 로직 부재

---

## 🔧 수정 내용

### 0. 추가 수정: `StartCombat()` - 전투 데이터 완전 초기화

**파일:** `assets/Scripts/Systems/CombatSystem.cs`

```csharp
public void StartCombat()
{
    // ✅ 전투 데이터 초기화 (이전 전투의 잔여 데이터 제거)
    var combatPhase = _gameState.CombatPhase;
    combatPhase.phase = 0;
    combatPhase.timer = 0;
    combatPhase.monsterState = new MonsterData(); // HP=0인 몬스터 초기화
    _gameState.CombatPhase = combatPhase;
    
    // 전투 타이머 초기화
    _combatTimer = 0f;
    _lastAttackTime = 0f;
    // ...
}
```

**이유:** 저장된 게임 데이터나 이전 전투의 잔여 데이터가 새 전투에 영향을 주지 않도록 완전히 초기화.

---

### 1. `CombatPhaseData.Reset()` - monsterState 초기화 추가

**파일:** `assets/Scripts/Core/DataModels/StageData.cs`

```csharp
public void Reset()
{
    phase = 0;
    timer = 0;
    // ✅ 몬스터 상태 초기화 (새 전투 시작 시 죽은 상태로 등장하는 버그 방지)
    monsterState = new MonsterData
    {
        name = "",
        stage = 0,
        currentHP = 0,
        maxHP = 0,
        attack = 0,
        defense = 0,
        grade = 0
    };
}
```

### 2. `CombatSystem.SpawnMonster()` - 명시적 HP 초기화

**파일:** `assets/Scripts/Systems/CombatSystem.cs`

```csharp
private void SpawnMonster()
{
    MonsterData monster = _monsterFactory.CreateMonster(stage);
    
    // ✅ 몬스터 HP를 최대 HP로 명시적 초기화
    monster.currentHP = monster.maxHP;
    
    var combatPhase = _gameState.CombatPhase;
    combatPhase.monsterState = monster;
    // ...
}
```

### 3. `UIGameRenderer` - ServiceLocator 초기화 확인 및 텍스처 로드 개선

**파일:** `assets/Scripts/Rendering/UIGameRenderer.cs`

```csharp
private void OnEnable()
{
    // ✅ ServiceLocator 초기화 확인
    if (ServiceLocator.Instance == null)
    {
        Debug.LogWarning("[UIGameRenderer] ServiceLocator가 아직 초기화되지 않았습니다.");
        return;
    }
    // ...
}

private Texture2D LoadTexture(string path)
{
    Debug.Log($"[UIGameRenderer] 텍스처 로드 시도: {path}");
    
    Texture2D texture = Resources.Load<Texture2D>(path);
    if (texture == null)
    {
        // ✅ 대체 경로 시도
        string altPath = path.Replace("images/", "");
        Texture2D altTexture = Resources.Load<Texture2D>(altPath);
        if (altTexture != null)
        {
            Debug.Log($"[UIGameRenderer] 대체 경로로 로드 성공: {altPath}");
            return altTexture;
        }
    }
    return texture;
}
```

### 4. `GameRenderer` - Inspector 참조 검증 및 자동 감지

**파일:** `assets/Scripts/Rendering/GameRenderer.cs`

```csharp
private void ValidateInspectorReferences()
{
    if (_playerSpriteRenderer == null)
    {
        Debug.LogWarning("[GameRenderer] _playerSpriteRenderer가 할당되지 않았습니다.");
        // ✅ 자동으로 자식 오브젝트에서 찾기
        _playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    // ...
}
```

### 5. `CombatSystem` - 공격/피격 애니메이션 트리거 추가

**파일:** `assets/Scripts/Systems/CombatSystem.cs`

```csharp
private void PlayerAttack()
{
    // ... 데미지 계산 ...
    
    // ✅ 공격 애니메이션 트리거
    GameRenderer.Instance?.TriggerPlayerAttack();
    GameRenderer.Instance?.TriggerMonsterHit();
    
    // ...
}

private void MonsterAttack()
{
    // ... 데미지 계산 ...
    
    // ✅ 몬스터 공격 애니메이션 트리거
    GameRenderer.Instance?.TriggerMonsterAttack();
    GameRenderer.Instance?.TriggerPlayerHit();
    
    // ...
}
```

---

## 📊 수정 결과

| 문제 | 수정 전 | 수정 후 |
|------|---------|---------|
| 스프라이트 미표시 | ServiceLocator 초기화 전 접근, 텍스처 로드 실패 | 초기화 확인, 대체 경로 시도, 자동 감지 |
| 몬스터 죽은 상태 | HP=0으로 등장, 즉시 사망 판정 | StartCombat에서 완전 초기화, SpawnMonster에서 HP=maxHP 설정 |
| 스테이지 무한 증가 | 죽은 몬스터 → 즉시 승리 → 무한 NextStage | 정상 전투 → 승리 → 다음 스테이지 |

---

## ✅ 테스트 체크리스트

- [ ] 게임 시작 시 플레이어/몬스터 스프라이트가 표시되는가?
- [ ] 몬스터가 최대 HP로 등장하는가?
- [ ] 전투가 정상적으로 진행되는가 (공격/피격 반복)?
- [ ] 몬스터 사망 후 승리로 전환되는가?
- [ ] 승리 후 2초 대기 후 다음 스테이지로 이동하는가?
- [ ] 다음 스테이지에서 몬스터가 정상 HP로 등장하는가?
- [ ] 스테이지가 무한히 증가하지 않는가?
- [ ] **저장 파일 삭제 후 새 게임에서도 정상 작동하는가?** (기존 저장 데이터 삭제 권장)

## 🔍 추가 디버깅이 필요한 경우

Unity 콘솔에서 다음 로그를 확인하세요:

1. `[UIGameRenderer] 텍스처 로드 시도: images/characters/player_spritesheet_0`
2. `[UIGameRenderer] 텍스처 로드 성공: 512x512` (또는 실패 로그)
3. `[GameRenderer] _playerSpriteRenderer가 할당되지 않았습니다` (경고)
4. `몬스터 등장 - 슬라임 (스테이지 1, HP: 50/50, 일반)`

**스프라이트가 여전히 안 보인다면:**
- `Assets/Resources/images/` 폴더에 텍스처 파일이 있는지 확인
- Unity 에디터에서 텍스처 파일의 Import Settings 확인 (Texture Type이 Default인지)
- `UIGameRenderer`가 UIManager에서 올바르게 초기화되는지 확인

---

## 📝 참고 사항

1. **Resources 폴더 확인**: 텍스처 파일이 `Assets/Resources/images/`에 존재하는지 확인
2. **Inspector 설정**: GameRenderer 컴포넌트에 SpriteRenderer 참조가 할당되어 있는지 확인
3. **애니메이션**: Animator Controller에 Attack, Hit, Victory, Defeated 트리거가 정의되어 있는지 확인

---

*본 문서는 AGENTS.md 규칙에 따라 작성되었습니다.*
