# 📚 AFK 프로젝트 파일 길잡이

> 이 프로젝트는 **Unity C#** 버전과 **Web JavaScript** 버전, 두 가지로 나뉘어 있습니다.
> 파일을 찾을 때는 이 문서를 참조하세요.

---

## 📂 프로젝트 구조 한눈에 보기

```
C:\Users\admin\AFK\
│
├── 📁 Assets/                    # 🎮 Unity C# 버전
│   ├── 📁 Scripts/              # C# 스크립트
│   ├── 📁 UI/                  # UXML/USS UI 파일
│   ├── 📁 images/              # 스프라이트, 배경화면
│   ├── 📁 Editor/              # 유니티 에디터 확장
│   └── 📁 Settings/             # 유니티 프로젝트 설정
│
├── 📁 src/                      # 🌐 Web JavaScript 버전
│   ├── 📁 core/                # 핵심 시스템
│   ├── 📁 systems/              # 게임 시스템
│   ├── 📁 ui/                  # UI 컴포넌트
│   ├── 📁 adapters/            # Unity 연동 브릿지
│   ├── 📁 data-parser/         # 데이터 파서
│   ├── 📁 config/              # 설정
│   ├── 📁 audio/               # 오디오
│   └── 📁 test/                # 테스트
│
├── 📁 docs/                     # 📄 문서
│   ├── 📁 planning/            # 계획 문서
│   ├── 📁 progress/            # 진행 상황
│   ├── 📁 reports/              # 완료 보고서
│   ├── 📁 systems/             # 시스템 문서
│   └── 📁 guides/              # 가이드
│
├── 📁 tools/                    # 🛠️ 도구 및 스킬
├── 📁 .opencode/               # opencode 설정
│
├── 📄 AGENTS.md                 # ⚠️ AI Agent 규칙 (매우 중요!)
├── 📄 package.json              # npm 설정
└── 📄 index.html                # Web 버전 진입점
```

---

# 🎮 Unity C# 版本

## Scripts 구조

```
Assets/Scripts/
├── Core/                        # 핵심 시스템
│   └── DI/                      # DI 컨테이너 (순수 C#)
├── Systems/                     # 게임 시스템
├── UI/
│   ├── Controllers/             # UI 컨트롤러
│   ├── Views/                   # UI 뷰
│   ├── Effects/                 # 이펙트
│   └── Data/                    # UI 데이터
├── Rendering/                   # 렌더링
├── Audio/                       # 오디오
└── Tests/                       # 테스트
```

### 핵심 (`Core/`)
| 파일 | 설명 |
|------|------|
| `GameState.cs` | 게임 상태 관리 (플레이어, 스테이지, 전투, 인벤 등) |
| `GameConfig.cs` | 게임 밸런스 상수 (스탯, 드롭률 등) |
| `SaveManager.cs` | 저장/불러오기 (JSON 기반) |
| `EventBus.cs` | 이벤트 발생/수신 시스템 |
| `Bootstrap.cs` | 게임 초기화 진입점 |
| `GameLogger.cs` | 중앙화 로깅 |
| `DataLoader.cs` | CSV/데이터 로드 |
| `CSVParser.cs` | CSV 파싱 유틸 |
| `StatCalculator.cs` | 스탯 계산 유틸 |

### 데이터 모델 (`Core/DataModels/`)
| 파일 | 설명 |
|------|------|
| `PlayerData.cs` | 플레이어 정보 |
| `StageData.cs` | 스테이지 정보 |
| `InventoryData.cs` | 인벤토리 정보 |
| `MissionData.cs` | 미션 정보 |
| `RebirthData.cs` | 환생 시스템 데이터 |
| `SerializableDictionary.cs` | 직렬화 딕셔너리 |

### 인터페이스 (`Core/Interfaces/`)
| 파일 | 설명 |
|------|------|
| `IGameState.cs` | GameState 인터페이스 |
| `IEventBus.cs` | EventBus 인터페이스 |
| `ISaveManager.cs` | SaveManager 인터페이스 |
| `ILogger.cs` | 로거 인터페이스 |
| `ServiceLocator.cs` | 서비스 로케이터 |

