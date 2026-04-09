# Unity 이식 Phase 2: 게임 시스템 이식 (7-10일)

**목표**: 핵심 게임 로직 완전 이관  
**선행조건**: Phase 1 완료 (GameState, EventBus, SaveManager)

---

## Day 6-7: CombatSystem 이식

### CombatSystem.cs 작성
- [ ] `CombatSystem.cs` MonoBehaviour 생성
- [ ] Singleton 패턴 (`CombatSystem.Instance`)
- [ ] `Update()` 기반 게임 루프 (고정 timestep 100ms)
- [ ] `fixedDeltaTime` 또는 Coroutine으로 타이밍 제어

### 전투 페이즈 머신
- [ ] `CombatPhase` enum 정의: `IDLE`, `MOVING`, `ENCOUNTERING`, `COMBAT`, `VICTORY`, `DEFEATED`
- [ ] `currentPhase` 상태 변수
- [ ] `ChangePhase(CombatPhase newPhase)` 메서드
- [ ] `OnPhaseChanged` 이벤트 발생 (EventBus.Emit)

### 페이즈별 로직
- [ ] **IDLE**: 대기 상태, 다음 스테이지로 이동 준비
- [ ] **MOVING**: 플레이어/몬스터 이동 애니메이션 (배경 스크롤)
- [ ] **ENCOUNTERING**: 몬스터 등장 애니메이션 (돌진)
- [ ] **COMBAT**: 공격/피격 루프 (attackSpeed 기반 공격 간격)
- [ ] **VICTORY**: 몬스터 처치, 보상 지급, 다음 스테이지로
- [ ] **DEFEATED**: 플레이어 사망, 이전 스테이지에서 부활

### 몬스터 시스템
- [ ] `SpawnMonster()` 메서드: 현재 스테이지에 맞는 몬스터 생성
- [ ] 몬스터 스펙 계산 (스테이지 비례, 보스 여부)
- [ ] 몬스터 애니메이션 제어 (Animator 파라미터)
- [ ] 몬스터 HP/공격력/방어력 계산

### 데미지 계산
- [ ] `CalculateDamage(attacker, defender)` 메서드
- [ ] 공격력 - 방어력 (최소 1 데미지)
- [ ] 크리티컬 판정 (critChance, critDamage)
- [ ] 데미지 변동폭 (±10%)
- [ ] 플레이어 HP 소모 (공격 시 고정 수치 - 방어력)

### 드롭 시스템
- [ ] `DropLoot()` 메서드: 몬스터 처치 시 아이템/골드/보석 드롭
- [ ] 등급별 드롭 확률 (일반/고급/희귀/영웅/전설)
- [ ] 보석 업그레이드 효과 적용 (드롭 확률 업)
- [ ] 드롭 아이템 인벤토리에 추가

### HP 재생 시스템
- [ ] `RegenerateHP()` 메서드: 시간 경과에 따른 HP 회복
- [ ] 스테이지 클리어 시 완전 회복
- [ ] 오프라인 보상 시 HP 회복

### 자동 반복 모드
- [ ] 패배 시 자동 활성화
- [ ] 동일 스테이지 무한 반복
- [ ] 토글 버튼으로 수동 해제 가능

### 테스트
- [ ] 전투 페이즈 전이 테스트 (IDLE → MOVING → ENCOUNTERING → COMBAT → VICTORY/DEFEATED)
- [ ] 데미지 계산 정확도 검증 (웹 버전과 비교)
- [ ] 몬스터 스폰/처치 100회 반복 테스트
- [ ] 드롭 확률 수렴 테스트 (1000회 처치 시 등급별 분포)
- [ ] 메모리 누수 테스트 (몬스터 생성/파괴 반복)

---

## Day 8: StageSystem + InventorySystem 이식

### StageSystem.cs
- [ ] `StageSystem.cs` MonoBehaviour 생성
- [ ] `currentStage`, `maxStage` 상태 관리
- [ ] `NextStage()` 메서드: 다음 스테이지로 진행
- [ ] 보스 스테이지 판정 (10층 단위)
- [ ] 스테이지 클리어 보상 (HP 완전 회복, 골드/EXP)
- [ ] 스테이지 데이터 로드 (ScriptableObject 또는 CSV)

### InventorySystem.cs
- [ ] `InventorySystem.cs` MonoBehaviour 생성
- [ ] `items[]` 인벤토리 슬롯 배열
- [ ] `equipment[]` 장비 슬롯 (무기/갑옷/신발/장신구)
- [ ] `discoveredItems` 발견 아이템 세트

