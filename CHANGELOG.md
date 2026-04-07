# Changelog

모든 중요한 변경사항은 이 파일에 기록됩니다.

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
- 아이템 15 종 (5 등급 × 3 타입)
- 몬스터 20 종 (일반 18, 보스 2)
- 스테이지 20 개 (보스 2 개)

#### Infrastructure
- Git 커밋 컨벤션 수립
- Agent.md 워크플로우 문서
- CombatSystem 상세 문서

### 🔧 Changed

- GameState 통합 (플레이어, 인벤토리, 설정 일원화)
- main.js 시스템 통합 (7 개 시스템 초기화)

### 📝 Fixed

-.tasks.md 체크박스 업데이트

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
