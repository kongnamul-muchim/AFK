# Phase 3 - UI Toolkit 이식 진행 상황

## 개요
- **목표**: 웹 버전 Idle RPG의 UI를 Unity UI Toolkit으로 이식
- **기간**: 2024년 4월 10일
- **상태**: ✅ 완료 (모든 UI 이식 완료, 시스템 통합 완료)

## 완료된 작업

### 1. 코어 시스템 통합
- [x] AFK-Unity 폴더의 코어 스크립트를 메인 프로젝트로 통합
- [x] Bootstrap, GameState, EventBus, SaveManager 등 코어 시스템 이식 완료
- [x] Tests 폴더 제거 (Unity Test Framework 미설치)

### 2. UI Toolkit 기본 설정
- [x] PanelSettings Asset 생성 (ReferenceResolution: 1920x1080)
- [x] UIDocument Scene 설정
- [x] Canvas Scaler 설정 (Scale With Screen Size, Match 0.5)
- [x] UIManager, PopupManager 스크립트 구현

### 3. UI 마크업 및 스타일
- [x] MainGameUI.uxml 생성 (403줄)
  - 로딩 화면
  - 게임 컨테이너 (HUD 상/하단, 메뉴 버튼)
  - 모달 UI (인벤토리, 설정, 업그레이드, 미션, 상점, 오프라인보상, 통계)
- [x] GameUIStyle.uss 생성 (798줄)
  - FHD 최적화 (2.5배 폰트/패딩)
  - Unity 6 호환 (gap 속성 warning 있음)

### 4. Bootstrap 통합
- [x] Scene에 Bootstrap GameObject 추가
- [x] GAME_LOADED 이벤트 발생 → UIManager 구독
- [x] 로딩 화면 → 게임 화면 자동 전환 구현

### 5. UI-게임 로직 연동
- [x] UIManager.UpdateAllUI() 구현
- [x] GameState 데이터 → UI 실시간 업데이트
- [x] 이벤트 기반 UI 갱신 시스템

### 6. 인벤토리 UI 카드 그리드 변환
- [x] ListView → ScrollView + 카드 그리드로 변경
- [x] 1행 5개 자동 줄바꿈 레이아웃
- [x] 아이템 슬롯 정사각형 유지 (GeometryChangedEvent)
- [x] 커스텀 스크롤 구현 (마우스 휠 + 드래그)
- [x] 희귀도별 테두리 색상 적용
- [x] 아이템 아이콘 이모지 표시 (⚔️🛡️👢💍)

### 7. 업그레이드 UI 카드 리스트 구현
- [x] 4개 탭 (골드/스탯/보석/환생) 구현
- [x] ScrollView 기반 카드 리스트
- [x] 골드/스탯 업그레이드 카드 생성
- [x] 보석 업그레이드 카드 생성
- [x] 환생 업그레이드 카드 생성
- [x] 헤더 재화 표시 (💰G, ⭐SP, 💎宝石, 🎁PT)

### 8. 미션 UI 카드 리스트 구현
- [x] 일일/주간 탭 구현
- [x] ScrollView 기반 카드 리스트
- [x] 미션 진행바 및 보상 청구 버튼
- [x] 갱신 타이머 (1초 단위 업데이트)

### 9. 보석 상점 UI 구현
- [x] 버프 상점 카드 리스트
- [x] 4개 버프 아이템 (공격력 2배, 체력 2배, 골드 2배, 경험치 2배)
- [x] 보석 비용 표시 및 구매 버튼

### 10. 통계 UI 4분할 레이아웃
- [x] 플레이어 스탯, 진행 상황, 재화 정보, 기타 정보 섹션
- [x] 세로 40px 간격으로 띄엄띄엄 배치
- [x] 스크롤 가능한 레이아웃

### 11. UI 파일 분리 (지연 로딩 준비)
- [x] 모달별 별도 UXML 파일 생성
  - InventoryModal.uxml
  - UpgradeModal.uxml
  - MissionsModal.uxml
  - GemShopModal.uxml
  - StatisticsModal.uxml
  - SettingsModal.uxml
- [x] Resources/UXML/modals/ 폴더 구조

### 12. CombatSystem HP 재생 시스템 추가
- [x] Web 버전의 updateHpRegen 로직 이식
- [x] 1초마다 hpRegen 값만큼 회복
- [x] 효율 배율 적용 (10레벨마다 증가)
- [x] 모든 페이즈에서 적용

### 13. CombatSystem 공격 반동 데미지 시스템
- [x] Web 버전의 consumePlayerHP 로직 이식
- [x] 공격 시 스테이지당 4 데미지 - 방어력 공식 적용
- [x] 최소 1 데미지 보장
- [x] 몬스터 처치 시 반동 데미지 없음

