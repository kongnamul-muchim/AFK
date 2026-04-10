using UnityEngine;

/// <summary>
/// 게임 뷰 렌더링을 담당하는 클래스
/// 캐릭터, 몬스터, 배경, 이펙트 등을 렌더링합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class GameRenderer : MonoBehaviour
{
    private static GameRenderer _instance;
    
    /// <summary>
    /// GameRenderer의 싱글톤 인스턴스
    /// </summary>
    public static GameRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameRenderer");
                _instance = go.AddComponent<GameRenderer>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    private void InjectDependencies()
    {
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
    }

    // ========== 렌더링 요소 ==========
    
    [Header("Player")]
    [SerializeField] private SpriteRenderer _playerSpriteRenderer;
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Transform _playerTransform;
    
    [Header("Monster")]
    [SerializeField] private SpriteRenderer _monsterSpriteRenderer;
    [SerializeField] private Animator _monsterAnimator;
    [SerializeField] private Transform _monsterTransform;
    
    [Header("Background")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private Transform _backgroundTransform;
    
    [Header("Camera")]
    [SerializeField] private Camera _mainCamera;
    
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float backgroundParallaxFactor = 0.5f;
    
    // ========== 상태 ==========
    
    private Vector3 _playerStartPosition;
    private Vector3 _monsterStartPosition;
    private bool _isPlayerMoving = false;
    private bool _isMonsterMoving = false;
    
    // ========== MonoBehaviour 라이프사이클 ==========
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 의존성 주입은 OnEnable에서 (Bootstrap보다 늦게 호출되도록)
        
        // 초기 위치 저장
        if (_playerTransform != null)
            _playerStartPosition = _playerTransform.position;
        if (_monsterTransform != null)
            _monsterStartPosition = _monsterTransform.position;
    }
    
    private void OnEnable()
    {
        // 의존성 주입 (Bootstrap 이후에 호출됨)
        InjectDependencies();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.COMBAT_PHASE_CHANGED, OnCombatPhaseChanged);
        _eventBus.On(GameEvents.COMBAT_ENCOUNTER, OnCombatEncounter);
        _eventBus.On(GameEvents.COMBAT_VICTORY, OnCombatVictory);
        _eventBus.On(GameEvents.COMBAT_DEFEAT, OnCombatDefeat);
    }
    
    private void OnDisable()
    {
        // 이벤트 해제
        if (_eventBus != null)
        {
            _eventBus.Off(GameEvents.COMBAT_PHASE_CHANGED, OnCombatPhaseChanged);
            _eventBus.Off(GameEvents.COMBAT_ENCOUNTER, OnCombatEncounter);
            _eventBus.Off(GameEvents.COMBAT_VICTORY, OnCombatVictory);
            _eventBus.Off(GameEvents.COMBAT_DEFEAT, OnCombatDefeat);
        }
    }
    
    private void Update()
    {
        UpdatePlayerAnimation();
        UpdateMonsterAnimation();
        UpdateBackgroundScroll();
    }
    
    // ========== 플레이어 렌더링 ==========
    
    private void UpdatePlayerAnimation()
    {
        if (_playerAnimator == null) return;
        
        CombatPhase currentPhase = CombatSystem.Instance.CurrentPhase;
        
        switch (currentPhase)
        {
            case CombatPhase.IDLE:
                _playerAnimator.SetBool("IsMoving", false);
                _playerAnimator.SetBool("IsAttacking", false);
                _playerAnimator.SetTrigger("Idle");
                break;
                
            case CombatPhase.MOVING:
                _playerAnimator.SetBool("IsMoving", true);
                _playerAnimator.SetBool("IsAttacking", false);
                break;
                
            case CombatPhase.ENCOUNTERING:
                _playerAnimator.SetBool("IsMoving", false);
                _playerAnimator.SetBool("IsAttacking", false);
                break;
                
            case CombatPhase.COMBAT:
                // 공격 애니메이션은 CombatSystem의 공격 타이밍에 동기화
                break;
                
            case CombatPhase.VICTORY:
                _playerAnimator.SetTrigger("Victory");
                break;
                
            case CombatPhase.DEFEATED:
                _playerAnimator.SetTrigger("Defeated");
                break;
        }
    }
    
    /// <summary>
    /// 플레이어 공격 애니메이션 트리거
    /// </summary>
    public void TriggerPlayerAttack()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.SetTrigger("Attack");
        }
    }
    
    /// <summary>
    /// 플레이어 피격 애니메이션 트리거
    /// </summary>
    public void TriggerPlayerHit()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.SetTrigger("Hit");
        }
    }
    
    // ========== 몬스터 렌더링 ==========
    
    private void UpdateMonsterAnimation()
    {
        if (_monsterAnimator == null) return;
        
        CombatPhase currentPhase = CombatSystem.Instance.CurrentPhase;
        
        switch (currentPhase)
        {
            case CombatPhase.IDLE:
                _monsterAnimator.SetBool("IsMoving", false);
                _monsterAnimator.SetBool("IsAttacking", false);
                break;
                
            case CombatPhase.MOVING:
                _monsterAnimator.SetBool("IsMoving", false);
                _monsterAnimator.SetBool("IsAttacking", false);
                break;
                
            case CombatPhase.ENCOUNTERING:
                _monsterAnimator.SetTrigger("Charge");
                break;
                
            case CombatPhase.COMBAT:
                // 공격 애니메이션은 CombatSystem의 공격 타이밍에 동기화
                break;
                
            case CombatPhase.VICTORY:
                _monsterAnimator.SetTrigger("Defeated");
                break;
                
            case CombatPhase.DEFEATED:
                _monsterAnimator.SetBool("IsAttacking", false);
                break;
        }
    }
    
    /// <summary>
    /// 몬스터 공격 애니메이션 트리거
    /// </summary>
    public void TriggerMonsterAttack()
    {
        if (_monsterAnimator != null)
        {
            _monsterAnimator.SetTrigger("Attack");
        }
    }
    
    /// <summary>
    /// 몬스터 피격 애니메이션 트리거
    /// </summary>
    public void TriggerMonsterHit()
    {
        if (_monsterAnimator != null)
        {
            _monsterAnimator.SetTrigger("Hit");
        }
    }
    
    // ========== 배경 렌더링 ==========
    
    private void UpdateBackgroundScroll()
    {
        if (_backgroundTransform == null) return;
        
        CombatPhase currentPhase = CombatSystem.Instance.CurrentPhase;
        
        if (currentPhase == CombatPhase.MOVING)
        {
            // 이동 중 배경 스크롤
            Vector3 scrollDirection = Vector3.right * moveSpeed * backgroundParallaxFactor * Time.deltaTime;
            _backgroundTransform.position -= scrollDirection;
        }
    }
    
    // ========== 위치 업데이트 ==========
    
    /// <summary>
    /// 플레이어 위치 업데이트 (이동 애니메이션용)
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    public void MovePlayerTo(Vector3 targetPosition)
    {
        if (_playerTransform == null) return;
        
        _isPlayerMoving = true;
        StartCoroutine(SmoothMove(_playerTransform, targetPosition, () => _isPlayerMoving = false));
    }
    
    /// <summary>
    /// 몬스터 위치 업데이트
    /// </summary>
    public void MoveMonsterTo(Vector3 targetPosition)
    {
        if (_monsterTransform == null) return;
        
        _isMonsterMoving = true;
        StartCoroutine(SmoothMove(_monsterTransform, targetPosition, () => _isMonsterMoving = false));
    }
    
    private System.Collections.IEnumerator SmoothMove(Transform transform, Vector3 targetPosition, System.Action onComplete)
    {
        Vector3 startPosition = transform.position;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
        onComplete?.Invoke();
    }
    
    // ========== 이벤트 핸들러 ==========
    
    private void OnCombatPhaseChanged()
    {
        CombatPhase phase = CombatSystem.Instance.CurrentPhase;
        _logger.Debug($"렌더러: 페이즈 변경 감지 - {phase}");
    }
    
    private void OnCombatEncounter()
    {
        // 몬스터 등장 이펙트
        if (_monsterTransform != null)
        {
            _monsterTransform.localPosition = _monsterStartPosition + Vector3.left * 5f;
            MoveMonsterTo(_monsterStartPosition);
        }
    }
    
    private void OnCombatVictory()
    {
        // 승리 이펙트
        _logger.Debug("렌더러: 승리 이펙트");
    }
    
    private void OnCombatDefeat()
    {
        // 패배 이펙트
        _logger.Debug("렌더러: 패배 이펙트");
    }
    
    // ========== 카메라 제어 ==========
    
    /// <summary>
    /// 카메라 줌
    /// </summary>
    /// <param name="zoomIn">줌인 여부</param>
    public void ZoomCamera(bool zoomIn)
    {
        if (_mainCamera == null) return;
        
        float targetOrtho = zoomIn ? 3f : 5f;
        _mainCamera.orthographicSize = Mathf.Lerp(_mainCamera.orthographicSize, targetOrtho, Time.deltaTime * 5f);
    }
    
    /// <summary>
    /// 카메라 쉐이크
    /// </summary>
    /// <param name="intensity">세기</param>
    /// <param name="duration">지속 시간</param>
    public void ShakeCamera(float intensity, float duration)
    {
        if (_mainCamera == null) return;
        
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        Vector3 originalPos = _mainCamera.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            _mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            intensity *= 0.95f; // 점차 감소
            yield return null;
        }
        
        _mainCamera.transform.localPosition = originalPos;
    }
    
    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 모든 애니메이션 리셋
    /// </summary>
    public void ResetAnimations()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.SetBool("IsMoving", false);
            _playerAnimator.SetBool("IsAttacking", false);
            _playerAnimator.SetTrigger("Idle");
        }
        
        if (_monsterAnimator != null)
        {
            _monsterAnimator.SetBool("IsMoving", false);
            _monsterAnimator.SetBool("IsAttacking", false);
        }
    }
    
    /// <summary>
    /// 몬스터 외형 업데이트 (등급에 따라)
    /// </summary>
    /// <param name="grade">몬스터 등급</param>
    public void UpdateMonsterAppearance(int grade)
    {
        // 등급에 따라 몬스터 색상/크기 변경
        if (_monsterSpriteRenderer != null)
        {
            Color gradeColor = GetGradeColor(grade);
            _monsterSpriteRenderer.color = gradeColor;
        }
    }
    
    private Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return Color.white; // 일반
            case 1: return new Color(0.3f, 0.7f, 1f); // 고급 (파랑)
            case 2: return new Color(0.6f, 0.3f, 1f); // 희귀 (보라)
            case 3: return new Color(1f, 0.8f, 0.2f); // 영웅 (금색)
            case 4: return new Color(1f, 0.4f, 0.1f); // 전설 (주황)
            default: return Color.white;
        }
    }
}
