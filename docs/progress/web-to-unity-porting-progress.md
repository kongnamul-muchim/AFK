# Web → Unity 포팅 진행 상황

## 최종 업데이트: 2026-04-14

---

## 완료된 작업

### 1. 밸런스 동기화 (GameConfig.cs)
| 설정 | Web | Unity (수정됨) |
|------|-----|----------------|
| BasePlayerHP | 100 | 100 |
| BasePlayerAttack | 10 | 10 |
| BasePlayerDefense | 5 | 5 |
| BasePlayerSpeed | 100 | 100 |
| BasePlayerCritChance | 0.05 | 0.05 |
| BasePlayerCritDamage | 1.5 | 1.5 |
| ExpMultiplier | 1.2 | 1.2 |
| MonsterStatPerStage | 1.1 (지수) | 1.1 (지수) |
| MinDamage | - | 1 |

### 2. CombatSystem.cs 수정
- [x] 공격 애니메이션 상태 초기화 추가 (OnEnterPhase(COMBAT))
- [x] VICTORY→MOVING 전환 수정
- [x] DealDamageToMonster: Web 공식 적용 (max(1, attack - 5) * stageMultiplier)
- [x] AutoCombat Damage Bonus 활성화 (2%/레벨, 최대 100%)
- [x] GameState.AddExperience() 사용으로 변경

### 3. MonsterFactory.cs 수정
- [x] 1.1^(stage-1) 스케일링 공식 적용 (Web과 동일)

### 4. UIGameRenderer.cs 수정
- [x] 몬스터 슬라이딩 애니메이션 (MOVING 페이즈에서 -50%→12%)
- [x] VICTORY에서 플레이어 idle 유지
- [x] IDLE에서 몬스터 숨김
- [x] HP 바/이름 Label 크기 3.5배 확대
- [x] 플레이어 스프라이트 1.5배 확대 (55%→82.5%)
- [x] 데미지 텍스트 UI 구현 완료

### 5. DropTable.cs (Web 버전 로직)
- [x] items.csv 기반 드롭 로직 구현
- [x] grade + type 매칭
- [x] 가중치 기반 드롭 (70%, 20%, 7%, 2.5%, 0.5%)
- [x] stage 기반 grade 범위 결정 (stage >= 91이면 Mythril tier)
- [x] stats JSON 파싱

### 6. UIManager.cs 수정
- [x] OnPlayerStatChanged()에서 HP, Level, EXP 모두 업데이트
- [x] PLAYER_LEVEL_UP 이벤트 핸들링 추가

### 7. CombatLogManager.cs 수정
- [x] GOLD_CHANGED 이벤트 구독
- [x] ITEM_ACQUIRED, ITEM_DISCOVERED 이벤트 구독
- [x] PLAYER_LEVEL_UP 이벤트 구독
- [x] GEM_CHANGED 이벤트 구독

### 8. Boss First-Clear Gem Rewards
- [x] StatsData에 clearedBossStages 추가 (List<int>)
- [x] HasClearedBossStage(), AddClearedBossStage() 메서드 추가
- [x] GameState에 CalculateBossGemReward() 추가 (10층=5개, 20층=10개...)
- [x] IGameState 인터페이스에 CalculateBossGemReward() 추가
- [x] CombatSystem.ProcessVictory()에서 보스 첫 클리어 시 보석 보상

### 9. hpDouble 버프
- [x] GetTotalHealth()에 hpDouble 버프 적용

---

## 진행 중 (Pending)

### Item Synthesis System
- **우선순위**: 중간
- **Web 버전 InventorySystem.js 기능**:
  - `synthesize(itemId)`: 5개→1개 합성
  - `synthesizeAllByType(type)`: 타입별 자동 합성
  - `findNextGradeItem()`: 다음 등급 아이템 찾기
- **Unity에 해당 클래스 없음**

---

## Web 버전 대비 미구현 (우선순위 낮음)

| 기능 | 상태 | 비고 |
|------|------|------|
| Move Speed → Animation Timing | 미구현 | 고정 시간 사용 |
| Tutorial System | 미구현 | UI 없음 |
| Data Export/Import | 미구현 | localStorage 처리 |
| Boots Equipment Slot | 다름 | Unity는 5슬롯, Web은 4슬롯 |

---

## Git 히스토리

| 커밋 | 내용 |
|------|------|
| 0d3840c | Boss First-Clear Gem Rewards 구현 |
| c184435 | Web to Unity 포팅 진행 상황 문서화 및 hpDouble 버프 |
| d850caa | DropTable, CombatSystem, UIManager 동기화 |
| 6169cec | CombatLogManager 이벤트 연동 |
| 70c34b8 | Web 버전 밸런스 및 애니메이션 시스템 동기화 |
| b036dee | COMBAT 페이즈 진입 시 디버그 로그 추가 |
| 149b8a7 | 명시적 display style 및 로깅 추가 |
