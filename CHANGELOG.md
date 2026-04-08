# Changelog

모든 중요한 변경사항은 이 파일에 기록됩니다.

---

## [2.0.0] - 2026-04-08

### ✨ Added

#### 전투 시스템 대대적 개편
- **이동/조우/전투/처치 루프** 구현 (Player 이동 → 적 조우 → 전투 → 처치 → 이동 반복)
- **공격 애니메이션 3프레임** (1→2→3) 구현, attackSpeed에 비례한 속도 조절
- **데미지 판정 타이밍**: 공격 3번 프레임에서 한 번만 발생
- **체력 소모 시스템**: 공격시 스테이지 비례 고정 수치 - 방어력 만큼 감소
- **패배/부활 시스템**: 체력 0시 쓰러짐, 이전 스테이지에서 50% HP로 부활
- **자동반복 모드**: 패배시 자동 활성화, 동일 스테이지 무한 반복
- **스테이지 클리어 보상**: 체력 완전 회복
- **몬스터 등장 애니메이션**: 0(등장전) → 5(돌진) → 4/6(idle)
- **플레이어/몬스터 Dead 애니메이션**: 6→7, 2→3 (한 번만 재생)
- **배경 스크롤 효과**: 첫 이동/두 번째 이동 방향 구분

#### UI
- **자동반복 모드 토글 버튼**: Stage Text 아래 동그란 🔄 버튼
- **UI null 체크 강화**: formatNumber()에 isNaN() 추가

### 🔧 Changed

- **CombatSystem**: 자동 공격 루프 → 페이즈 기반 전투 시스템
- **GameState**: combatPhase 상태 추가 (phase, playerState, monsterState, 타이머 등)
- **WebRenderer**: 상태별 애니메이션 프레임 매핑, 배경 스크롤
- **EventBus**: COMBAT_PHASE_CHANGED, COMBAT_ENCOUNTER, COMBAT_VICTORY 이벤트 추가
- **DataLoader**: game_config.csv 중복 ID 검사 제외

### 📝 Fixed

- game_config.csv 중복 ID 경고 수정
- 공격 애니메이션 무한 반복 버그 수정
- 플레이어 공격시 체력 소모 타이밍 수정 (데미지 판정과 동기화)
- 몬스터 돌진 타이밍 조정 (이동 80% → 65%)
- 이미지 경로 대소문자 수정 (assets/ → Assets/)
- UI 골드/EXP NaN 표시 문제 수정

---

## [Unreleased] - 2025-04-07

### ✨ Added

#### Core Systems
- **CombatSystem** - 자동 전투 시스템 (100ms 공격 루프, 크리티컬, 드롭)
- **StageSystem** - 스테이지 진행 (10 층 단위 보스, 자동 반복)
- **InventorySystem** - 인벤토리 관리 (중복 카운트, 5 개 합성)
- **OfflineRewards** - 오프라인 보상 (최대 24 시간)
- **TutorialSystem** - 5 단계 튜토리얼 가이드
- **AchievementSystem** - 업적 시스템 (10 개 업적)
- **StatsTracker** - 통계 기록 (플레이 시간, 처치 수 등)

#### Data
- CSV 데이터 7 종 (items, monsters, stages, config, achievements, tutorial, audio)
- 아이템 45 종 (무기 15, 갑옷 10, 신발 10, 장신구 10)
- 몬스터 20 종 (일반 18, 보스 2)
- 스테이지 20 개 (보스 2 개)

#### Infrastructure
- Git 커밋 컨벤션 수립
- Agent.md 워크플로우 문서
- CombatSystem 상세 문서

### 🔧 Changed

- GameState 통합 (플레이어, 인벤토리, 설정 일원화)
- main.js 시스템 통합 (7 개 시스템 초기화)
- 인벤토리 UI 대폭 개선 (탭, 스크롤, 도감)

### 📝 Fixed

- tasks.md 체크박스 업데이트
- discoveredItems 저장/로드 구현 (GameState toJSON/fromJSON)
- 합성 시스템 타입별 최대 등급 (weapon:15, armor/boots/accessory:10)
- 발견 아이템 항상 활성화 (count=0 이어도)
- findNextGradeItem() 상세 디버깅 로그 추가
- **전 합성 경로 정상 확인** (45 개 아이템, 41 단계)
- **getMaxGradeByType() CSV 기반으로 수정** (하드코딩 제거)
- **addItem() type 저장 디버깅** (CSV 에서 읽은 값 그대로)
- **createItemSlot() discovered 체크 강화** (count=0 이어도 활성화)
- UI-코드 일치성 테스트 9/9 통과
- **stats_min → stats 필드 참조 수정** (InventoryUI, InventorySystem, CombatSystem)
- **formatStats() 스탯 표시 수정** (attackBonus/defenseBonus/moveSpeed/hpBonus 지원)
- **updateStatsBonus() 장비 스탯 계산 수정** (새 스탯 구조 적용)
- **인벤토리 UI 탭/장비 패널 순서 조정** (장신구↔신발 교체)
- **합성 테스트 성공 확인** (bronze_sword grade 1-3, iron_sword grade 6)

### 🔧 Changed

- GameState 통합 (플레이어, 인벤토리, 설정 일원화)
- main.js 시스템 통합 (7 개 시스템 초기화)
- 인벤토리 UI 대폭 개선 (탭, 스크롤, 도감)
- **items.csv 재생성** (100 개 아이템, 5 재질 x 5 희귀도 x 4 타입)
- **GameState moveSpeed 스탯 추가** (derivedStats.moveSpeed = 100 + 장비 보너스)
- **스탯 패널 UI 수정** (크리티컬 → 이동속도 변경)
- **장비 장착 시 grade/type 필드 추가 저장**

---

## [0.1.0] - 2025-04-07

### ✨ Added

#### Initial Project Setup
- 프로젝트 구조 (HTML/CSS/JS)
- 코어 시스템 (EventBus, GameState, StorageManager, Logger)
- CSV 파서 (CSVParser, DataLoader)
- UI 시스템 (LoadingScreen, UIManager)
- 렌더링 시스템 (GameRenderer, Canvas)
- 사운드 시스템 (AudioManager)

#### CSV Data Files
- items.csv - 15 개 아이템 데이터
- monsters.csv - 20 개 몬스터 데이터
- stages.csv - 20 개 스테이지 데이터
- game_config.csv - 밸런스 설정
- achievements.csv - 10 개 업적
- tutorial.csv - 5 단계 튜토리얼
- audio_definitions.csv - 11 개 사운드 정의

### 🎨 UI Components
- 로딩 화면 (진행률, 팁, 버전 표시)
- HUD (레벨, HP, 경험치, 골드, 스테이지)
- 모달 (인벤토리, 설정, 상태창)
- 오프라인 보상 모달

### 📄 Documentation
- Agent.md - 개발 워크플로우
- README.md (준비 중)

---

##legend

### Added
- `✨ Added` - 새로운 기능
- `🔧 Changed` - 변경 사항
- `📝 Fixed` - 버그 수정
- `🗑️ Deprecated` - 제거 예정 기능
- `🗑️ Removed` - 삭제된 기능
- `⚡ Performance` - 성능 개선
- `📄 Documentation` - 문서 추가/수정