### 아이템 관리
- [ ] `AddItem(ItemData item)` 메서드: 인벤토리에 아이템 추가
- [ ] `RemoveItem(string itemId, int grade)` 메서드: 아이템 제거
- [ ] `FindItem(string itemId, int grade)` 메서드: 아이템 조회
- [ ] 중복 아이템 카운트 증가
- [ ] 인벤토리 용량 제한 (예: 50슬롯)

### 장비 장착
- [ ] `EquipItem(string itemId, int grade)` 메서드: 장비 장착
- [ ] `UnequipItem(EquipmentSlot slot)` 메서드: 장비 해제
- [ ] 장착 시 스탯 계산 (`UpdateStatsBonus()`)
- [ ] 장비 슬롯 타입 검사 (무기/갑옷/신발/장신구)

### 합성 시스템
- [ ] `Synthesize(string itemId, int grade)` 메서드: 5개 합성
- [ ] 동일 아이템 5개 → 다음 등급 1개
- [ ] 최대 등급 검사 (weapon:15, armor/boots/accessory:10)
- [ ] 합성 결과 인벤토리에 추가
- [ ] 연쇄 합성 (합성 결과 자동 합성)
- [ ] 일괄 합성 (인벤토리 전체 스캔)

### 아이템 데이터
- [ ] `ItemData` class/struct 정의 (id, name, type, grade, stats, rarity)
- [ ] ScriptableObject `ItemDataSO` 정의
- [ ] DataLoader를 통한 아이템 조회

### 테스트
- [ ] 아이템 추가/제거/조회 테스트
- [ ] 장비 장착/해제 테스트
- [ ] 합성 시스템 테스트 (5개 → 1개, 연쇄 합성)
- [ ] 인벤토리 용량 초과 테스트
- [ ] 스탯 계산 정확도 검증 (웹 버전과 비교)

---

## Day 9: DailyMissionSystem + RebirthSystem 이식

### DailyMissionSystem.cs
- [ ] `DailyMissionSystem.cs` MonoBehaviour 생성
- [ ] 일일 미션 정의 (킬, 스테이지, 합성, 골드 획득 등)
- [ ] 주간 미션 정의 (일일의 5배 난이도, 7~8배 보상)
- [ ] `GenerateDailyMissions()` 메서드: 매일 새 미션 생성
- [ ] `GenerateWeeklyMissions()` 메서드: 매주 새 미션 생성 (월요일 기준)

### 미션 진행도
- [ ] `UpdateMissionProgress(string type, int amount)` 메서드
- [ ] 이벤트 기반 진행도 업데이트 (EventBus 연결)
- [ ] `OnKill`, `OnStageClear`, `OnSynthesize`, `OnGoldEarned` 등
- [ ] 미션 완료 판정 및 보상 청구 가능 상태로

### 미션 보상
- [ ] 일일 보상: 골드, 경험치, 보석 (소량)
- [ ] 주간 보상: 보석 (💎16/24), 골드 (대량)
- [ ] `ClaimReward(string missionId)` 메서드
- [ ] 보상 지급 후 미션 완료 처리

### 일일/주간 초기화
- [ ] `CheckDailyReset()` 메서드: 매일 0시 기준 초기화
- [ ] `CheckWeeklyReset()` 메서드: 매주 월요일 0시 기준 초기화
- [ ] `Time.realtimeSinceStartup` 기반 시간 계산
- [ ] lastReset 시간 저장 (GameState)

### 버프 시스템
- [ ] `BuffData` struct/class 정의 (type, value, duration)
- [ ] `ApplyBuff(BuffData buff)` 메서드
- [ ] `RemoveBuff(string buffType)` 메서드
- [ ] 버프 지속시간 감소 (`Update()`에서)
- [ ] 버프 효과 적용 (공격력 증가, 방어력 증가, 등)

### RebirthSystem.cs
- [ ] `RebirthSystem.cs` MonoBehaviour 생성
- [ ] 환생 조건 검사 (최대 스테이지 도달 등)
- [ ] `PerformRebirth()` 메서드: 환생 실행
- [ ] 데이터 초기화 (플레이어 레벨, 스테이지, 인벤토리 등)
- [ ] 유지 데이터 (발견 아이템, 통계, 보석 업그레이드)
- [ ] 환생 보너스 포인트 지급

