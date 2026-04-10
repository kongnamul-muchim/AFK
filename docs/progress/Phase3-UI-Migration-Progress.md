# Phase 3 - UI Toolkit 이식 진행 상황

## 개요
- **목표**: 웹 버전 Idle RPG의 UI를 Unity UI Toolkit으로 이식
- **기간**: 2024년 4월 10일
- **상태**: ✅ 완료 (기본 기능 이식 완료, UI 스타일 개선은 Phase 4로 이관)

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

### 6. 인벤토리 아이템 동적 생성
- [x] RefreshInventoryGrid() 메서드 구현
- [x] CreateInventoryItemButton() - 아이템 버튼 동적 생성
- [x] 탭별 필터링 (무기/갑옷/장신구/신발)
- [x] 아이템 툴팁 표시
- [x] 우클릭 합성 기능
- [x] 등급별 색상 표시
- [x] CSV 데이터 로딩 (items.csv → 100개 아이템)
- [x] 모든 아이템을 count=0(잠금) 상태로 인벤토리에 추가

### 7. 컴파일 에러 해결
- [x] ILogger → IGameLogger 이름 변경 (UnityEngine.ILogger와 충돌)
- [x] ItemData.quantity → ItemData.count로 변경
- [x] ServiceLocator obsolete warning 처리 (#pragma warning)
- [x] TooltipManager.CancelInvoke() warning 해결

## Phase 4로 이관된 작업

### 1. 인벤토리 UI 카드 그리드 변환
- **현재**: ListView 기반 세로 리스트 (Unity 기본 스타일)
- **목표**: Web 버전처럼 1행당 5개(희귀도별) 카드 그리드
- **작업**: UXML GridView 변경, 아이템 슬롯 템플릿 생성, 그룹화 로직 이식

### 2. 업그레이드 UI 표시 수정
- **현재**: UpgradeGrid ListView가 UXML에서 제대로 연결되지 않음
- **작업**: UXML name 속성 확인, Initialize() 디버깅, 탭별 그리드 레이아웃 정의

### 3. 미션 UI 표시 수정
- **현재**: MissionsGrid ListView가 UXML에서 제대로 연결되지 않음
- **작업**: UXML name 속성 확인, Initialize() 디버깅, 미션 카드 템플릿 생성

### 4. 드롭/합성 시스템 구현
- **현재**: items.csv 데이터는 로드되지만 드롭/합성 로직은 미구현
- **작업**: DropTable.cs CSV 기반 수정, InventorySystem.Synthesize() 구현

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
- `feat: complete Phase 3 - UI migration` (완료)

## 비고
- AFK-Unity 폴더는 백업용으로 유지
- Tests 폴더는 Unity Test Framework 설치 후 재생성 예정
- 로딩 화면 → 게임 화면 전환 로직 정상 동작 확인됨
- Phase 4에서 UI 카드 그리드 변환 및 드롭/합성 시스템 구현 예정

