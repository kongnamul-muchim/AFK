# 🤖 Idle RPG 개발 에이전트 워크플로우

## 📋 개요

이 문서는 Idle RPG 게임 개발 시 **에이전트가 따를べき 구현 → 커밋 → 문서화 워크플로우**를 정의합니다.

---

## 🎯 핵심 원칙

1. **작은 단위로 구현** - 한 번에 하나의 시스템/기능
2. **즉시 커밋** - 구현 완료 후 즉시 git 커밋
3. **상황별 문서화** - 구현 내용에 맞는 .md 파일 생성/수정
4. **Unity 독립성** - 게임 로직은 100% 순수 JavaScript

---

## 🔄 워크플로우

```
┌─────────────────────────────────────────────────────────────────┐
│  1. 작업 선택 (tasks.md 에서)                                   │
│     - 우선순위 높은 항목부터                                    │
│     - 의존성 있는 작업은 선행 작업 완료 후                       │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│  2. 구현                                                       │
│     - 관련 파일 생성/수정                                       │
│     - 테스트 while 개발                                         │
│     - tasks.md 체크박스 업데이트 (- [ ] → - [x])                │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│  3. Git 커밋                                                   │
│     - git add <files>                                           │
│     - git commit -m "타입: 설명"                                │
│     - 컨벤션 준수 (아래 참조)                                   │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│  4. 상황별 문서화                                               │
│     - 구현 내용에 맞는 .md 파일 생성/수정                        │
│     - CHANGELOG.md 업데이트                                     │
│     - README.md 업데이트 (필요시)                               │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│  5. 다음 작업                                                   │
│     - 1 번으로 돌아가 반복                                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 Git 커밋 컨벤션

### 커밋 메시지 형식

```
타입: 짧은 설명

자세한 설명 (선택)
```

### 타입 목록

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새로운 기능 | `feat: 전투 시스템 구현` |
| `fix` | 버그 수정 | `fix: HP 바 렌더링 오류` |
| `refactor` | 코드 리팩토링 | `refactor: GameState 클래스 구조 개선` |
| `docs` | 문서 수정 | `docs: Agent.md 워크플로우 추가` |
| `style` | 코드 포맷팅 | `style: ESLint 규칙 적용` |
| `test` | 테스트 추가 | `test: 데미지 계산 단위 테스트` |
| `chore` | 기타 (빌드, 설정) | `chore: .gitignore 업데이트` |
| `data` | CSV 데이터 추가/수정 | `data: 몬스터 10 종 추가` |

### 커밋 메시지 예시

```bash
# 좋은 예
git commit -m "feat: 플레이어 스탯 시스템 구현

- 힘, 민첩, 지력, 체력 스탯 추가
- 파생 능력치 자동 계산 (공격력, 방어력, 크리티컬)
- 스탯포인트 분배 UI 연동

#player #stats #system"

# 나쁜 예
git commit -m "수정함"
git commit -m "작업 완료"
```

---

## 📄 상황별 문서화 가이드

### 1. **새로운 시스템 구현 시**

**파일:** `docs/systems/<시스템명>.md`

```markdown
# <시스템명>

## 개요
<시스템의 목적과 역할>

## 아키텍처
<클래스 다이어그램, 데이터 흐름>

## 주요 클래스
### ClassName
- **역할**: <설명>
- **메서드**:
  - `method1()`: <설명>
  - `method2(param)`: <설명>

## 사용 예시
<코드 스니펫>

## 의존성
<다른 시스템과의 관계>
```

**예시 파일:**
- `docs/systems/combat.md` - 전투 시스템
- `docs/systems/inventory.md` - 인벤토리 시스템
- `docs/systems/progression.md` - 성장 시스템

---

### 2. **CSV 데이터 추가/수정 시**

**파일:** `docs/data/<데이터명>.md`

```markdown
# <데이터명> CSV

## 파일 위치
`data/<파일명>.csv`

## 스키마
| 필드명 | 타입 | 설명 |
|--------|------|------|
| id | number | 고유 ID |
| name | string | 이름 |

## 데이터 목록
<주요 데이터 나열>

## 밸런스 노트
<조정된 값과 이유>
```

**예시 파일:**
- `docs/data/items.md` - 아이템 데이터
- `docs/data/monsters.md` - 몬스터 데이터

---

### 3. **UI 컴포넌트 추가 시**

**파일:** `docs/ui/<컴포넌트명>.md`

```markdown
# <컴포넌트명>

