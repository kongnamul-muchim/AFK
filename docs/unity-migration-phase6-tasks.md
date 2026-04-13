# Unity 이식 Phase 6: 최적화 및 폴리싱 (3-5일)

**목표**: 성능 최적화 + 플랫폼 빌드  
**선행조건**: Phase 1-5 완료 (GameState, 게임 시스템, UI, 데이터, 사운드/이펙트)

---

## Day 28-29: 최적화

### Object Pooling

#### 몬스터 풀
- [ ] `MonsterPool.cs` MonoBehaviour 생성
- [ ] 몬스터 프리팹 풀 (10마리)
- [ ] `GetMonster()` / `ReleaseMonster()` 메서드
- [ ] 몬스터 처치 시 즉시 Destroy 대신 비활성화
- [ ] 재사용 시 상태 초기화 (HP, 포지션, 애니메이션)

#### 파티클 풀
- [ ] `ParticleManager`의 Object Pooling 이미 구현됨 확인
- [ ] 파티클 시스템 재사용 (재생 완료 후 비활성화)
- [ ] 풀 크기 동적 조정 (필요시)

#### UI 요소 풀
- [ ] 인벤토리 슬롯 재사용 (UI Toolkit `ListView` 가상화)
- [ ] 토스트 알림 풀 (5개)
- [ ] 모달 패널 재사용 (활성화/비활성화)

### Sprite Atlas 설정
- [ ] `Sprite Atlas` 에셋 생성 (`Characters`, `Monsters`, `UI`, `Effects`)
- [ ] 각 카테고리별 스프라이트 등록
- [ ] `Include in Build` 체크
- [ ] 배치 드로우콜 최소화 확인

### Addressables 설정
- [ ] Addressable Asset Groups 생성:
  - [ ] `characters` - 캐릭터 스프라이트/애니메이션
  - [ ] `monsters` - 몬스터 스프라이트/애니메이션
  - [ ] `backgrounds` - 배경 스프라이트
  - [ ] `ui-panels` - UI 패널 (선택)
  - [ ] `audio` - 오디오 클립 (선택)
- [ ] `Build Addressable Content` 테스트
- [ ] 프로파일 시뮬레이션 (네트워크 속도)

### 모바일 최적화

#### 터치 입력
- [ ] Unity Input System 설정 (구 Input Manager 대체)
- [ ] 터치/마우스 동시 지원
- [ ] 멀티터치 지원 (핀치 줌, 등)
- [ ] 터치 영역 최소 크기 (44x44 포인트)

#### 해상도 대응
- [ ] Canvas Scaler 설정 (`Scale With Screen Size`)
- [ ] 참조 해상도: 1920x1080
- [ ] UI 요소 최소/최대 크기 제한
- [ ] 잘림 방지 (Safe Area)

#### 성능 설정
- [ ] Target Frame Rate: 60 (모바일), 30 (저사양)
- [ ] VSync: Off (모바일)
- [ ] Antialiasing: 2x 또는 Off (성능 우선)
- [ ] Texture Compression: ASTC (Android), PVRTC (iOS)

#### 메모리 최적화
- [ ] Texture Import Settings: `Max Size` 조정 (512-1024)
- [ ] 오디오 압축: VBR (보이스), AAC (BGM)
- [ ] 불필요한 에셋 제거 (Resources 폴더 정리)
- [ ] `Resources.UnloadUnusedAssets()` (장면 전환 시)

### 코드 최적화
- [ ] `Update()` 호출 최소화 (고정 timestep 게임 로직)
- [ ] `GetComponent()` 캐싱 (Awake에서 한 번)
- [ ] 문자열 연결 최소화 (`StringBuilder` 사용)
- [ ] LINQ 사용 최소화 (성능 이슈)
- [ ] 코루틴 정리 (불필요한 동시 실행 방지)

### 프로파일링
- [ ] Unity Profiler 실행
- [ ] CPU Usage 분석 (어떤 시스템이 무거운지)
- [ ] GPU Usage 분석 (렌더링 병목)
- [ ] 메모리 사용량 (200MB 이하 목표)
- [ ] 프레임레이트 (60fps 유지 목표)

### 테스트
- [ ] Object Pooling 성능 테스트 (1000회 생성/파괴)
- [ ] Sprite Atlas 배치 드로우콜 확인
- [ ] Addressables 비동기 로딩 시간 측정
- [ ] 모바일 해상도 대응 테스트 (여러 기기)
- [ ] Unity Profiler로 최적화 전/후 비교

---

## Day 30: 크로스플랫폼 빌드