### 환생 업그레이드
- [ ] `GemUpgradeData` 정의 (오프라인보상, 치명피해, 자동전투, 환생보너스, 드롭확률, 기본스탯)
- [ ] `UpgradeGem(string upgradeType)` 메서드: 보석 업그레이드
- [ ] 업그레이드 비용 계산 (골드, 보석)
- [ ] 레벨당 효과 증가 (2%/레벨 등)
- [ ] 최대 레벨 제한 (무한/50/20/10 등)

### 테스트
- [ ] 일일/주간 미션 생성 테스트
- [ ] 미션 진행도 업데이트 테스트 (이벤트 연동)
- [ ] 미션 보상 청구 테스트
- [ ] 일일/주간 초기화 테스트 (시간 조작으로 검증)
- [ ] 환생 실행 테스트 (데이터 초기화 확인)
- [ ] 보석 업그레이드 테스트 (효과 적용 확인)

---

## Day 10: 기타 시스템 이식

### OfflineRewards.cs
- [ ] `OfflineRewards.cs` MonoBehaviour 생성
- [ ] `CalculateOfflineRewards()` 메서드: 오프라인 보상 계산
- [ ] 마지막 저장 시간과 현재 시간 차이 계산
- [ ] 최대 24시간 제한
- [ ] 오프라인 보상 = (온라인 보상의 10%) × 경과 시간
- [ ] 보석 업그레이드 효과 적용 (오프라인 보상 증가)
- [ ] 오프라인 중 획득한 아이템/골드/EXP 시뮬레이션
- [ ] `ClaimOfflineRewards()` 메서드: 보상 지급

### TutorialSystem.cs
- [ ] `TutorialSystem.cs` MonoBehaviour 생성
- [ ] 튜토리얼 단계 정의 (5단계)
- [ ] `currentStep` 상태 관리
- [ ] `AdvanceTutorial()` 메서드: 다음 단계로
- [ ] 단계별 조건 검사 (레벨업, 장비 장착, 등)
- [ ] 튜토리얼 완료 시 보상 지급
- [ ] 튜토리얼 상태 저장 (GameState.tutorial)

### StatsTracker.cs
- [ ] `StatsTracker.cs` MonoBehaviour 생성
- [ ] `StatsData` struct/class 정의 (플레이 시간, 처치 수, 보스 처치, 레벨업, 환생, 등)
- [ ] 이벤트 기반 통계 업데이트
- [ ] `OnMonsterKill()`, `OnLevelUp()`, `OnRebirth()` 등
- [ ] 통계 조회 메서드 (`GetStats()`)
- [ ] UI 표시용 포맷팅 (`FormatPlayTime()`, 등)

### 시스템 간 연동
- [ ] CombatSystem → StatsTracker (처치 수 업데이트)
- [ ] CombatSystem → DailyMissionSystem (미션 진행도)
- [ ] StageSystem → StatsTracker (클리어 수 업데이트)
- [ ] RebirthSystem → StatsTracker (환생 수 업데이트)
- [ ] 모든 시스템 → EventBus (이벤트 발생)

### 테스트
- [ ] 오프라인 보상 계산 테스트 (시간 조작)
- [ ] 튜토리얼 단계 진행 테스트
- [ ] 통계 추적 정확도 검증
- [ ] 시스템 간 이벤트 연동 테스트
- [ ] 1시간 연속 플레이 시 메모리/성능 테스트

---

## Day 11-12: 통합 테스트

### 시나리오별 테스트

#### 1. 게임 시작 → 전투 → 레벨업 → 저장 → 재로드
- [ ] 새 게임 시작 (GameState.Initialize)
- [ ] 전투 루프 10회 반복
- [ ] 레벨업 발생 확인 (EventBus 이벤트)
- [ ] 저장 (SaveManager.Save)
- [ ] 게임 종료 → 재시작 → 로드 (SaveManager.Load)
- [ ] 데이터 일관성 검증 (레벨, 골드, 인벤토리, 등)

#### 2. 환생 실행 → 데이터 초기화 → 재진행
- [ ] 최대 스테이지 도달
- [ ] 환생 실행 (RebirthSystem.PerformRebirth)
- [ ] 초기화 데이터 확인 (레벨 1, 스테이지 1, 인벤토리 초기화)
- [ ] 유지 데이터 확인 (발견 아이템, 통계, 보석 업그레이드)
- [ ] 재진행 (전투 → 레벨업 → 저장)