## 스크린샷
<이미지 또는 ASCII 아트>

## 구조
<DOM 구조, CSS 클래스>

## 이벤트
<사용자 상호작용>

## 상태 연동
<GameState 와의 데이터 바인딩>
```

---

### 4. **버그 수정 시**

**파일:** `CHANGELOG.md` (항목 추가)

```markdown
## [버전] - YYYY-MM-DD

### Fixed
- #이슈번호: <버그 설명> - <해결 방법>

### Changed
- <변경 사항>
```

---

### 5. **성능 최적화 시**

**파일:** `docs/performance/<주제>.md`

```markdown
# <최적화 주제>

## 문제
<성능 병목 지점>

## 해결 방안
<적용한 최적화 기법>

## 결과
- Before: <수치>
- After: <수치>
```

---

## 📊 문서 구조

```
docs/
├── systems/           # 시스템 문서
│   ├── combat.md
│   ├── inventory.md
│   ├── progression.md
│   └── ...
├── data/              # CSV 데이터 문서
│   ├── items.md
│   ├── monsters.md
│   └── ...
├── ui/                # UI 컴포넌트
│   ├── hud.md
│   ├── inventory-modal.md
│   └── ...
├── performance/       # 성능 최적화
│   └── rendering.md
├── architecture/      # 아키텍처
│   ├── overview.md
│   └── unity-independence.md
└── guides/            # 개발 가이드
    ├── getting-started.md
    └── csv-data-guide.md
```

---

## 🔧 자동화 스크립트 (추후)

### pre-commit 훅

```bash
#!/bin/bash
# .git/hooks/pre-commit

# 1. tasks.md 체크박스 확인
# 2. CSV 파일 유효성 검사
# 3. ESLint 실행

echo "Running pre-commit checks..."

# CSV 검증
node scripts/validate-csv.js
if [ $? -ne 0 ]; then
    echo "CSV validation failed!"
    exit 1
fi

# ESLint
npm run lint
if [ $? -ne 0 ]; then
    echo "Linting failed!"
    exit 1
fi

echo "All checks passed!"
```

### 커밋 후 자동 문서화

```bash
#!/bin/bash
# scripts/post-commit-docs.sh

# 변경된 파일 감지
CHANGED_FILES=$(git diff-tree --no-commit-id --name-only -r HEAD)

# 파일 경로에 따라 문서 업데이트
if [[ $CHANGED_FILES == *"src/systems/"* ]]; then
    echo "System implementation detected. Please create docs/systems/<name>.md"
fi

if [[ $CHANGED_FILES == *"data/"* ]]; then
    echo "Data change detected. Please update docs/data/<name>.md"
fi
```

---

## ✅ 체크리스트

### 구현 전
- [ ] tasks.md 에서 작업 선택
- [ ] 선행 작업 완료 확인
- [ ] 명세서 확인 (specs/)

### 구현 중
- [ ] 코드 주석 작성 (필수적인 부분만)
- [ ] 테스트 while 개발
- [ ] 로그 추가 (gameLogger)

### 구현 후
- [ ] tasks.md 체크박스 업데이트
- [ ] 로컬 테스트
- [ ] Git 커밋 (컨벤션 준수)
- [ ] 문서화 (상황별 .md 파일)
- [ ] CHANGELOG.md 업데이트

---

## 📈 진행 상황 추적

### OpenSpec 태스크

- **위치:** `openspec/changes/idle-rpg-game/tasks.md`
- **형식:** `- [ ]` → `- [x]`
- **커밋 메시지:** `chore: tasks.md progress update`

### Git 히스토리

```bash
# 이번 스프린트 커밋 보기
git log --since="2 weeks ago" --oneline

# 파일별 커밋 히스토리
git log --follow src/core/GameState.js

# 변경 통계
git log --shortstat
```

---

## 🎯 다음 단계

1. **구현 계속** - 남은 태스크 진행
2. **테스트 작성** - 단위/통합 테스트
3. **문서 완성** - 누락된 .md 파일 작성
4. **빌드 준비** - 배포용 번들링

---

## 📞 연락처

- **에이전트:** Character Orchestrator
- **명세:** `openspec/changes/idle-rpg-game/`
- **문서:** `docs/`

---

*마지막 업데이트: 2025-04-07*