### 시스템 (`Systems/`)
| 파일 | 설명 |
|------|------|
| `CombatSystem.cs` | 전투 로직 |
| `StageSystem.cs` | 스테이지 진행 |
| `InventorySystem.cs` | 인벤토리 관리 |
| `DailyMissionSystem.cs` | 일일/주간 미션 |
| `MissionSystem.cs` | 일반 미션 |
| `RebirthSystem.cs` | 환생 시스템 |
| `OfflineRewardSystem.cs` | 오프라인 보상 |
| `TutorialSystem.cs` | 튜토리얼 |
| `DropTable.cs` | 드롭 테이블 |
| `MonsterFactory.cs` | 몬스터 생성 |
| `ItemFactory.cs` | 아이템 생성 |
| `StatsTracker.cs` | 통계 추적 |

### UI 컨트롤러 (`UI/Controllers/`)
| 파일 | 설명 |
|------|------|
| `UIManager.cs` | UI 전체 관리 |
| `PopupManager.cs` | 팝업 관리 |
| `ModalManager.cs` | 모달 창 관리 |
| `TooltipManager.cs` | 툴팁 관리 |
| `CombatLogManager.cs` | 전투 로그 |
| `UXMLLoader.cs` | UXML 로드 |

### UI 뷰 (`UI/Views/`)
| 파일 | 설명 |
|------|------|
| `InventoryUI.cs` | 인벤토리 창 |
| `UpgradeUI.cs` | 강화 창 |
| `GemShopUI.cs` | 보석 상점 |
| `MissionsUI.cs` | 미션 창 |

### 렌더링 (`Rendering/`)
| 파일 | 설명 |
|------|------|
| `GameRenderer.cs` | 메인 게임 렌더링 |
| `UIGameRenderer.cs` | UI 렌더링 |
| `GameRendererSceneSetup.cs` | 씬 설정 |
| `ParticleManager.cs` | 파티클 관리 |

### 테스트 (`Tests/`)
| 파일 | 설명 |
|------|------|
| `Phase2IntegrationTest.cs` | Phase 2 통합 테스트 |

---

## UI 파일 (`Assets/UI/`)

### UXML (마크업)
| 파일 | 설명 |
|------|------|
| `UXML/MainGameUI.uxml` | 메인 게임 UI |

### USS (스타일)
| 파일 | 설명 |
|------|------|
| `StyleSheets/GameUIStyle.uss` | 게임 UI 스타일 |

---

# 🌐 Web JavaScript 版本

## 소스 구조

```
src/
├── core/                         # 핵심 시스템
├── systems/                      # 게임 시스템
├── ui/                           # UI 컴포넌트
├── adapters/                     # Unity 연동 브릿지
├── config/                       # 설정
├── data-parser/                  # 데이터 파서
├── audio/                        # 오디오
├── css/                          # 스타일시트
├── test/                         # 테스트
└── utils/                        # 유틸리티
```

### 핵심 (`core/`)
| 파일 | 설명 |
|------|------|
| `GameState.js` | 게임 상태 |
| `EventBus.js` | 이벤트 시스템 |
| `StorageManager.js` | 로컬 스토리지 (세이브/로드) |
| `Logger.js` | 로깅 |
| `ImageLoader.js` | 이미지 로드 |

### 시스템 (`systems/`)
| 파일 | 설명 |
|------|------|
| `CombatSystem.js` | 전투 |
| `StageSystem.js` | 스테이지 |
| `InventorySystem.js` | 인벤토리 |
| `DailyMissionSystem.js` | 일일 미션 |
| `RebirthSystem.js` | 환생 |
| `OfflineRewards.js` | 오프라인 보상 |
| `TutorialSystem.js` | 튜토리얼 |
| `StatsTracker.js` | 통계 |

