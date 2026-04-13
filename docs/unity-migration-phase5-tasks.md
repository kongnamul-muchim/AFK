# Unity 이식 Phase 5: 사운드/이펙트 추가 (3-5일)

**목표**: 오디오/비주얼 효과 강화  
**선행조건**: Phase 1-4 완료 (GameState, 게임 시스템, UI, 데이터)

---

## Day 23-24: 오디오 시스템

### AudioManager.cs 작성
- [ ] `AudioManager.cs` MonoBehaviour 생성
- [ ] Singleton 패턴 (`AudioManager.Instance`)
- [ ] `DontDestroyOnLoad` 설정

### 오디오 소스 풀
- [ ] BGM용 AudioSource (1개, loop)
- [ ] SFX용 AudioSource 풀 (10개, 재사용)
- [ ] UI 사운드용 AudioSource (2개)
- [ ] Object Pooling으로 AudioSource 관리

### 오디오 카테고리
- [ ] `AudioCategory` enum: `BGM`, `SFX`, `UI`, `Ambience`
- [ ] 카테고리별 볼륨 제어 (AudioMixer 또는 개별 볼륨)
- [ ] `SetVolume(AudioCategory category, float volume)` 메서드

### 사운드 재생 메서드
- [ ] `PlayBGM(string audioId)` - BGM 재생 (페이드 인/아웃)
- [ ] `PlaySFX(string audioId)` - 효과음 재생 (위치 3D 또는 2D)
- [ ] `PlayUISound(string audioId)` - UI 사운드 (버튼 클릭, 등)
- [ ] `StopBGM()` - BGM 정지 (페이드 아웃)
- [ ] `StopAllSFX()` - 모든 효과음 정지

### AudioDefinitionSO 연동
- [ ] `AudioDefinitionSO` ScriptableObject 사용
- [ ] 필드: `id`, `clip`, `volume`, `pitch`, `loop`, `category`
- [ ] `DataLoader`를 통해 오디오 정의 로드
- [ ] `PlaySound(string audioId)` 메서드에서 ID로 조회

### 오디오 믹서
- [ ] `AudioMixer` 에셋 생성 (`MasterMixer`)
- [ ] BGM, SFX, UI 그룹 분리
- [ ] 그룹별 볼륨/로우패스/하이패스 설정
- [ ] `AudioMixerSnapshot` (일시정지, 등)

### 사운드 라이브러리
- [ ] BGM: 메인 테마, 전투 BGM, 보스 BGM, 승리 BGM
- [ ] SFX: 공격, 피격, 처치, 레벨업, 아이템 획득, 합성, 업그레이드
- [ ] UI: 버튼 클릭, 패널 열기/닫기, 알림
- [ ] Ambience: 배경음 (선택)

### 볼륨/뮤트 설정
- [ ] `settings.bgmVolume`, `settings.sfxVolume` (GameState)
- [ ] 설정 패널의 슬라이더와 연동
- [ ] 뮤트 토글 (`AudioListener.pause`)
- [ ] 저장/복원 (SaveManager)

### 모바일 진동
- [ ] `Handheld.Vibrate()` (iOS/Android)
- [ ] 진동 패턴 (공격, 피격, 처치)
- [ ] 설정에서 진동 온/오프

### 테스트
- [ ] 모든 사운드 재생 테스트
- [ ] BGM 페이드 인/아웃 테스트
- [ ] 동시 여러 SFX 재생 테스트 (10개 이상)
- [ ] 볼륨 조절 테스트 (0-100%)
- [ ] 뮤트 토글 테스트
- [ ] 모바일 진동 테스트 (실기기)

---

## Day 25-26: 이펙트/애니메이션

### 파티클 시스템

#### 공격 이펙트
- [ ] `ParticleAttackHit.prefab` - 공격 타격 이펙트
- [ ] 색상: 주황/노랑 (불꽃)
- [ ] 지속시간: 0.3초
- [ ] 크기: 작음 (캐릭터 근처)

