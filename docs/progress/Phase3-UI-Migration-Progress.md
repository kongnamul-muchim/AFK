# Phase 3 - UI Toolkit 이식 진행 상황

## 개요
- **목표**: 웹 버전 Idle RPG의 UI를 Unity UI Toolkit으로 이식
- **기간**: 2024년 4월 10일
- **상태**: 진행 중 (UI 표시 및 레이아웃 조정 단계)

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
- [x] MainGameUI.uxml 생성 (393줄)
  - 로딩 화면
  - 게임 컨테이너 (HUD 상/하단, 메뉴 버튼)
  - 모달 UI (인벤토리, 설정, 업그레이드, 미션, 상점, 오프라인보상, 통계)
- [x] GameUIStyle.uss 생성 (761줄)
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

## 현재 문제점 (디버깅 중)

### 1. 인벤토리 장비 슬롯 미표시
- **증상**: 인벤토리 모달에서 장비 슬롯(무기/갑옷/장신구/신발)이 보이지 않음
- **원인 추정**: flex 레이아웃 또는 표시 여부(style.display) 문제
- **해결 방향**: UXML 구조 확인, USS flex 속성 조정
- **진행 상황**: ✅ `.stats-bonus-panel`의 flex-direction을 `row`로 수정 완료 (기존 column → row)

### 2. 업그레이드/미션/상점 탭 UI 미표시
- **증상**: 모달 Panel만 보이고 탭별 아이템 그리드가 표시되지 않음
- **원인 추정**: items-grid, upgrade-grid 등의 레이아웃 설정 누락
- **실제 원인**: UIManager에 아이템을 생성해서 그리드에 추가하는 코드가 없음
- **해결 방향**: 인벤토리/업그레이드/미션 아이템 동적 생성 로직 구현 필요

### 3. 인벤토리 스탯 표시 오류
- **증상**: 공격력/방어력/체력/이동속도가 4줄이 아닌 한줄로 표시됨
- **원인**: .stats-bonus-panel의 flex-direction이 column으로 설정됨
- **해결 상태**: ✅ 수정 완료 (row + flex-wrap으로 변경)

### 4. 자동진행 버튼 위치
- **증상**: 자동반복 버튼이 스테이지 텍스트 옆이 아닌 아래에 위치
- **원인 추정**: .stage-info의 flex-direction 또는 align-items 설정 문제
- **확인 결과**: USS에서 .stage-info는 `flex-direction: row`로 정상. 실제 런타임 문제일 가능성

### 5. 통계 창 크기
- **증상**: 통계 모달이 화면에 비해 너무 큼
- **원인**: .statistics-modal-content의 min-width가 1250px로 너무 큼
- **해결 상태**: ✅ 수정 완료 (1250px → 900px)

## 다음 작업

### 1. UI 레이아웃 디버깅 (우선순위 높음)
- [x] 인벤토리 스탯 4줄 표시 수정 (완료: flex-direction row 변경)
- [x] 통계 창 크기 조정 (완료: 1250px → 900px)
- [ ] 인벤토리 장비 슬롯 표시 문제 해결 (테스트 필요)
- [ ] 자동진행 버튼 위치 조정 (런타임 확인 필요)

### 2. UI-게임 로직 연동 강화 (핵심 작업)
- [ ] 인벤토리 아이템 실제 표시 (동적 생성 로직 구현)
- [ ] 업그레이드 항목 실제 표시 (동적 생성 로직 구현)
- [ ] 미션 목록 실제 표시 (동적 생성 로직 구현)
- [ ] 상점 아이템 실제 표시 (동적 생성 로직 구현)
- [ ] 버튼 클릭 이벤트 처리 (장착, 합성, 구매 등)

### 3. 최적화
- [ ] gap 속성 warning 해결 (margin으로 대체 고려)
- [ ] USS 파일 정리 및 최적화
- [ ] 불필요한 스타일 제거

### 3. 최적화
- [ ] gap 속성 warning 해결 (margin으로 대체 고려)
- [ ] USS 파일 정리 및 최적화
- [ ] 불필요한 스타일 제거

## 기술적 고려사항

### Unity 6 호환성
- `gap` 속성이 공식 지원되지 않아 warning 발생 (동작은 함)
- `text-align` 대신 `-unity-text-align` 사용 필요
- `FindObjectOfType` 대신 `FindFirstObjectByType` 사용 (obsolete 경고)

### FHD 최적화
- Reference Resolution: 1920x1080
- 폰트 크기: 웹 버전 대비 2.5배
- 패딩/마진: 웹 버전 대비 2.5배

## Git 커밋 이력
- `feat: complete Phase 3 - UI migration` (예정)

## 비고
- AFK-Unity 폴더는 백업용으로 유지
- Tests 폴더는 Unity Test Framework 설치 후 재생성 예정
- 로딩 화면 → 게임 화면 전환 로직 정상 동작 확인됨