### WebGL 빌드 설정
- [ ] Build Settings → WebGL 플랫폼 선택
- [ ] Player Settings:
  - [ ] Company Name, Product Name 설정
  - [ ] Version: 1.0.0
  - [ ] Icon 설정
  - [ ] Resolution Presentation: Fullscreen
  - [ ] Color Space: Linear (권장)
  - [ ] Scripting Backend: IL2CPP
  - [ ] Exception Handling: Explicit (성능)
  - [ ] Decompression Fallback: 체크 (로딩 시간 단축)
  - [ ] Data Caching: 체크 (오프라인 지원)
- [ ] Publishing Settings:
  - [ ] Compression Format: Brotli (권장) 또는 Gzip
  - [ ] Enable Exceptions: Explicitly Thrown
- [ ] Build → `Builds/WebGL/` 폴더

### WebGL 호스팅 테스트
- [ ] 로컬 웹 서버에서 테스트 (Python: `python -m http.server 8000`)
- [ ] 브라우저에서 실행 (Chrome, Firefox, Safari)
- [ ] 로딩 시간 측정 (5초 이내 목표)
- [ ] localStorage 대체 (`Application.persistentDataPath` → IndexedDB)
- [ ] 오디오 컨텍스트 제한 테스트 (사용자 클릭 필요)

### Android 빌드 설정
- [ ] Build Settings → Android 플랫폼 선택
- [ ] Player Settings:
  - [ ] Package Name: `com.yourstudio.afk`
  - [ ] Minimum API Level: Android 7.0 (API 24)
  - [ ] Target API Level: 최신 (API 33+)
  - [ ] Scripting Backend: IL2CPP
  - [ ] Target Architectures: ARM64 (권장)
  - [ ] Split APK: 체크 (크기 최적화)
- [ ] Publishing Settings:
  - [ ] Keystore 생성 (릴리스용)
  - [ ] 버전 코드/이름 설정
- [ ] Build → `Builds/Android/afk.apk`

### Android 테스트
- [ ] APK 설치 (기기 또는 에뮬레이터)
- [ ] 터치 입력 테스트
- [ ] 백그라운드/포그라운드 전환 테스트
- [ ] 저장 데이터 확인 (`/storage/emulated/0/Android/data/...`)
- [ ] 오디오 출력 테스트
- [ ] 진동 테스트

### iOS 빌드 설정 (선택)
- [ ] Build Settings → iOS 플랫폼 선택
- [ ] Player Settings:
  - [ ] Bundle Identifier: `com.yourstudio.afk`
  - [ ] Minimum Target Version: iOS 13.0
  - [ ] Target Device: iPhone + iPad
  - [ ] Orientation: Portrait (세로 모드)
  - [ ] Status Bar: Hidden (선택)
- [ ] Build → Xcode 프로젝트 생성
- [ ] Xcode에서 서명/빌드 (Apple Developer 계정 필요)

### iOS 테스트 (선택)
- [ ] TestFlight 배포 (선택)
- [ ] 실제 기기 테스트
- [ ] 터치 입력, 진동, 오디오 테스트

### 빌드 크기 최적화
- [ ] WebGL: 10MB 이하 목표 (Brotli 압축)
- [ ] Android APK: 50MB 이하 목표 (Split APK)
- [ ] iOS IPA: 100MB 이하 목표 (App Thinning)
- [ ] Addressables로 에셋 분리 (온디맨드 로딩)

---

## Day 31-32: 최종 테스트 및 버그 수정

### 종합 테스트 시나리오

#### 1. 처음부터 끝까지 플레이
- [ ] 게임 시작 → 튜토리얼 → 첫 전투 → 레벨업 → 스테이지 클리어
- [ ] 인벤토리 관리 → 합성 → 장비 장착
- [ ] 일일/주간 미션 수행 → 보상 청구
- [ ] 오프라인 보상 받기
- [ ] 환생 실행 → 재진행
- [ ] 모든 과정 저장/로드 테스트

#### 2. 엣지 케이스
- [ ] 인벤토리 풀 (50슬롯) 상태에서 아이템 획득
- [ ] 동시 여러 시스템 이벤트 (레벨업 + 미션완료 + 아이템드롭)
- [ ] 오프라인 24시간 후 접속
- [ ] 저장 데이터 손상 시나리오 (파일 삭제 후 복구)
- [ ] 앱 강제 종료 후 재시작

#### 3. 성능 테스트
- [ ] 1시간 연속 플레이 (메모리 누수 확인)
- [ ] 전투 1000회 반복 (프레임레이트 유지)
- [ ] 인벤토리 50슬롯 풀 상태에서 합성/장착 반복
- [ ] 파티클 이펙트 동시 20개 이상
- [ ] 모바일 배터리 소모 테스트 (30분)

