# Unity 이식 Phase 4: UI 개선 및 데이터 이관

**목표**: Web 버전과 동일한 UI/UX로 개선 + CSV 데이터 완전 이관  
**선행조건**: Phase 1-3 완료 (GameState, 게임 시스템, UI)  
**우선순위**: 높음 (사용자 경험 직결)

---

## 1. 인벤토리 UI 카드 그리드 변환

### 현재 문제점
- ListView 기반의 세로 리스트 (Unity 기본 스타일)
- Web 버전은 1행당 5개(희귀도별) 카드 그리드
- 아이템을 네모 카드 형태로 표시해야 함

### 구현 사항

#### 1.1 인벤토리 레이아웃 변경
- [ ] ListView → GridView/GridLayoutGroup으로 변경
- [ ] 아이템을 5열 고정 그리드로 표시
- [ ] 각 아이템 슬롯은 네모 카드 형태 (아이콘 + 이름 + 수량)
- [ ] 잠금 상태 아이템은 회색/반투명 처리
- [ ] count >= 5인 아이템은 합성 가능 표시 (테두리 색상 등)

#### 1.2 UXML 수정
- [ ] `MainGameUI.uxml`의 InventoryItems ListView를 GridView로 변경
- [ ] 아이템 슬롯 템플릿 UXML 생성 (`ItemSlotTemplate.uxml`)
- [ ] USS 스타일 추가 (카드 테두리, 그림자, 호버 효과)

#### 1.3 InventoryUI.cs 리팩토링
- [ ] `RefreshInventoryGrid()`를 그리드 기반으로 변경
- [ ] `createRarityRow()` 로직 이식 (5개 희귀도를 한 행에 표시)
- [ ] 아이템 그룹화 로직 추가 (베이스 이름별 그룹핑)
- [ ] GridView의 makeItem/bindItem을 카드 슬롯용으로 변경

### Web 버전 참고 코드
- `src/ui/InventoryUI.js`의 `renderInventory()`, `createRarityRow()`, `createItemSlot()`
- `src/ui/InventoryUI.js`의 `groupItemsByBase()` - 아이템 그룹화 로직

---

## 2. 업그레이드 UI 표시 개선

### 현재 문제점
- 업그레이드 모달의 아이템 그리드가 표시되지 않음
- UpgradeUI.cs의 `_upgradeContainer`가 UXML에서 제대로 연결되지 않음

### 구현 사항

#### 2.1 UXML 확인 및 수정
- [ ] `UpgradeModal` 내 `UpgradeGrid` ListView의 name 속성 확인
- [ ] 탭별(골드/스탯/보석/환생) 그리드 레이아웃 정의
- [ ] 업그레이드 항목 카드 템플릿 UXML 생성

#### 2.2 UpgradeUI.cs 디버깅
- [ ] `Initialize()`에서 `_upgradeContainer`가 제대로 찾아지는지 확인
- [ ] `RefreshUpgradeGrid()`에서 itemsSource 설정 후 표시되는지 확인
- [ ] 탭 전환 시 그리드 새로고침 로직 확인

---

## 3. 미션 UI 표시 개선

### 현재 문제점
- 미션 모달의 아이템 그리드가 표시되지 않음
- MissionsUI.cs의 `_missionsContainer`가 UXML에서 제대로 연결되지 않음

### 구현 사항

#### 3.1 UXML 확인 및 수정
- [ ] `DailyMissionsModal` 내 `MissionsGrid` ListView의 name 속성 확인
- [ ] 미션 카드 레이아웃 정의 (이름, 설명, 진행도, 보상)
- [ ] 미션 카드 템플릿 UXML 생성

#### 3.2 MissionsUI.cs 디버깅
- [ ] `Initialize()`에서 `_missionsContainer`가 제대로 찾아지는지 확인
- [ ] `RefreshMissionsGrid()`에서 itemsSource 설정 후 표시되는지 확인
- [ ] 탭 전환(일일/주간) 시 그리드 새로고침 로직 확인

---

## 4. CSV 데이터 완전 이관

### 현재 상태
- `items.csv`는 DataLoader로 로드됨
- Bootstrap에서 모든 아이템을 count=0으로 인벤토리에 추가
- 드롭 시스템은 미구현

### 구현 사항

