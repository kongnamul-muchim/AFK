# 프로젝트 정리 보고서

**작성일:** 2026-04-13  
**작업 유형:** 프로젝트 구조 정리 및 불필요한 파일 제거

---

## 📁 정리 전 문제점

1. **중복 Unity 프로젝트**: `AFK-Unity/` 폴더에 오래된 Unity 프로젝트 사본이 존재
2. **임시 파일 누적**: `Library/`, `Temp/`, `Logs/`, `UserSettings/` 폴더에 불필요한 캐시 파일 존재
3. **자동 생성 파일**: `.csproj`, `.slnx` 파일이 버전 관리에 포함됨
4. **혼란스러운 구조**: Web 버전과 Unity 버전 파일이 섞여있음

---

## 🔧 수행한 작업

### 1. 중복 Unity 프로젝트 제거

**삭제:** `AFK-Unity/` (전체)

- 오래된 Unity 프로젝트 사본
- 루트의 `Assets/`가 실제 활성 프로젝트
- 별도의 `.git/`을 가진 중첩 저장소

### 2. 임시/빌드 파일 정리 시도

**대상:**
- `Library/` (~1.95 GB) - Unity 캐시
- `Logs/` (~26 MB) - Unity 로그
- `Temp/` (~0.28 MB) - 임시 빌드 파일
- `UserSettings/` (~4 KB) - 사용자 설정

**결과:** Unity 에디터가 실행 중이라 파일이 lock되어 삭제 불가.  
**대안:** `.gitignore`에 이미 포함되어 있으므로 문제없음. Unity 종료 후 자동 정리됨.

### 3. 자동 생성 파일 제거

**삭제:**
- `*.csproj` (Visual Studio 프로젝트 파일)
- `*.slnx` (Visual Studio 솔루션 파일)
- `nul` (잘못 생성된 파일)

### 4. .gitignore 업데이트

```gitignore
# ========== Unity ==========
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
*.csproj
*.sln
*.slnx
*.unityproj

# Unity 자동 생성 폴더 (로컬 환경만 해당)
/[Pp]roject[Ss]ettings/ProjectVersion.txt
/[Pp]ackages/packages-lock.json

# ========== Web / Node ==========
node_modules/
.opencode/node_modules/
dist/

# ========== Save Data (런타임 생성) ==========
savegame.json
**/savegame.json

# ========== OpenSpec ==========
openspec/changes/

# ========== Stray Files ==========
nul
```

### 5. 폴더 구조 재조직

| 변경 전 | 변경 후 | 설명 |
|---------|---------|------|
| `css/` | `src/css/` | Web CSS 파일 통합 |
| `*.js` (루트) | `tests/` | 테스트 파일 통합 |
| `docs/superpowers/` | `docs/ai-plans/` | AI 생성 문서 통합 |

---

## 📂 최종 폴더 구조

```
C:\Users\admin\AFK\
├── .git/                    # Git 저장소
├── .gitignore               # Git 무시 규칙
├── .opencode/               # AI 어시스턴트 플러그인
├── .vscode/                 # VS Code 설정
├── Assets/                  # Unity 프로젝트 (메인)
│   ├── Scripts/             # C# 스크립트
│   ├── Scenes/              # Unity 씬
│   ├── Resources/           # 리소스 파일
│   ├── images/              # 스프라이트 이미지
│   ├── sounds/              # 사운드 파일
│   └── ...
├── ProjectSettings/         # Unity 프로젝트 설정
├── Packages/                # Unity 패키지 매니페스트
├── src/                     # Web 버전 소스 코드
│   ├── core/                # 핵심 시스템 (JS)
│   ├── systems/             # 게임 시스템 (JS)
│   ├── ui/                  # UI 컴포넌트 (JS)
│   ├── css/                 # 스타일시트
│   └── main.js              # 진입점
├── index.html               # Web 버전 진입점
├── package.json             # npm 설정
├── docs/                    # 문서화 폴더
│   ├── ai-plans/            # AI 생성 계획/명세
│   ├── architecture/        # 아키텍처 문서
│   ├── progress/            # 진행 상황
│   └── ...
├── data/                    # 게임 데이터 CSV
├── tests/                   # 테스트 파일
│   ├── unit/                # 단위 테스트
│   ├── integration/         # 통합 테스트
│   └── debug/               # 디버그 테스트
├── tools/                   # 개발 도구
├── openspec/                # 명세 파일
├── AGENTS.md                # AI Agent 워크플로우 규칙
└── CHANGELOG.md             # 변경 이력
```

---

## 🎯 정리 결과

| 항목 | 정리 전 | 정리 후 |
|------|---------|---------|
| 루트 폴더 수 | 19개 | 15개 |
| 중복 프로젝트 | 2개 (Assets + AFK-Unity) | 1개 (Assets) |
| Git 관리 파일 | 혼재 | 명확히 분리 |
| .gitignore | 기본 | Unity/Web 최적화 |

---

## ⚠️ 주의사항

1. **Library/Logs/Temp 삭제 불가**: Unity 에디터가 실행 중일 때는 파일이 lock됩니다.  
   → Unity를 종료하면 자동으로 정리되거나, 수동으로 삭제 가능합니다.

2. **저장 데이터**: `savegame.json`은 런타임에 `Application.persistentDataPath`에 생성됩니다.  
   → 버그 있는 세이브데이터는 게임 내 "재시작" 기능으로 초기화하세요.

3. **Web 버전**: `src/` 폴더의 JavaScript 코드는 Web 빌드용입니다.  
   → Unity와는 독립적으로 실행됩니다.

---

## 📝 다음 단계

1. **Unity 에디터 종료 후 Library 폴더 삭제** (선택사항, ~1.95 GB 확보)
2. **게임 테스트**: 버그 수정 + 정리 후 정상 작동 확인
3. **Web 버전 테스트**: `index.html`로 Web 빌드 확인

---

*정리 작업 완료. 커밋 해시: `013f229`*