#### 4. 크로스플랫폼 호환성
- [ ] WebGL: Chrome, Firefox, Safari, Edge
- [ ] Android: 다양한 해상도/기기 (저사양 포함)
- [ ] iOS: iPhone/iPad (선택)
- [ ] 데이터 동기화 (같은 세이브 파일로 여러 플랫폼)

#### 5. 사용자 경험
- [ ] 튜토리얼 명확성 (초보자가 이해할 수 있는지)
- [ ] UI 직관성 (무엇을 클릭해야 할지 명확한지)
- [ ] 피드백 충분성 (공격/피격/보상 등)
- [ ] 진행 속도 (너무 빠르거나 느리지 않은지)

### Unity Profiler 상세 분석
- [ ] CPU Usage: 16.67ms 이하 (60fps)
- [ ] GPU Usage: 16.67ms 이하
- [ ] 메모리: 200MB 이하 (모바일)
- [ ] PhysX, Animation, Rendering, Scripts별 사용량
- [ ] 가비지 컬렉션 빈도 (최소화)

### 버그 수정
- [ ] 발견된 버그 목록 작성 (치명적/중요/경미)
- [ ] 우선순위별 수정
- [ ] 수정 후 재테스트 (회귀 테스트)
- [ ] Git 커밋: `fix: [설명]`

### 최종 검증
- [ ] 모든 시스템 정상 작동
- [ ] 저장/로드 100회 연속 성공
- [ ] 크로스플랫폼 빌드 정상
- [ ] 성능 목표 달성 (60fps, 200MB 이하)
- [ ] 사용자 경험 만족 (직관적, 재미있는)

---

## Phase 6 완료 체크리스트

### 필수 항목
- [ ] WebGL 빌드 정상 동작 (Chrome, Firefox, Safari)
- [ ] Android APK 빌드 및 기기 설치 정상
- [ ] iOS 빌드 정상 (선택사항)
- [ ] Unity Profiler에서 프레임 60fps 유지 (모바일 기준 30fps)
- [ ] 메모리 사용량 200MB 이하 (모바일)
- [ ] 기존 웹 세이브 데이터 불러오기 성공
- [ ] 1시간 연속 플레이 시 크래시 없음
- [ ] 백그라운드 → 포그라운드 전환 시 데이터 복구 정상

### 코드 품질
- [ ] 모든 클래스에 XML 문서 주석 완료
- [ ] 코드 정리 (주석 처리된 코드 제거, 네이밍 일관성)
- [ ] 예외 처리 완성 (null 체크, Try-Catch)
- [ ] 성능 최적화 (Object Pooling, 캐싱, 등)

### Git 커밋
- [ ] Day 28-29: `perf: optimize object pooling and sprite atlas`
- [ ] Day 30: `feat: configure cross-platform builds (WebGL, Android, iOS)`
- [ ] Day 31-32: `fix: final bug fixes and polish`
- [ ] Phase 6 완료: `feat: complete Phase 6 - optimization and polish`
- [ ] **전체 이식 완료**: `feat: complete Unity migration - all phases done`

### 릴리스 준비
- [ ] CHANGELOG.md 업데이트 (Unity 버전)
- [ ] README.md 업데이트 (설치/실행 방법)
- [ ] Git 태그: `v1.0.0-unity`
- [ ] GitHub Releases에 빌드 파일 업로드
- [ ] 웹 호스팅 (GitHub Pages, Netlify, 등)

---

## 📝 메모

- **WebGL**: 오디오 컨텍스트 제한 (사용자 클릭 필요), localStorage 대체 (IndexedDB)
- **Android**: SDK/NDK 버전 일치, 64비트 필수 (Google Play 정책)
- **iOS**: Apple Developer 계정 ($99/년), TestFlight 배포 (내부/외부 테스트)
- **성능**: 모바일에서는 특히 Object Pooling과 Sprite Atlas 중요
- **저장**: 크로스플랫폼 세이브 동기화는 별도 서버 필요 (준비되지 않음)

---

**Phase 6은 게임의 완성도를 결정하는 최종 단계입니다. 성능과 사용자 경험을 철저히 검증하세요.**

---

# 🎉 Unity 이식 프로젝트 완료!

모든 Phase가 완료되었습니다. 이제 웹 게임을 Unity 2D 게임으로 완전히 이관했습니다.

**다음 단계**:
- [ ] WebGL 빌드를 웹 호스팅에 배포
- [ ] Android/iOS 빌드를 스토어에 제출 (선택)
- [ ] 사용자 피드백 수집 및 업데이트
- [ ] 추가 콘텐츠 개발 (던전, PvP, 길드, 등)

**축하합니다!** 🎮