#### 4.1 드롭 시스템 구현
- [ ] `DropTable.cs`를 CSV 데이터 기반으로 수정
- [ ] 몬스터 처치 시 items.csv에서 해당 타입/등급 아이템 드롭
- [ ] 드롭 시 count 증가 (기존 아이템) 또는 count=1 추가 (새 아이템)
- [ ] discoveredItems에 자동 등록

#### 4.2 합성 시스템 구현
- [ ] `InventorySystem.Synthesize()`를 CSV 데이터 기반으로 수정
- [ ] 5개 소모 → 다음 등급 아이템 1개 생성
- [ ] items.csv의 grade/rarity 체계에 따라 다음 아이템 찾기
- [ ] 베이스 아이템 전환 지원 (bronze_sword → iron_sword → steel_sword)

#### 4.3 기타 CSV 데이터 로드
- [ ] 몬스터 데이터 CSV (monster.csv) - CombatSystem에서 사용
- [ ] 업그레이드 비용 데이터 CSV (upgrades.csv) - UpgradeUI에서 사용
- [ ] 미션 데이터 CSV (missions.csv) - DailyMissionSystem에서 사용

---

## 5. UI 테마/스타일 일괄 적용

### 현재 문제점
- USS 스타일이 Web 버전과 다름
- 폰트 크기, 색상, 간격 등이 Unity 기본값

### 구현 사항
- [ ] Web 버전의 CSS를 USS로 변환 적용
- [ ] 카드 테두리 색상 (등급별)
- [ ] 호버 효과 (마우스 오버 시 밝기 증가)
- [ ] 잠금 상태 스타일 (회색/반투명)
- [ ] 합성 가능 표시 (테두리 애니메이션 등)

---

## 6. 테스트

### 6.1 인벤토리 테스트
- [ ] 모든 탭(무기/갑옷/장신구/신발)에서 25개 아이템 표시
- [ ] 5개 희귀도 행이 올바르게 표시되는지
- [ ] count=0 아이템은 잠금, count>=1 아이템은 사용 가능
- [ ] count>=5 아이템은 합성 가능 표시
- [ ] 우클릭 합성 작동
- [ ] 툴팁 표시

### 6.2 업그레이드 테스트
- [ ] 골드 업그레이드 탭에서 모든 업그레이드 항목 표시
- [ ] 스탯 업그레이드 탭 표시
- [ ] 보석 업그레이드 탭 표시
- [ ] 환생 탭 표시
- [ ] 업그레이드 구매 시 골드 차감 및 레벨 증가

### 6.3 미션 테스트
- [ ] 일일 미션 목록 표시
- [ ] 주간 미션 목록 표시
- [ ] 진행도 실시간 업데이트
- [ ] 미션 완료 시 보상 청구

---

## 7. Git 커밋 계획

- [ ] `feat: convert inventory UI to card grid layout`
- [ ] `feat: fix upgrade UI display issues`
- [ ] `feat: fix missions UI display issues`
- [ ] `feat: implement CSV-based drop system`
- [ ] `feat: implement CSV-based synthesis system`
- [ ] `feat: complete Phase 4 - UI improvements and data migration`

---

## 📝 메모

- **우선순위**: 인벤토리 카드 그리드 > 업그레이드/미션 표시 수정 > 드롭/합성 시스템
- **Web 버전 코드 재사용**: `src/ui/InventoryUI.js`, `src/systems/InventorySystem.js` 로직을 C#으로 이식
- **성능 고려**: GridView 가상화 (ListView의 가상화 기능 활용)
- **LSP 에러**: IGameLogger, IEventBus 등 인터페이스 참조 문제는 Unity 리컴파일 필요

---

**Phase 4는 Web 버전과 동일한 사용자 경험을 제공하는 것이 목표입니다.**
- [ ] `Editor/CSVToScriptableObjectConverter.cs` 에디터 스크립트 생성
- [ ] 메뉴 항목: `Tools > Convert CSV > Items`, `Tools > Convert CSV > Monsters`, 등
- [ ] CSV 파일 파싱 (TextAsset 또는 StreamingAssets)
- [ ] ScriptableObject 에셋 일괄 생성 (`AssetDatabase.CreateAsset`)
- [ ] 중복 ID 검사 (`EditorUtility.DisplayDialog`)
- [ ] 필수 필드 검증 (null/빈 값 검사)
- [ ] 진행률 표시 (Progress bar)