### UI (`ui/`)
| 파일 | 설명 |
|------|------|
| `UIManager.js` | UI 관리 |
| `InventoryUI.js` | 인벤토리 |
| `UpgradeUI.js` | 강화 |
| `GemShopUI.js` | 보석 상점 |
| `DailyMissionUI.js` | 미션 |
| `LoadingScreen.js` | 로딩 화면 |

### 어댑터 (`adapters/`)
| 파일 | 설명 |
|------|------|
| `UnityBridge.js` | Unity 연동 브릿지 |
| `WebRenderer.js` | Web 렌더러 |

### 설정 (`config/`)
| 파일 | 설명 |
|------|------|
| `GameConfig.js` | 게임 설정/밸런스 |

### 데이터 파서 (`data-parser/`)
| 파일 | 설명 |
|------|------|
| `DataLoader.js` | 데이터 로드 |
| `CSVParser.js` | CSV 파싱 |

### 오디오 (`audio/`)
| 파일 | 설명 |
|------|------|
| `AudioManager.js` | 오디오 관리 |

---

# 📋 Unity ↔ Web 대응 표

동일한 기능을 가진 파일들입니다. 데이터를 공유합니다.

| Unity (C#) | Web (JS) | 설명 |
|------------|----------|------|
| `GameState.cs` | `GameState.js` | 게임 상태 관리 |
| `EventBus.cs` | `EventBus.js` | 이벤트 시스템 |
| `SaveManager.cs` | `StorageManager.js` | 저장/로드 |
| `GameLogger.cs` | `Logger.js` | 로깅 |
| `GameConfig.cs` | `GameConfig.js` | 게임 설정 |
| `UIManager.cs` | `UIManager.js` | UI 관리 |
| `InventorySystem.cs` | `InventorySystem.js` | 인벤토리 |
| `CombatSystem.cs` | `CombatSystem.js` | 전투 |
| `StageSystem.cs` | `StageSystem.js` | 스테이지 |
| `DailyMissionSystem.cs` | `DailyMissionSystem.js` | 일일 미션 |
| `RebirthSystem.cs` | `RebirthSystem.js` | 환생 |
| `OfflineRewardSystem.cs` | `OfflineRewards.js` | 오프라인 보상 |
| `TutorialSystem.cs` | `TutorialSystem.js` | 튜토리얼 |
| `StatsTracker.cs` | `StatsTracker.js` | 통계 |
| `DataLoader.cs` | `DataLoader.js` | 데이터 로드 |
| `CSVParser.cs` | `CSVParser.js` | CSV 파싱 |
| `AudioManager.cs` | `AudioManager.js` | 오디오 |

---

# ⚙️ AGENTS.md 연동 참조

`AGENTS.md`에서 언급하는 내용들을 바로 찾을 수 있도록 연결합니다.

## 📝 문서화 규칙 관련 (`AGENTS.md` 3장)

`AGENTS.md`의 문서화 규칙에서 사용하는 폴더 구조입니다.

```
docs/
├── 📁 planning/              # 계획 문서
│   ├── [작업명]-plan.md
│   └── [작업명]-requirements.md
├── 📁 progress/              # 진행 상황
│   ├── [작업명]-progress.md
│   ├── [작업명]-milestones.md
│   └── progress.md
├── 📁 reports/               # 완료 보고서
│   ├── [작업명]-report.md
│   └── [작업명]-summary.md
├── 📁 systems/               # 시스템 문서
│   └── combat.md
├── 📁 guides/               # 가이드
└── templates/               # 템플릿
```

### 문서 파일 위치
| 파일 | 설명 |
|------|------|
| `docs/PROJECT_FILE_GUIDE.md` | 이 파일 |
| `docs/AGENTS.md` | AI Agent 규칙 |
| `docs/progress.md` | 전체 진행 상황 |
| `docs/systems/combat.md` | 전투 시스템 문서 |
| `docs/unity-migration-plan.md` | 마이그레이션 계획 |
| `docs/unity-migration-phase*.md` | Phase별 태스크 |

### 현재 문서 목록
| 파일 | 설명 |
|------|------|
| `docs/unity-migration-phase1-tasks.md` | Phase 1 태스크 |
| `docs/unity-migration-phase2-tasks.md` | Phase 2 태스크 |
| `docs/unity-migration-phase3-tasks.md` | Phase 3 태스크 |
| `docs/unity-migration-phase4-tasks.md` | Phase 4 태스크 |
| `docs/unity-migration-phase5-tasks.md` | Phase 5 태스크 |
| `docs/unity-migration-phase6-tasks.md` | Phase 6 태스크 |
| `docs/progress/project-cleanup-report.md` | 프로젝트 정리 보고서 |
| `docs/progress/SOLID-Refactoring-Progress.md` | SOLID 리팩토링 진행 |
| `docs/reports/inventory-review.md` | 인벤토리 리뷰 |

## 🏗️ 코딩 원칙 관련 (`AGENTS.md` 5장)

SOLID 원칙과 DI를 준수하는 코드 구조입니다.

### 인터페이스 (추상화)
| 파일 | 설명 |
|------|------|
| `Assets/Scripts/Core/Interfaces/IGameState.cs` | GameState 인터페이스 |
| `Assets/Scripts/Core/Interfaces/IEventBus.cs` | EventBus 인터페이스 |
| `Assets/Scripts/Core/Interfaces/ISaveManager.cs` | SaveManager 인터페이스 |
| `Assets/Scripts/Core/Interfaces/ILogger.cs` | 로거 인터페이스 |
| `Assets/Scripts/Core/DI/IDIContainer.cs` | DI 컨테이너 (ServiceLocator 대체) |
| `Assets/Scripts/Core/Interfaces/GameLoggerAdapter.cs` | 로거 어댑터 |

### DI 컨테이너 (`Assets/Scripts/Core/DI/`)
| 파일 | 설명 |
|------|------|
| `IDIContainer.cs` | DI 컨테이너 인터페이스 + ServiceLifetime |
| `DIContainer.cs` | DI 컨테이너 구현체 (순수 C#) |

### DI/솔리드 적용 예시
| 파일 | 설명 |
|------|------|
| `Assets/Scripts/Core/Systems/BaseSystem.cs` | 베이스 시스템 (DI 적용) |
| `Assets/Scripts/Core/EventBus.cs` | EventBus (싱글톤) |
| `Assets/Scripts/Core/GameState.cs` | GameState (싱글톤) |
| `Assets/Scripts/Core/SaveManager.cs` | SaveManager (싱글톤) |

## 🛠️ 도구 및 설정

| 파일 | 설명 |
|------|------|
| `tools/superpowers/` | AI Agent 스킬 및 도구 |
| `.opencode/` | opencode 설정 |
| `.vscode/` | VSCode 설정 |
| `Packages/manifest.json` | Unity 패키지 목록 |
| `package.json` | npm 설정 |

---

# 🔍 빠른 찾기 tips

### Unity 스크립트
```
Assets/Scripts/
├── Core/
│   ├── DI/            → DI 컨테이너 (IDIContainer, DIContainer)
│   └── ...            → GameState, EventBus 등
├── Systems/        → 게임 시스템 (Combat, Stage 등)
├── UI/
│   ├── Controllers/   → 컨트롤러
│   └── Views/         → 뷰
├── Rendering/        → 렌더링
├── Audio/            → 오디오
└── Tests/            → 테스트
```

### Web 코드
```
src/
├── core/            → 핵심 (GameState, EventBus)
├── systems/         → 게임 시스템
├── ui/              → UI 컴포넌트
├── adapters/        → Unity 연동
├── config/          → 설정
├── data-parser/     → 데이터 파싱
├── audio/           → 오디오
├── css/             → 스타일시트
└── test/            → 테스트
```

---

## 📌 규칙

1. **Unity 작업** → `Assets/` 폴더만 사용
2. **Web 작업** → `src/` 폴더 사용
3. **문서화** → `docs/` 폴더에 Markdown으로 저장
4. **AGENTS.md** → AI Agent 규칙 파일 (루트에 위치)

---

*마지막 업데이트: 2026-04-13*