### 14. DailyMissionSystem 버프 시스템 구현
- [x] HasActiveBuff(): 버프 활성화 여부 확인
- [x] ActivateBuff(): 버프 활성화 (지속 시간 설정)
- [x] GetBuffMultiplier(): 버프 배율 반환 (2.0 or 1.0)
- [x] GetRemainingBuffTime(): 남은 버프 시간 (초)
- [x] EventBus에 BUFF_ACTIVATED, BUFF_EXPIRED 이벤트 추가

### 15. CombatSystem 버프 연동
- [x] GetBuffMultiplier() 메서드 추가
- [x] PlayerAttack()에서 attackDouble 버프 적용
- [x] CalculateDamage()에 buffMultiplier, autoCombatBonus 파라미터 추가

### 16. GemShopUI 버프 구매 연동
- [x] HasActiveBuff()에서 DailyMissionSystem 확인
- [x] PurchaseBuff()에서 ActivateBuff() 호출
- [x] GEM_CHANGED 이벤트 발생

### 17. CombatSystem 보석 드롭 시스템
- [x] RollGemDrop() 메서드 추가
- [x] 0.1% 확률로 보석 1개 드롭
- [x] 자동 반복 모드에서는 드랍되지 않음

## 기술적 고려사항

### Unity 6 호환성
- `gap` 속성이 공식 지원되지 않아 warning 발생 (동작은 함)
- `text-align` 대신 `-unity-text-align` 사용 필요
- `FindObjectOfType` 대신 `FindFirstObjectByType` 사용 (obsolete 경고)
- `borderRadius` 대신 개별 `borderTopLeftRadius` 등 사용

### FHD 최적화
- Reference Resolution: 1920x1080
- 폰트 크기: 웹 버전 대비 2.5배
- 패딩/마진: 웹 버전 대비 2.5배

## Git 커밋 이력
- `feat: complete Phase 3 UI migration and add HP regen system` (완료)
- `feat: complete Phase 3 - UI migration` (완료)
- `feat: 인벤토리 아이템 동적 생성 로직 구현 및 통계 모달 레이아웃 개선` (완료)
- `feat: 업그레이드/미션 탭 동적 생성 로직 구현` (완료)
- `fix: Unity UI Toolkit 스타일 속성 호환성 수정 (padding, borderRadius, PointerEvent)` (완료)
- `fix: PointerEvent -> MouseDownEvent로 변경 (Unity UI Toolkit 호환성)` (완료)
- `feat: ListView 기반 Infinity Scroll 구현 (인벤토리/업그레이드/미션 리스트로 변경)` (완료)
- `fix: using System.Collections.Generic; 추가` (완료)
- `fix: OnUpgradePurchase 메서드 추가 및 clicked 이벤트 수정` (완료)
- `fix: ListView 템플릿 제거 (코드에서 makeItem/bindItem으로 완전 제어)` (완료)
- `fix: ILogger -> IGameLogger 변경 (UnityEngine.ILogger와 충돌 해결)` (완료)
- `feat: items.csv 데이터 로딩 및 모든 아이템 잠금 상태로 추가` (완료)

## 다음 단계 (Phase 4)

### 1. CombatSystem Web 버전 로직 이식 ✅ 완료
- [x] 공격 반동 데미지 시스템
- [x] 버프 시스템 연동 (attackDouble, goldDouble, expDouble)
- [x] 보석 업그레이드 연동 (autoCombatDamage)
- [x] 아이템 드롭 로직 (기존 DropTable 구현)
- [x] 보석 드롭 (0.1% 확률)

### 2. DailyMissionSystem 이식 ✅ 완료
- [x] 미션 진행도 업데이트
- [x] 버프 시스템 구현
- [x] 오프라인 보상 계산 (StatCalculator 구현)

### 3. UI 지연 로딩 시스템 ✅ 완료
- [x] UXMLLoader: Resources/UXML/modals/ 폴더의 UXML 파일을 동적으로 로드하고 캐싱
- [x] ModalManager에 ShowModalLazy() 메서드 추가 (지연 로딩)
- [x] UnloadModal(), UnloadAllModals() 메서드 추가 (메모리 관리)
- [x] 캐시 상태 확인: IsLoaded(), GetCached(), LogCacheStatus()

## 비고
- AFK-Unity 폴더는 백업용으로 유지
- Tests 폴더는 Unity Test Framework 설치 후 재생성 예정
- 로딩 화면 → 게임 화면 전환 로직 정상 동작 확인됨
- 모든 UI가 Web 버전과 동일한 카드 그리드/리스트 레이아웃으로 이식됨