### 변환 툴 세부 기능
- [ ] CSV 헤더 자동 매핑 (필드명 일치)
- [ ] 타입 변환 (string → enum, int, float)
- [ ] 에러 로깅 (어느 줄에서 에러 발생)
- [ ] 롤백 기능 (이전 에셋 삭제)
- [ ] 배치 생성 (100개 아이템 한 번에)

### 테스트
- [ ] 변환 툴 실행 테스트 (CSV → SO)
- [ ] 생성된 ScriptableObject 에셋 Inspector에서 확인
- [ ] 필드값 정확성 검증 (CSV 원본과 비교)
- [ ] 중복 ID 검사 테스트 (의도적으로 중복 넣어보기)
- [ ] 대량 데이터 변환 테스트 (100개 아이템, 20개 몬스터)

---

## Day 21: 데이터 로드 시스템

### DataLoader.cs 작성
- [ ] `DataLoader.cs` MonoBehaviour 생성
- [ ] Singleton 패턴 (`DataLoader.Instance`)
- [ ] `Awake()`에서 모든 ScriptableObject 로드

### 로드 메서드
- [ ] `LoadAllItems()` - 모든 아이템 데이터 로드
- [ ] `LoadAllMonsters()` - 모든 몬스터 데이터 로드
- [ ] `LoadAllStages()` - 모든 스테이지 데이터 로드
- [ ] `LoadGameConfig()` - 게임 설정 로드
- [ ] `LoadTutorials()` - 튜토리얼 데이터 로드
- [ ] `LoadAudioDefinitions()` - 오디오 정의 로드

### 조회 메서드 (웹 버전과 동일하게)
- [ ] `GetItem(string id, int grade)` - 아이템 조회
- [ ] `GetMonster(string id)` - 몬스터 조회
- [ ] `GetStage(int stageId)` - 스테이지 조회
- [ ] `GetGameConfig()` - 게임 설정 반환
- [ ] `FilterItems(Func<ItemDataSO, bool> predicate)` - 아이템 필터링
- [ ] `FilterMonsters(Func<MonsterDataSO, bool> predicate)` - 몬스터 필터링

### Addressables 연동 (선택, 대규모 데이터용)
- [ ] Addressable Asset System 설정
- [ ] ScriptableObject 에셋을 Addressable Group으로 등록
- [ ] `Addressables.LoadAssetAsync<T>()` 비동기 로드
- [ ] `Addressables.Release()`로 메모리 해제
- [ ] 온디맨드 로딩 (필요할 때만 로드)

### 캐싱 시스템
- [ ] Dictionary缓存 (첫 로드 후 메모리에 상주)
- [ ] `itemsById`, `monstersById`, `stagesById` 캐시
- [ ] 조회 성능 최적화 (O(1) 해시 조회)

### 데이터 검증
- [ ] `ValidateAllData()` 메서드: 모든 데이터 무결성 검사
- [ ] 중복 ID 검사
- [ ] 필수 필드 null 검사
- [ ] 참조 무결성 (아이템 타입, 몬스터 드롭 테이블, 등)
- [ ] 밸런스 검증 (너무 높거나 낮은 수치 경고)

### 기존 CSV 어댑터 (하위 호환성)
- [ ] `CSVDataLoader.cs` 생성 (기존 CSV 파일을 TextAsset으로 로드)
- [ ] ScriptableObject 전환 완료 전까지 동시 사용
- [ ] 점진적 전환 지원

### 테스트
- [ ] DataLoader 초기화 테스트 (모든 데이터 로드)
- [ ] 조회 메서드 정확도 테스트 (웹 버전과 비교)
- [ ] 필터링 메서드 테스트 (여러 조건)
- [ ] Addressables 비동기 로드 테스트
- [ ] 캐싱 성능 테스트 (10000회 조회 < 10ms)

---

## Day 22: 데이터 검증

### 데이터 무결성 검사

#### 아이템 데이터
- [ ] 모든 아이템 ID 고유성 확인
- [ ] 타입/희귀도/등급 조합 유효성
- [ ] 스탯 값 범위 검사 (음수, 비현실적 수치)
- [ ] 아이콘 스프라이트 존재 여부
- [ ] 설명 텍스트 비어있지 않은지