#### 피격 이펙트
- [ ] `ParticleDamageFlash.prefab` - 피격 플래시
- [ ] 색상: 빨강 (데미지)
- [ ] 데미지 숫자 표시 (UI Text, 위로 떠오름)
- [ ] 지속시간: 0.5초

#### 처치 이펙트
- [ ] `ParticleMonsterDeath.prefab` - 몬스터 처치 이펙트
- [ ] 폭발 효과 + 아이템 드롭 파티클
- [ ] 색상: 금색 (아이템 드롭)
- [ ] 지속시간: 1초

#### 레벨업 이펙트
- [ ] `ParticleLevelUp.prefab` - 레벨업 이펙트
- [ ] 빛 효과 (radial burst)
- [ ] 색상: 금색/흰색
- [ ] 지속시간: 2초

#### 합성 이펙트
- [ ] `ParticleSynthesis.prefab` - 아이템 합성 이펙트
- [ ] 빛 기둥 + 아이템 아이콘 표시
- [ ] 지속시간: 1.5초

#### 환생 이펙트
- [ ] `ParticleRebirth.prefab` - 환생 이펙트
- [ ] 화면 전체 빛 효과
- [ ] 지속시간: 3초

### 파티클 매니저
- [ ] `ParticleManager.cs` MonoBehaviour 생성
- [ ] Object Pooling (`ParticleSystem` 풀)
- [ ] `PlayParticle(string particleId, Vector3 position)` 메서드
- [ ] 자동 정리 (재생 완료 후 비활성화)

### UI 애니메이션

#### 패널 등장/소실
- [ ] `PanelAnimationController.cs` MonoBehaviour 생성
- [ ] 페이드 인/아웃 (CanvasGroup.alpha)
- [ ] 확대/축소 (RectTransform.localScale)
- [ ] 슬라이드 (anchoredPosition)
- [ ] DOTween 또는 Unity Animation 사용

#### 버튼 피드백
- [ ] 버튼 호버 (색상 변화, 약간 확대)
- [ ] 버튼 클릭 (축소 → 복원)
- [ ] 버튼 비활성화 (회색, 투명도 감소)

#### 토스트 알림
- [ ] 위에서 아래로 슬라이드 + 페이드
- [ ] 아이콘 애니메이션 (bounce)
- [ ] 자동 사라짐 (3초 후 페이드 아웃)

#### HUD 업데이트
- [ ] 골드/EXP 증가 시 숫자 카운트업 애니메이션
- [ ] HP 바 색상 변화 (50% 이하: 주황, 25% 이하: 빨강)
- [ ] 레벨업 시 레벨 텍스트 팝

### 캐릭터/몬스터 애니메이션 블렌딩
- [ ] Animator Controller에 블렌드 트리 추가
- [ ] Idle/Move/Attack 애니메이션 자연스러운 전이
- [ ] Attack 3프레임 연쇄 (Animation Event로 타이밍)
- [ ] Hit/Dead 애니메이션 (한 번 재생 후 Idle로)

### 카메라 이펙트
- [ ] 피격 시 카메라 쉐이크 (짧게)
- [ ] 처치 시 카메라 줌인/줌아웃 (선택)
- [ ] 보스 등장 시 카메라 포커스 (선택)

### 테스트
- [ ] 모든 파티클 이펙트 재생 테스트
- [ ] Object Pooling 성능 테스트 (동시 20개 이상)
- [ ] UI 애니메이션 자연스러움 확인
- [ ] 버튼 피드백 터치/마우스 테스트
- [ ] 애니메이션 블렌딩 부드러움 확인
- [ ] 카메라 이펙트 테스트 (멀미 유발하지 않는지)

---

## Day 27: 사운드/이펙트 통합 테스트

### 시나리오별 테스트

