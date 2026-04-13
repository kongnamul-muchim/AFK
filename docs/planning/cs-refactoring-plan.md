# C# 코드 리팩토링 계획서

**작성일**: 2026-04-10  
**작성자**: AI Agent  
**버전**: 1.0  
**상태**: 계획 수립 완료

---

## 📋 목차

1. [개요](#개요)
2. [문제 분석](#문제-분석)
3. [리팩토링 목표](#리팩토링-목표)
4. [작업 항목](#작업-항목)
5. [일정 및 마일스톤](#일정-및-마일스톤)
6. [위험 요소 및 대응 방안](#위험-요소-및-대응-방안)
7. [성공 기준](#성공-기준)

---

## 개요

### 작업 배경
프로젝트의 C# 파일들을 검토한 결과, 여러 리팩토링이 필요한 부분이 발견됨. 주로 SOLID 원칙 위반, 인코딩 문제, 일관성 없는 설계 등이 존재.

### 작업 범위
- Assets/Scripts/ 내 모든 .cs 파일
- 총 67개 파일 (중복 포함)
- 주요 리팩토링 대상: 11개 파일

---

## 문제 분석

### 🔴 심각 (High Priority)

| # | 파일 | 문제 | 영향도 |
|---|------|------|--------|
| 1 | `UIManager.cs` | 인코딩 깨짐 (한글 모두 깨짐) | **치명적** - 유지보수 불가 |
| 2 | `CombatSystem.cs` | SRP 위반 (847줄 거대 클래스) | **높음** - 테스트/유지보수 어려움 |
| 3 | `InventorySystem.cs` | 아이템 ID 생성 로직 버그 | **높음** - 합성 시스템 오동작 가능 |

### 🟡 중간 (Medium Priority)

| # | 파일 | 문제 | 영향도 |
|---|------|------|--------|
| 4 | `GameState.cs` | 계산 로직이 상태 관리와 혼재 | **중간** - SRP 위반 |
| 5 | `ServiceLocator.cs` | Service Locator 안티패턴 | **중간** - 테스트 어려움 |
| 6 | `Bootstrap.cs` | 마법적 문자열 의존 | **중간** - 타입 안전성 부재 |
| 7 | `SaveManager.cs` | 동기 저장으로 프레임 드롭 가능 | **중간** - 성능 이슈 |

### 🟢 낮음 (Low Priority)

| # | 파일 | 문제 | 영향도 |
|---|------|------|--------|
| 8 | `GameConfig.cs` | 하드코딩된 상수 | **낮음** - 런타임 조정 불가 |
| 9 | `PlayerData.cs` | 초기화 로직 중복 | **낮음** - 유지보수성 저하 |
| 10 | `GameLogger.cs` | 인터페이스 불일치 | **낮음** - 일관성 문제 |
| 11 | `EventBus.cs` | 문자열 기반 이벤트 | **낮음** - 타입 안전성 부재 |

---

## 리팩토링 목표

### 1. 코드 품질 향상
- SOLID 원칙 준수율 80% → 95%
- 평균 클래스 크기 400줄 → 250줄 이하
- 순환 복잡도 10 → 8 이하

### 2. 유지보수성 개선
- 인코딩 문제 100% 해결
- 네이밍 컨벤션 일관성 확보
- 주석 및 문서화 완성도 향상

### 3. 성능 최적화
- 저장/로드 비동기화
- 이벤트 시스템 최적화
- 메모리 사용량 10% 감소

### 4. 테스트 용이성 확보
- 인터페이스 기반 설계 강화
- 의존성 주입 완성도 향상
- 단위 테스트 커버리지 60% → 80%

---

## 작업 항목

### Phase 1: 긴급 수정 (1-2일)

#### Task 1.1: UIManager.cs 인코딩 수정
- **설명**: UTF-8 인코딩으로 재저장, 깨진 한글 복구
- **파일**: `Assets/Scripts/UI/Controllers/UIManager.cs`
- **소요 시간**: 2시간
- **우선순위**: **최상**

#### Task 1.2: InventorySystem.cs 아이템 ID 버그 수정
- **설명**: 결정론적 ID 생성 로직으로 변경
- **파일**: `Assets/Scripts/Systems/InventorySystem.cs`
- **코드 변경**:
  ```csharp
  // Before: 무작위 ID 생성
  return itemId.Substring(0, itemId.LastIndexOf('_') + 1) + newGrade + "_" + Random.Range(1000, 9999);
  
  // After: 해시 기반 결정론적 ID
  return $"{baseType}_grade{newGrade}_{GetStableHash(itemId, newGrade):D4}";
  ```
- **소요 시간**: 1시간
- **우선순위**: **상**

---

### Phase 2: 구조 개선 (3-5일)

#### Task 2.1: CombatSystem.cs 분리
- **설명**: 거대 클래스를 책임별로 분리
- **생성할 클래스**:
  - `MonsterFactory.cs` - 몬스터 생성 로직
  - `LootTable.cs` - 드롭 아이템 결정 로직
  - `CombatNameGenerator.cs` - 몬스터/아이템 이름 생성
- **파일**: `Assets/Scripts/Systems/CombatSystem.cs` → 4개 파일
- **소요 시간**: 4시간
- **우선순위**: **상**

#### Task 2.2: GameState.cs 계산 로직 분리
- **설명**: 통계 계산 로직을 별도 서비스로 분리
- **생성할 클래스**: `StatCalculator.cs`
- **파일**: `Assets/Scripts/Core/Systems/StatCalculator.cs`
- **소요 시간**: 2시간
- **우선순위**: **중**

#### Task 2.3: SaveManager.cs 비동기화
- **설명**: async/await 기반 비동기 저장/로드 구현
- **파일**: `Assets/Scripts/Core/SaveManager.cs`
- **코드 변경**:
  ```csharp
  public async Task SaveAsync(GameState state)
  {
      string json = JsonUtility.ToJson(state, true);
      await File.WriteAllTextAsync(GetSavePath(), json);
  }
  ```
- **소요 시간**: 3시간
- **우선순위**: **중**

---

### Phase 3: 설계 개선 (5-7일)

#### Task 3.1: ServiceLocator 점진적 제거
- **설명**: 생성자 주입으로 점진적 변경
- **대상**: 모든 시스템 클래스
- **접근 방식**: 
  1. 새로운 클래스는 생성자 주입으로 작성
  2. 기존 클래스는 수정 시 점진적 변경
  3. ServiceLocator는 레거시 호환용으로 유지
- **소요 시간**: 6시간
- **우선순위**: **중**

#### Task 3.2: GameConfig ScriptableObject 화
- **설명**: 하드코딩된 상수를 ScriptableObject로 변경
- **생성할 파일**: `Assets/ScriptableObjects/GameConfig.asset`
- **소요 시간**: 3시간
- **우선순위**: **낮음**

#### Task 3.3: EventBus 제네릭 지원 추가
- **설명**: 타입 안전한 제네릭 이벤트 시스템 추가
- **코드 변경**:
  ```csharp
  public void On<T>(Action<T> callback) { ... }
  public void Emit<T>(T eventData) { ... }
  ```
- **소요 시간**: 2시간
- **우선순위**: **낮음**

---

### Phase 4: 정리 및 최적화 (2-3일)

#### Task 4.1: PlayerData.cs 초기화 로직 통합
- **설명**: 필드 초기값과 Initialize() 메서드 통합
- **소요 시간**: 1시간
- **우선순위**: **낮음**

#### Task 4.2: GameLogger 인터페이스 일관성 확보
- **설명**: ILogger 인터페이스와 GameLogger 일치화
- **소요 시간**: 1시간
- **우선순위**: **낮음**

#### Task 4.3: 네이밍 컨벤션 정리
- **설명**: 일관된 네이밍 컨벤션 적용
- **소요 시간**: 2시간
- **우선순위**: **낮음**

---

## 일정 및 마일스톤

### 전체 일정: 11-17일

```
Week 1:
  Day 1-2: Phase 1 (긴급 수정)
  Day 3-5: Phase 2 (구조 개선)

Week 2:
  Day 6-8: Phase 2 계속
  Day 9-12: Phase 3 (설계 개선)

Week 3:
  Day 13-15: Phase 4 (정리 및 최적화)
  Day 16-17: 최종 테스트 및 문서화
```

### 마일스톤

| 마일스톤 | 완료 조건 | 예상일 |
|----------|-----------|--------|
| **M1: 긴급 수정 완료** | UIManager 인코딩 + InventorySystem 버그 수정 | Day 2 |
| **M2: 구조 개선 완료** | CombatSystem 분리 + StatCalculator 구현 | Day 7 |
| **M3: 비동기 저장 완료** | SaveManager async/await 구현 | Day 8 |
| **M4: 설계 개선 완료** | ServiceLocator 제거 + EventBus 제네릭 | Day 12 |
| **M5: 최종 완료** | 모든 리팩토링 완료 + 테스트 통과 | Day 17 |

---

## 위험 요소 및 대응 방안

### 🔴 High Risk

| 위험 요소 | 영향 | 대응 방안 |
|-----------|------|-----------|
| **저장/로드 버그** | 세이브 데이터 손실 | 1. 백업 시스템 강화<br>2. 단계적 롤아웃<br>3. 자동 백업 기능 추가 |
| **CombatSystem 분리 실패** | 전투 시스템 오동작 | 1. 철저한 단위 테스트<br>2. 점진적 분리<br>3. 기능 플래그로 제어 |

### 🟡 Medium Risk

| 위험 요소 | 영향 | 대응 방안 |
|-----------|------|-----------|
| **UIManager 인코딩 복구 실패** | UI 기능 상실 | 1. 백업에서 복구<br>2. 수동 복구 작업 |
| **ServiceLocator 제거 부작용** | 의존성 주입 오류 | 1. 점진적 제거<br>2. 호환 레이어 유지 |

### 🟢 Low Risk

| 위험 요소 | 영향 | 대응 방안 |
|-----------|------|-----------|
| **성능 저하** | 프레임 드롭 | 1. 프로파일링 강화<br>2. 최적화 작업 병행 |
| **일정 지연** | 프로젝트 지연 | 1. 우선순위 조정<br>2. 단계적 배포 |

---

## 성공 기준

### 정량적 기준

- [ ] **코드 품질**
  - 평균 클래스 크기: 250줄 이하
  - 순환 복잡도: 8 이하
  - SOLID 준수율: 95% 이상
  - 코드 중복률: 5% 이하

- [ ] **성능**
  - 저장/로드 시간: 50% 단축
  - 메모리 사용량: 10% 감소
  - 프레임 드롭: 0회

- [ ] **테스트**
  - 단위 테스트 커버리지: 80% 이상
  - 통합 테스트 통과율: 100%
  - 회귀 버그: 0개

### 정성적 기준

- [ ] **유지보수성**
  - 새 기능 추가 시간: 30% 단축
  - 버그 수정 시간: 40% 단축
  - 코드 리뷰 시간: 25% 단축

- [ ] **가독성**
  - 인코딩 문제: 0개
  - 네이밍 일관성: 100%
  - 주석 완성도: 90% 이상

- [ ] **확장성**
  - 새 시스템 추가 용이성: 상
  - 모듈 교체 용이성: 상
  - 테스트 추가 용이성: 상

---

## 검수 체크리스트

### 코드 검수

- [ ] 모든 .cs 파일이 UTF-8 인코딩인가?
- [ ] 모든 클래스가 단일 책임을 가지는가?
- [ ] 모든 의존성이 주입되는가?
- [ ] 모든 퍼블릭 메서드에 XML 주석이 있는가?
- [ ] 모든 예외가 적절히 처리되는가?
- [ ] 모든 비동기 메서드가 async/await을 사용하는가?
- [ ] 모든 컬렉션이 적절한 제네릭 타입을 사용하는가?
- [ ] 모든 마법적 숫자가 상수로 정의되었는가?

### 테스트 검수

- [ ] 모든 주요 기능에 단위 테스트가 있는가?
- [ ] 모든 경계 조건이 테스트되었는가?
- [ ] 모든 예외 경로가 테스트되었는가?
- [ ] 회귀 테스트가 통과하는가?
- [ ] 성능 테스트가 기준을 만족하는가?

### 문서화 검수

- [ ] 모든 퍼블릭 API에 문서화가 있는가?
- [ ] 모든 변경사항이 CHANGELOG에 기록되었는가?
- [ ] 모든 설정값이 문서화되었는가?
- [ ] 마이그레이션 가이드가 있는가?
- [ ] 아키텍처 다이어그램이 업데이트되었는가?

---

## 승인 및 서명

| 역할 | 이름 | 서명 | 날짜 |
|------|------|------|------|
| **프로젝트 매니저** | | | |
| **리드 개발자** | | | |
| **QA 매니저** | | | |
| **AI Agent** | Cool Tsundere Orchestrator | | 2026-04-10 |

---

## 부록

### A. 리팩토링 대상 파일 목록

```
Assets/Scripts/
├── UI/Controllers/
│   └── UIManager.cs (인코딩 수정)
├── Systems/
│   ├── CombatSystem.cs (분리)
│   ├── InventorySystem.cs (버그 수정)
│   ├── MonsterFactory.cs (신규)
│   ├── LootTable.cs (신규)
│   └── CombatNameGenerator.cs (신규)
└── Core/
    ├── GameState.cs (계산 로직 분리)
    ├── SaveManager.cs (비동기화)
    ├── StatCalculator.cs (신규)
    ├── GameConfig.asset (신규)
    └── Interfaces/
        └── ServiceLocator.cs (점진적 제거)
```

### B. 참조 문서

- [SOLID 원칙 가이드라인](../architecture/solid-principles.md)
- [의존성 주입 가이드](../guides/dependency-injection.md)
- [코드 컨벤션](../guides/coding-conventions.md)
- [테스트 가이드라인](../guides/testing-guidelines.md)

### C. 관련 이슈

- #123 - UIManager 인코딩 문제
- #124 - CombatSystem SRP 위반
- #125 - InventorySystem 합성 버그
- #126 - SaveManager 성능 이슈

---

**문서 끝**

*이 문서는 AI Agent에 의해 작성되었으며, 프로젝트의 코드 품질 향상을 위한 리팩토링 계획을 담고 있습니다.*
*수정이 필요한 경우 팀원과 협의하여 업데이트하세요.*