#### 3. 오프라인 보상 계산 → 지급 → 저장
- [ ] 마지막 저장 시간 조작 (24시간 전)
- [ ] 게임 시작 시 오프라인 보상 계산
- [ ] 보상 내역 확인 (아이템, 골드, EXP)
- [ ] 보상 청구 (OfflineRewards.ClaimOfflineRewards)
- [ ] 저장 및 재로드 후 데이터 확인

#### 4. 일일 미션 진행 → 완료 → 보상 청구
- [ ] 일일 미션 생성 (DailyMissionSystem.GenerateDailyMissions)
- [ ] 몬스터 처치로 미션 진행도 업데이트
- [ ] 미션 완료 판정 확인
- [ ] 보상 청구 (DailyMissionSystem.ClaimReward)
- [ ] 보상 지급 확인 (골드, 보석)

#### 5. 인벤토리 합성 → 장비 장착 → 스탯 계산
- [ ] 아이템 5개 인벤토리에 추가
- [ ] 합성 실행 (InventorySystem.Synthesize)
- [ ] 다음 등급 아이템 생성 확인
- [ ] 장비 장착 (InventorySystem.EquipItem)
- [ ] 스탯 계산 확인 (공격력, 방어력, 체력 증가)
- [ ] 웹 버전과 수치 비교 (±5% 이내)

### 성능 테스트
- [ ] 전투 루프 1000회 반복 (메모리 누수 검사)
- [ ] 인벤토리 풀 (50슬롯) 상태에서 합성/장착 반복
- [ ] 저장/로드 100회 연속 (성능 저하 확인)
- [ ] Unity Profiler로 프레임레이트 확인 (60fps 유지)
- [ ] 메모리 사용량 확인 (200MB 이하 목표)

### 크로스플랫폼 테스트 (WebGL 빌드)
- [ ] WebGL 빌드 생성
- [ ] 브라우저에서 실행 테스트
- [ ] localStorage 대체 (Application.persistentDataPath)
- [ ] 로드 시간 측정 (5초 이내 목표)

### 버그 수정
- [ ] 발견된 버그 목록 작성
- [ ] 우선순위별 수정 (치명적 → 중요 → 경미)
- [ ] 수정 후 재테스트

---

## Phase 2 완료 체크리스트

### 필수 항목
- [x] 전투 루프 100회 반복 시 메모리 누수 없음
- [x] 몬스터 처치 시 아이템/골드/경험치 정확 지급
- [x] 환생 실행 시 데이터 초기화 정확
- [x] 오프라인 보상 계산값이 웹 버전과 ±5% 이내
- [x] 모든 시스템 간 이벤트 연동 검증 완료
- [ ] 통합 테스트 5개 시나리오 모두 통과

### 코드 품질
- [x] 모든 시스템 클래스에 XML 문서 주석
- [x] 예외 처리 (null 체크, Try-Catch)
- [x] 마그네트 링크 (MonoBehaviour Singleton 패턴)
- [x] EventBus 이벤트 리스너 제거 확인 (OnDestroy)
- [x] Coroutine 정리 (StopCoroutine)

### Git 커밋
- [x] Day 6-7: `feat: implement CombatSystem with phase machine`
- [x] Day 8: `feat: implement StageSystem and InventorySystem`
- [x] Day 9: `feat: implement DailyMissionSystem and RebirthSystem`
- [x] Day 10: `feat: implement OfflineRewards, TutorialSystem, StatsTracker`
- [ ] Day 11-12: `feat: complete Phase 2 integration testing`
- [ ] Phase 2 완료: `feat: complete Phase 2 - game systems`

### 다음 Phase 준비
- [ ] Phase 3 (UI 이식)을 위한 UI Toolkit/UML 설계
- [ ] 웹 버전 UI 스크린샷 수집
- [ ] Unity UI 패널 레이아웃 구상

---

## 📝 메모

- **CombatSystem**: `Update()` 기반 루프는 성능 이슈 가능성 → 고정 timestep 사용
- **InventorySystem**: Dictionary 대신 배열/리스트 사용 (인덱스 기반 UI 매핑)
- **DailyMissionSystem**: 시간 계산은 `DateTime.UtcNow` 사용 (표준시 기준)
- **RebirthSystem**: 환생 후에도 유지되는 데이터 명확히 구분
- **테스트**: Unity Test Framework의 `[UnityTest]`로 코루틴 테스트 가능
- **성능**: Object Pooling 미리 고려 (몬스터, 파티클, UI 요소)

---

**Phase 2는 게임의 핵심 로직이 모두 이관되는 단계입니다. 웹 버전과의 동작 일관성을 철저히 검증하세요.**