#### 1. 게임 시작 → 첫 전투
- [ ] 시작 BGM 재생
- [ ] 몬스터 등장 사운드
- [ ] 공격 사운드 (3프레임 연쇄)
- [ ] 피격 이펙트 + 사운드
- [ ] 처치 이펙트 + 사운드 + 아이템 드롭 사운드

#### 2. 레벨업
- [ ] 레벨업 사운드
- [ ] 레벨업 파티클 이펙트
- [ ] HUD 레벨 텍스트 애니메이션

#### 3. 아이템 합성
- [ ] 합성 사운드
- [ ] 합성 파티클 이펙트
- [ ] 인벤토리 UI 업데이트 애니메이션

#### 4. 업그레이드 구매
- [ ] 구매 사운드
- [ ] 골드 감소 애니메이션
- [ ] 스탯 증가 숫자 팝

#### 5. 오프라인 보상
- [ ] 오프라인 보상 모달 등장 애니메이션
- [ ] 보상 아이템 드롭 사운드
- [ ] 청구 버튼 클릭 사운드

#### 6. 환생
- [ ] 환생 이펙트 (화면 전체)
- [ ] 환생 사운드 (웅장한)
- [ ] 데이터 초기화 후 새 게임 BGM

### 성능 테스트
- [ ] 파티클 이펙트 동시 20개 이상에서도 60fps 유지
- [ ] 오디오 소스 10개 이상 동시 재생 시 끊김 없음
- [ ] UI 애니메이션 여러 개 동시 재생 시 프레임 드롭 없음
- [ ] 모바일에서 배터리 소모 테스트 (5분 연속 플레이)

### 크로스플랫폼 테스트
- [ ] WebGL: 오디오 재생 (브라우저 제한 확인)
- [ ] Android: 진동, 오디오 출력
- [ ] iOS: 진동, 오디오 출력

### 버그 수정
- [ ] 발견된 사운드/이펙트 버그 목록 작성
- [ ] 우선순위별 수정 (소리 끊김, 이펙트 미재생, 등)
- [ ] 수정 후 재테스트

---

## Phase 5 완료 체크리스트

### 필수 항목
- [ ] 모든 사운드 재생 정상
- [ ] 모든 파티클 이펙트 재생 정상
- [ ] UI 애니메이션 자연스러움
- [ ] Object Pooling으로 성능 최적화
- [ ] 볼륨/뮤트 설정 저장/복원
- [ ] 모바일 진동 작동

### 코드 품질
- [ ] AudioManager에 XML 문서 주석
- [ ] 파티클 프리팹 네이밍 일관성
- [ ] AudioDefinitionSO 필드 완전성
- [ ] 예외 처리 (오디오 클립 누락, 등)

### Git 커밋
- [ ] Day 23-24: `feat: implement AudioManager with pooling`
- [ ] Day 25-26: `feat: implement particle effects and UI animations`
- [ ] Day 27: `feat: complete Phase 5 audio/effects integration`
- [ ] Phase 5 완료: `feat: complete Phase 5 - audio and effects`

### 다음 Phase 준비
- [ ] Phase 6 (최적화/폴리싱)을 위한 프로파일링 준비
- [ ] Unity Profiler 사용법 복습
- [ ] 모바일 빌드 환경 설정 (SDK/NDK)

---

## 📝 메모

- **오디오**: WebGL에서는 오디오 컨텍스트 제한 (사용자 상호작용 필요)
- **파티클**: Mobile에서는 최대 입자 수 제한 (성능 고려)
- **UI 애니메이션**: DOTween 사용 시 더 부드러운 애니메이션 가능 (유료 에셋)
- **Object Pooling**: 파티클, AudioSource 모두 풀링으로 성능 최적화
- **모바일**: 진동은 배터리를 많이 소모하므로 설정에서 조절 가능하게

---

**Phase 5는 게임의 몰입감을 크게 향상시키는 단계입니다. 과하지 않으면서도 적절한 피드백을 제공하세요.**