#### 몬스터 데이터
- [ ] 모든 몬스터 ID 고유성
- [ ] HP/공격력/방어력 > 0
- [ ] 드롭 테이블 확률 합 = 100% (또는 의도적 조정)
- [ ] 보스 플래그 일관성 (10층 단위)
- [ ] 스프라이트/애니메이션 존재 여부

#### 스테이지 데이터
- [ ] 스테이지 ID 연속성 (1-20)
- [ ] 보스 스테이지 플래그 (10, 20)
- [ ] 몬스터 레벨/스펙 증가 곡선 단조증가
- [ ] 보상 골드/EXP 합리성

#### 게임 설정
- [ ] 모든 상수 > 0
- [ ] 오프라인 보상 배율 (0.1)
- [ ] 드롭 확률 합 (100%)
- [ ] 경험치 곡선 검증

### 웹 버전과 수치 비교
- [ ] 아이템 100종 스탯 비교 (±5% 이내)
- [ ] 몬스터 20종 스펙 비교 (±5% 이내)
- [ ] 스테이지 난이도 곡선 비교
- [ ] 드롭 확률 비교 (일반/고급/희귀/영웅/전설)
- [ ] 게임 설정 상수 비교 (완전 일치 목표)

### 성능 테스트
- [ ] DataLoader 초기 로드 시간 측정 (1초 이내 목표)
- [ ] 조회 성능 (10000회 조회 < 10ms)
- [ ] Addressables 비동기 로드 시간 (에셋당 < 100ms)
- [ ] 메모리 사용량 (모든 데이터 로드 후 < 50MB)

### 에러 처리
- [ ] 누락된 데이터 로드시 경고 로그
- [ ] 잘못된 데이터 스킵 및 기본값 사용
- [ ] 에러 리포트 생성 (어떤 데이터에 문제 있는지)

### 최종 검증 리포트
- [ ] `DataValidationReport.txt` 생성
- [ ] 총 데이터 개수 (아이템/몬스터/스테이지)
- [ ] 에러/경고 개수
- [ ] 웹 버전과의 차이점
- [ ] 수정 권고사항

---

## Phase 4 완료 체크리스트

### 필수 항목
- [ ] 모든 ScriptableObject 에셋 생성 완료
- [ ] CSV 데이터와 ScriptableObject 수치 100% 일치
- [ ] DataLoader 조회 성능 (10000회 조회 < 10ms)
- [ ] 데이터 무결성 검사 통과 (에러 0, 경고 최소화)
- [ ] 웹 버전과의 수치 비교 ±5% 이내
- [ ] Addressables 연동 테스트 통과 (선택사항)

### 코드 품질
- [ ] ScriptableObject 클래스에 XML 문서 주석
- [ ] 에디터 스크립트에 메뉴 항목 명확히
- [ ] 데이터 검증 로깅 상세하게
- [ ] 예외 처리 (null 체크, Try-Catch)

### Git 커밋
- [ ] Day 20: `feat: create ScriptableObject classes and conversion tool`
- [ ] Day 21: `feat: implement DataLoader with caching`
- [ ] Day 22: `feat: complete data validation and verification`
- [ ] Phase 4 완료: `feat: complete Phase 4 - data migration`

### 다음 Phase 준비
- [ ] Phase 5 (사운드/이펙트)를 위한 오디오 에셋 수집
- [ ] 파티클 이펙트 레퍼런스 준비
- [ ] BGM/SFX 파일 정리

---

## 📝 메모

- **ScriptableObject**: 빌드 시 에셋에 포함되므로 런타임 수정 불가 (밸런스 조정은 에디터에서)
- **Addressables**: 데이터가 많을 경우 유용하지만, 작은 프로젝트에서는 오버킬일 수 있음
- **CSV 어댑터**: 점진적 전환을 위해 잠시 유지하다가 Phase 4 완료 후 제거
- **데이터 검증**: 자동화 스크립트로 매 빌드마다 검증하면 좋음
- **버전 관리**: ScriptableObject 에셋도 Git으로 관리 (바이너리 파일은 LFS)

---

**Phase 4는 데이터의 정확성과 일관성이 핵심입니다. 웹 버전과의 수치 차이를 최소화하세요.**
