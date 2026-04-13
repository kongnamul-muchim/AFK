# 프로젝트 파일 구조 가이드

이 문서는 AFK 프로젝트의 파일 위치를 빠르게 찾을 수 있도록 안내합니다.

## 📁 주요 디렉토리 구조

```
C:\Users\admin\AFK\
├── Assets/                    # Unity 에셋 (메인 작업 폴더)
│   ├── Scripts/
│   │   ├── Core/             # 핵심 시스템
│   │   ├── Systems/          # 게임 시스템
│   │   ├── UI/               # UI 컨트롤러
│   │   └── Tests/            # 테스트 스크립트
│   ├── UI/
│   │   ├── UXML/             # UI 마크업 파일
│   │   └── StyleSheets/      # USS 스타일시트
│   ├── Scenes/               # Unity 씬 파일
│   └── Editor/               # 에디터 스크립트
│
├── AFK-Unity/                # Unity 프로젝트 사본 (참조용)
│   └── Assets/
│
├── src/                      # Web 버전 소스 코드
│   ├── core/                 # 핵심 시스템 (JS)
│   ├── systems/              # 게임 시스템 (JS)
│   ├── ui/                   # UI 컴포넌트 (JS)
│   ├── adapters/             # 어댑터 (Unity 연동)
│   ├── data-parser/          # 데이터 파서
│   ├── config/               # 설정 파일
│   ├── audio/                # 오디오 관리자
│   └── test/                 # 테스트 파일
│
├── docs/                     # 문서화 폴더
│   ├── ai-plans/             # AI 생성 계획
│   ├── architecture/         # 아키텍처 문서
│   ├── guides/               # 가이드 문서
│   ├── progress/             # 진행 상황
│   └── unity-migration-*.md  # Unity 마이그레이션 태스크
│
├── AGENTS.md                 # AI Agent 워크플로우 규칙 (★ 중요)
├── package.json              # npm 설정
└── index.html                # Web 버전 진입점
```

## 🔍 자주 찾는 파일 위치

### Unity 스크립트

| 파일/기능 | 경로 |
|-----------|------|
| UIManager | `Assets/Scripts/UI/Controllers/UIManager.cs` |
| PopupManager | `Assets/Scripts/UI/Controllers/PopupManager.cs` |
| GameState | `Assets/Scripts/Core/GameState.cs` |
| SaveManager | `Assets/Scripts/Core/SaveManager.cs` |
| EventBus | `Assets/Scripts/Core/EventBus.cs` |
| Bootstrap | `Assets/Scripts/Core/Bootstrap.cs` |
| InventorySystem | `Assets/Scripts/Systems/InventorySystem.cs` |
| DailyMissionSystem | `Assets/Scripts/Systems/DailyMissionSystem.cs` |
| CombatSystem | `Assets/Scripts/Systems/CombatSystem.cs` |
| StageSystem | `Assets/Scripts/Systems/StageSystem.cs` |

### Unity UI 파일

| 파일/기능 | 경로 |
|-----------|------|
| 메인 UI 마크업 | `Assets/UI/UXML/MainGameUI.uxml` |
| UI 스타일시트 | `Assets/UI/StyleSheets/GameUIStyle.uss` |
| 메인 씬 | `Assets/Scenes/MainGameScene.unity` |

### Web 소스 코드

| 파일/기능 | 경로 |
|-----------|------|
| GameState (JS) | `src/core/GameState.js` |
| EventBus (JS) | `src/core/EventBus.js` |
| StorageManager | `src/core/StorageManager.js` |
| InventoryUI | `src/ui/InventoryUI.js` |
| UpgradeUI | `src/ui/UpgradeUI.js` |
| DailyMissionUI | `src/ui/DailyMissionUI.js` |
| InventorySystem (JS) | `src/systems/InventorySystem.js` |
| DailyMissionSystem (JS) | `src/systems/DailyMissionSystem.js` |
| UnityBridge | `src/adapters/UnityBridge.js` |

### 문서 파일

| 파일/기능 | 경로 |
|-----------|------|
| AI Agent 규칙 | `AGENTS.md` (루트) |
| 프로젝트 구조 가이드 | `docs/PROJECT_FILE_GUIDE.md` (이 파일) |
| Unity 마이그레이션 계획 | `docs/unity-migration-plan.md` |
| Phase 3 태스크 | `docs/unity-migration-phase3-tasks.md` |
| Phase 4 태스크 | `docs/unity-migration-phase4-tasks.md` |
| Phase 5 태스크 | `docs/unity-migration-phase5-tasks.md` |
| Phase 6 태스크 | `docs/unity-migration-phase6-tasks.md` |

## 🎯 빠른 찾기 팁

### 1. Unity 스크립트 찾기
- **UI 관련**: `Assets/Scripts/UI/Controllers/`
- **시스템 관련**: `Assets/Scripts/Systems/`
- **핵심 클래스**: `Assets/Scripts/Core/`

### 2. Web 코드 찾기
- **동일한 기능**은 Unity와 Web에 둘 다 존재
- 예: `UIManager.cs` ↔ `src/ui/UIManager.js`
- Web 코드는 `src/` 폴더 아래에 있음

### 3. 데이터 구조 찾기
- **GameState**: `Assets/Scripts/Core/GameState.cs` (Unity)
- **GameState**: `src/core/GameState.js` (Web)
- 두 파일의 데이터 구조는 **동일하게** 유지해야 함

### 4. UI 파일 찾기
- **UXML** (마크업): `Assets/UI/UXML/`
- **USS** (스타일): `Assets/UI/StyleSheets/`

## 📝 규칙

1. **Unity 작업 시**: `Assets/` 폴더만 사용
2. **Web 작업 시**: `src/` 폴더 사용
3. **문서화**: `docs/` 폴더에 Markdown 파일로 저장
4. **AGENTS.md**: AI Agent가 항상 참조하는 규칙 파일 (루트에 위치)

---

*마지막 업데이트: 2026-04-10*
*이 문서는 프로젝트 구조 변경 시 함께 업데이트하세요.*
