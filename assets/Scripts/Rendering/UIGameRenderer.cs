using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 게임 뷰 렌더러
/// Web 버전과 동일하게 프레임 기반 애니메이션 지원
/// </summary>
public class UIGameRenderer : MonoBehaviour
{
    private static UIGameRenderer _instance;
    
    public static UIGameRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIGameRenderer");
                _instance = go.AddComponent<UIGameRenderer>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    private VisualElement _gameView;
    private VisualElement _playerElement;
    private VisualElement _monsterElement;
    private VisualElement _backgroundElement;
    
    // 몬스터 HP 바
    private VisualElement _monsterHPBarContainer;
    private VisualElement _monsterHPBarFill;
    private Label _monsterNameLabel;
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    
    // ========== 스프라이트 경로 (실제 파일명과 일치) ==========
    
    // 실제 파일명: player_spritesheet_0.png ~ player_spritesheet_7.png
    private const string PLAYER_SPRITE_PREFIX = "images/characters/player_spritesheet_";
    // 실제 파일명: slime_spritesheet_0.png ~ slime_spritesheet_7.png
    private const string MONSTER_SPRITE_PREFIX = "images/monsters/slime_spritesheet_";
    private const string BG_NORMAL_PATH = "images/backgrounds/background_normal";
    private const string BG_BOSS_PATH = "images/backgrounds/background_boss";
    
    // ========== 애니메이션 프레임 매핑 (Web 버전과 동일) ==========
    
    // Player: 0-1 Idle, 2-3 Attack, 4-5 Hit(미사용), 6-7 Dead
    private readonly int[] PLAYER_IDLE_FRAMES = { 0, 1 };
    private readonly int[] PLAYER_ATTACK_FRAMES = { 2, 3 };
    private readonly int[] PLAYER_DEAD_FRAMES = { 6, 7 };
    
    // Monster: 0(등장전), 4/6(Idle), 5(돌진), 2→3(Dead), 1/7(미사용)
    private readonly int[] MONSTER_APPEARING_FRAMES = { 0 };
    private readonly int[] MONSTER_CHARGING_FRAMES = { 5 };
    private readonly int[] MONSTER_IDLE_FRAMES = { 4, 6 };
    private readonly int[] MONSTER_DEAD_FRAMES = { 2, 3 };
    
    // ========== 애니메이션 상태 ==========
    
    /// <summary>플레이어 애니메이션 상태</summary>
    private PlayerAnimState _playerAnimState = PlayerAnimState.idle;
    
    /// <summary>몬스터 애니메이션 상태</summary>
    private MonsterAnimState _monsterAnimState = MonsterAnimState.appearing;
    
    /// <summary>플레이어 프레임 인덱스</summary>
    private int _playerFrameIndex = 0;
    
    /// <summary>몬스터 프레임 인덱스</summary>
    private int _monsterFrameIndex = 0;
    
    /// <summary>프레임 타이머 (ms)</summary>
    private float _frameTimer = 0f;
    
    /// <summary>프레임 간격 (ms)</summary>
    private const float FRAME_INTERVAL = 150f;
    
    /// <summary>Dead 애니메이션 완료 플래그</summary>
    private bool _playerDeadAnimComplete = false;
    private bool _monsterDeadAnimComplete = false;
    
    // ========== 데미지 텍스트 ==========
    
    private class DamageText
    {
        public float x;
        public float y;
        public float startY;
        public string text;
        public bool isCrit;
        public float createdAt;
        public float duration = 1000f;
    }
    
    private System.Collections.Generic.List<DamageText> _damageTexts = new System.Collections.Generic.List<DamageText>();
    
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
    }
    
    private void OnEnable()
    {
        // 의존성 주입 (ServiceLocator 초기화 확인)
        if (ServiceLocator.Instance == null)
        {
            Debug.LogWarning("[UIGameRenderer] ServiceLocator가 아직 초기화되지 않았습니다. Bootstrap을 기다립니다.");
            return;
        }
        
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.COMBAT_PHASE_CHANGED, OnPhaseChanged);
        _eventBus.On(GameEvents.GAME_LOADED, OnGameLoaded);
    }
    
    private void OnDisable()
    {
        if (_eventBus != null)
        {
            _eventBus.Off(GameEvents.COMBAT_PHASE_CHANGED, OnPhaseChanged);
            _eventBus.Off(GameEvents.GAME_LOADED, OnGameLoaded);
        }
    }
    
    private void Update()
    {
        // 애니메이션 프레임 업데이트
        _frameTimer += Time.deltaTime * 1000f; // ms로 변환
        if (_frameTimer >= FRAME_INTERVAL)
        {
            _frameTimer = 0f;
            UpdateFrameIndices();
        }
        
        // 스프라이트 업데이트
        UpdatePlayerSprite();
        UpdateMonsterSprite();
        
        // 몬스터 슬라이딩 애니메이션 (MOVING 페이즈에서만)
        UpdateMonsterSliding();
        
        // 몬스터 HP 바 업데이트
        UpdateMonsterHPBar();
        
        // 데미지 텍스트 업데이트
        UpdateDamageTexts();
    }
    
    // ========== 초기화 ==========
    
    /// <summary>
    /// 초기화 (UIManager에서 호출)
    /// </summary>
    public void Initialize(VisualElement gameView)
    {
        if (gameView == null)
        {
            Debug.LogError("[UIGameRenderer] gameView가 null입니다!");
            return;
        }
        
        _gameView = gameView;
        Debug.Log("[UIGameRenderer] 초기화 시작");
        CreateRenderElements();
    }
    
    private void CreateRenderElements()
    {
        if (_gameView == null)
        {
            Debug.LogError("[UIGameRenderer] _gameView가 null입니다!");
            return;
        }
        
        Debug.Log("[UIGameRenderer] 렌더 요소 생성 중...");
        
        // GameView의 크기를 픽셀 단위로 계산
        float viewWidth = _gameView.resolvedStyle.width;
        float viewHeight = _gameView.resolvedStyle.height;
        
        // NaN 또는 0인 경우 기본값 사용
        if (viewHeight <= 0 || float.IsNaN(viewHeight)) viewHeight = 600;
        if (viewWidth <= 0 || float.IsNaN(viewWidth)) viewWidth = 800;
        
        // 플레이어는 더 크게, 몬스터는 더 작게 (스프라이트 픽셀 수 차이 반영)
        float playerSpriteSize = viewHeight * 0.825f; // 플레이어: 82.5% (1.5배)
        float monsterSpriteSize = viewHeight * 0.35f; // 몬스터: 35%
        Debug.Log($"[UIGameRenderer] GameView 크기: {viewWidth}x{viewHeight}, 플레이어: {playerSpriteSize}, 몬스터: {monsterSpriteSize}");
        
        // 배경
        _backgroundElement = new VisualElement();
        _backgroundElement.name = "GameBackground";
        _backgroundElement.style.position = Position.Absolute;
        _backgroundElement.style.left = 0;
        _backgroundElement.style.top = 0;
        _backgroundElement.style.right = 0;
        _backgroundElement.style.bottom = 0;
        _backgroundElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        _gameView.Add(_backgroundElement);
        Debug.Log("[UIGameRenderer] 배경 요소 추가 완료");
        
        // 플레이어 (왼쪽) - 더 크게
        _playerElement = new VisualElement();
        _playerElement.name = "PlayerSprite";
        _playerElement.style.position = Position.Absolute;
        _playerElement.style.left = Length.Percent(12);
        _playerElement.style.bottom = Length.Percent(12);
        _playerElement.style.width = playerSpriteSize;
        _playerElement.style.height = playerSpriteSize;
        _playerElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        _gameView.Add(_playerElement);
        Debug.Log("[UIGameRenderer] 플레이어 요소 추가 완료");
        
        // 몬스터 (오른쪽) - 더 작게, 왼쪽을 바라보게 반전
        _monsterElement = new VisualElement();
        _monsterElement.name = "MonsterSprite";
        _monsterElement.style.position = Position.Absolute;
        _monsterElement.style.right = Length.Percent(12);
        _monsterElement.style.bottom = Length.Percent(12);
        _monsterElement.style.width = monsterSpriteSize;
        _monsterElement.style.height = monsterSpriteSize;
        _monsterElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        // 좌우 반전 (왼쪽을 바라보게) - scale(-1, 1, 1)
        _monsterElement.transform.scale = new Vector3(-1, 1, 1);
        _gameView.Add(_monsterElement);
        Debug.Log("[UIGameRenderer] 몬스터 요소 추가 완료");
        
        // 몬스터 HP 바 생성 (Web 버전과 동일)
        CreateMonsterHPBar();
        
        // 초기 텍스처 로드
        Debug.Log("[UIGameRenderer] 초기 텍스처 로드 시도...");
        LoadPlayerFrame(0);
        LoadMonsterFrame(0);
        
        // 초기 배경 설정
        SetBackground(BG_NORMAL_PATH);
        Debug.Log("[UIGameRenderer] 초기 배경 설정 완료");
    }
    
    /// <summary>
    /// 몬스터 HP 바 생성 (Web 버전의 HP 바 렌더링 대응)
    /// </summary>
    private void CreateMonsterHPBar()
    {
        // HP 바 컨테이너 (3.5배 확대)
        _monsterHPBarContainer = new VisualElement();
        _monsterHPBarContainer.name = "MonsterHPBar";
        _monsterHPBarContainer.style.position = Position.Absolute;
        _monsterHPBarContainer.style.right = Length.Percent(10);
        _monsterHPBarContainer.style.bottom = Length.Percent(40); // 몬스터보다 위
        _monsterHPBarContainer.style.width = 210;
        _monsterHPBarContainer.style.height = 28;
        _monsterHPBarContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // 어두운 배경
        _gameView.Add(_monsterHPBarContainer);
        
        // HP 바 채우기
        _monsterHPBarFill = new VisualElement();
        _monsterHPBarFill.name = "MonsterHPBarFill";
        _monsterHPBarFill.style.position = Position.Absolute;
        _monsterHPBarFill.style.left = 0;
        _monsterHPBarFill.style.top = 0;
        _monsterHPBarFill.style.width = Length.Percent(100);
        _monsterHPBarFill.style.height = Length.Percent(100);
        _monsterHPBarFill.style.backgroundColor = new Color(0.9f, 0.3f, 0.3f); // 빨간색
        _monsterHPBarContainer.Add(_monsterHPBarFill);
        
        // 몬스터 이름 레이블 (3.5배 확대)
        _monsterNameLabel = new Label();
        _monsterNameLabel.name = "MonsterName";
        _monsterNameLabel.style.position = Position.Absolute;
        _monsterNameLabel.style.right = Length.Percent(10);
        _monsterNameLabel.style.bottom = Length.Percent(43);
        _monsterNameLabel.style.width = 280;
        _monsterNameLabel.style.height = 56;
        _monsterNameLabel.style.color = Color.white;
        _monsterNameLabel.style.fontSize = 35;
        _monsterNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _monsterNameLabel.text = "";
        _gameView.Add(_monsterNameLabel);
        
        Debug.Log("[UIGameRenderer] 몬스터 HP 바 생성 완료");
    }
    
    /// <summary>
    /// 몬스터 HP 바 업데이트
    /// </summary>
    private void UpdateMonsterHPBar()
    {
        if (_gameState == null) return;
        
        var monster = _gameState.CombatPhase.monsterState;
        
        // HP 바 업데이트
        if (_monsterHPBarFill != null && monster.maxHP > 0)
        {
            float hpPercent = Mathf.Clamp01(monster.currentHP / monster.maxHP);
            _monsterHPBarFill.style.width = Length.Percent(hpPercent * 100);
        }
        
        // 이름 업데이트
        if (_monsterNameLabel != null)
        {
            _monsterNameLabel.text = monster.name;
        }
    }
    
    // ========== 프레임 인덱스 업데이트 (Web 버전의 updateFrameIndices) ==========
    
    private void UpdateFrameIndices()
    {
        // 플레이어 프레임
        int[] playerFrameSet = GetPlayerFrameSet();
        
        // Dead 애니메이션은 한 번만 재생 (마지막 프레임에서 멈춤)
        if (_playerAnimState == PlayerAnimState.dead)
        {
            if (_playerFrameIndex < playerFrameSet.Length - 1)
            {
                _playerFrameIndex++;
            }
            _playerDeadAnimComplete = _playerFrameIndex >= playerFrameSet.Length - 1;
        }
        else
        {
            // 다른 상태는 반복 (8바퀴)
            _playerFrameIndex = (_playerFrameIndex + 1) % (playerFrameSet.Length * 8);
        }
        
        // 몬스터 프레임
        int[] monsterFrameSet = GetMonsterFrameSet();
        
        // Dead 애니메이션도 한 번만
        if (_monsterAnimState == MonsterAnimState.dead)
        {
            if (_monsterFrameIndex < monsterFrameSet.Length - 1)
            {
                _monsterFrameIndex++;
            }
            _monsterDeadAnimComplete = _monsterFrameIndex >= monsterFrameSet.Length - 1;
        }
        else
        {
            _monsterFrameIndex = (_monsterFrameIndex + 1) % (monsterFrameSet.Length * 8);
        }
    }
    
    // ========== 프레임 세트 반환 ==========
    
    private int[] GetPlayerFrameSet()
    {
        switch (_playerAnimState)
        {
            case PlayerAnimState.attacking:
                return PLAYER_ATTACK_FRAMES;
            case PlayerAnimState.dead:
                return PLAYER_DEAD_FRAMES;
            case PlayerAnimState.idle:
            case PlayerAnimState.hit:
            default:
                return PLAYER_IDLE_FRAMES;
        }
    }
    
    private int[] GetMonsterFrameSet()
    {
        switch (_monsterAnimState)
        {
            case MonsterAnimState.appearing:
                return MONSTER_APPEARING_FRAMES;
            case MonsterAnimState.charging:
                return MONSTER_CHARGING_FRAMES;
            case MonsterAnimState.dead:
                return MONSTER_DEAD_FRAMES;
            case MonsterAnimState.idle:
            case MonsterAnimState.hit:
            default:
                return MONSTER_IDLE_FRAMES;
        }
    }
    
    // ========== 현재 스프라이트 경로 반환 ==========
    
    /// <summary>
    /// 현재 플레이어 스프라이트 경로 반환 (Web 버전의 getPlayerFrameKey)
    /// </summary>
    private string GetPlayerSpritePath()
    {
        int[] frameSet = GetPlayerFrameSet();
        
        // Dead 애니메이션: 마지막 프레임에서 멈춤
        if (_playerAnimState == PlayerAnimState.dead)
        {
            int deadIndex = Mathf.Min(_playerFrameIndex, frameSet.Length - 1);
            return PLAYER_SPRITE_PREFIX + frameSet[deadIndex];
        }
        
        // Attack 애니메이션: CombatSystem의 attackCurrentFrame 사용
        if (_playerAnimState == PlayerAnimState.attacking)
        {
            int currentFrame = CombatSystem.Instance != null ? CombatSystem.Instance.attackCurrentFrame : 0;
            return PLAYER_SPRITE_PREFIX + frameSet[Mathf.Min(currentFrame, frameSet.Length - 1)];
        }
        
        // 기타 상태 (idle, hit 등)
        // MOVING/VICTORY 페이즈에서는 4프레임마다 변경, 아니면 8프레임마다
        CombatPhase phase = CombatSystem.Instance != null ? CombatSystem.Instance.CurrentPhase : CombatPhase.IDLE;
        int frameSkip = (phase == CombatPhase.MOVING || phase == CombatPhase.VICTORY) ? 4 : 8;
        int index = (_playerFrameIndex / frameSkip) % frameSet.Length;
        
        return PLAYER_SPRITE_PREFIX + frameSet[index];
    }
    
    /// <summary>
    /// 현재 몬스터 스프라이트 경로 반환 (Web 버전의 getMonsterFrameKey)
    /// </summary>
    private string GetMonsterSpritePath()
    {
        int[] frameSet = GetMonsterFrameSet();
        
        // appearing은 항상 0번
        if (_monsterAnimState == MonsterAnimState.appearing)
        {
            return MONSTER_SPRITE_PREFIX + "0";
        }
        
        // charging은 항상 5번
        if (_monsterAnimState == MonsterAnimState.charging)
        {
            return MONSTER_SPRITE_PREFIX + "5";
        }
        
        // Dead 애니메이션: 2→3 순서, 3번에서 멈춤
        if (_monsterAnimState == MonsterAnimState.dead)
        {
            int deadProgress = (_monsterFrameIndex / 4) % frameSet.Length;
            return MONSTER_SPRITE_PREFIX + frameSet[Mathf.Min(deadProgress, frameSet.Length - 1)];
        }
        
        // idle은 4프레임마다 변경하며 반복
        int index = (_monsterFrameIndex / 4) % frameSet.Length;
        return MONSTER_SPRITE_PREFIX + frameSet[index];
    }
    
    // ========== 스프라이트 업데이트 ==========
    
    private void UpdatePlayerSprite()
    {
        if (_playerElement == null) return;
        
        string spritePath = GetPlayerSpritePath();
        Texture2D texture = LoadTexture(spritePath);
        
        if (texture != null)
        {
            _playerElement.style.backgroundImage = texture;
        }
    }
    
    private void UpdateMonsterSprite()
    {
        if (_monsterElement == null) return;
        
        // Dead 상태이고 애니메이션이 완료되면 숨김
        if (_monsterAnimState == MonsterAnimState.dead && _monsterDeadAnimComplete)
        {
            _monsterElement.style.display = DisplayStyle.None;
            return;
        }
        
        string spritePath = GetMonsterSpritePath();
        Texture2D texture = LoadTexture(spritePath);
        
        if (texture != null)
        {
            _monsterElement.style.backgroundImage = texture;
            _monsterElement.style.display = DisplayStyle.Flex;
        }
    }
    
    /// <summary>
    /// 몬스터 슬라이딩 애니메이션 (Web 버전의 MOVING 페이즈 대응)
    /// moveProgress 0.5부터 1.0까지 몬스터가 오른쪽에서 왼쪽으로 슬라이드
    /// </summary>
    private void UpdateMonsterSliding()
    {
        if (_monsterElement == null) return;
        if (CombatSystem.Instance == null) return;
        
        CombatPhase phase = CombatSystem.Instance.CurrentPhase;
        if (phase != CombatPhase.MOVING) return;
        
        float moveProgress = CombatSystem.Instance.moveProgress;
        
        // moveProgress 0.5 미만: 몬스터가 화면 밖에 있음 (아직 스폰 안 됨)
        if (moveProgress < 0.5f)
        {
            // 몬스터를 화면 밖에 숨김 (우측) - right = -50이면 화면 오른쪽 바깥
            _monsterElement.style.right = Length.Percent(-50);
            _monsterElement.style.bottom = Length.Percent(12);
            return;
        }
        
        // moveProgress 0.5 ~ 1.0: 몬스터가 오른쪽에서 왼쪽으로 슬라이드
        float slideProgress = (moveProgress - 0.5f) / 0.5f; // 0 ~ 1
        
        // 시작: right = -50% (화면 밖 오른쪽)
        // 끝: right = 12% (최종 위치)
        float startRight = -50f;
        float endRight = 12f;
        float currentRight = Mathf.Lerp(startRight, endRight, slideProgress);
        
        _monsterElement.style.right = Length.Percent(currentRight);
        _monsterElement.style.bottom = Length.Percent(12);
    }
    
    // ========== 텍스처 로드 ==========
    
    private void LoadPlayerFrame(int frame)
    {
        if (_playerElement == null) return;
        
        string path = PLAYER_SPRITE_PREFIX + frame;
        Texture2D texture = LoadTexture(path);
        
        if (texture != null)
        {
            _playerElement.style.backgroundImage = texture;
            _playerElement.style.display = DisplayStyle.Flex;
            Debug.Log($"[UIGameRenderer] 플레이어 프레임 {frame} 로드 완료");
        }
    }
    
    private void LoadMonsterFrame(int frame)
    {
        if (_monsterElement == null) return;
        
        string path = MONSTER_SPRITE_PREFIX + frame;
        Texture2D texture = LoadTexture(path);
        
        if (texture != null)
        {
            _monsterElement.style.backgroundImage = texture;
            _monsterElement.style.display = DisplayStyle.Flex;
            Debug.Log($"[UIGameRenderer] 몬스터 프레임 {frame} 로드 완료");
        }
    }
    
    private Texture2D LoadTexture(string path)
    {
        // Resources 폴더에서 로드 (확장자 제외)
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            // 대체 경로 시도 (images/ 접두사 제거)
            string altPath = path.Replace("images/", "");
            texture = Resources.Load<Texture2D>(altPath);
        }
        return texture;
    }
    
    // ========== 페이즈 변경 처리 ==========
    
    private void OnPhaseChanged()
    {
        if (_gameState == null) return;
        
        CombatPhase phase = CombatSystem.Instance.CurrentPhase;
        Debug.Log($"[UIGameRenderer] 페이즈 변경: {phase}");
        
        switch (phase)
        {
            case CombatPhase.IDLE:
                _playerAnimState = PlayerAnimState.idle;
                _monsterAnimState = MonsterAnimState.appearing;
                _playerDeadAnimComplete = false;
                _monsterDeadAnimComplete = false;
                // 몬스터 숨김 (다음 MOVING에서 다시 나타남)
                if (_monsterElement != null) _monsterElement.style.display = DisplayStyle.None;
                // 몬스터 HP 바 숨김
                if (_monsterHPBarContainer != null) _monsterHPBarContainer.style.display = DisplayStyle.None;
                if (_monsterNameLabel != null) _monsterNameLabel.style.display = DisplayStyle.None;
                break;
                
            case CombatPhase.MOVING:
                // 이동 중 - 플레이어 idle 애니메이션
                _playerAnimState = PlayerAnimState.idle;
                _monsterAnimState = MonsterAnimState.appearing;
                _playerDeadAnimComplete = false;
                _monsterDeadAnimComplete = false;
                // 몬스터 HP 바 숨김
                if (_monsterHPBarContainer != null) _monsterHPBarContainer.style.display = DisplayStyle.None;
                if (_monsterNameLabel != null) _monsterNameLabel.style.display = DisplayStyle.None;
                // 몬스터 요소 표시 - 먼저 위치를 숨긴 위치로 설정한 후 표시
                if (_monsterElement != null)
                {
                    _monsterElement.style.right = Length.Percent(-50); // 화면 오른쪽 바깥
                    _monsterElement.style.bottom = Length.Percent(12);
                    _monsterElement.style.display = DisplayStyle.Flex;
                }
                Debug.Log("[UIGameRenderer] MOVING - 플레이어/몬스터 표시");
                break;
                
            case CombatPhase.ENCOUNTERING:
                // 몬스터 등장 - 최종 위치로 이동
                _playerAnimState = PlayerAnimState.idle;
                _monsterAnimState = MonsterAnimState.appearing;
                if (_playerElement != null) _playerElement.style.display = DisplayStyle.Flex;
                if (_monsterElement != null)
                {
                    _monsterElement.style.display = DisplayStyle.Flex;
                    _monsterElement.style.right = Length.Percent(12); // 최종 위치
                    _monsterElement.style.bottom = Length.Percent(12);
                }
                Debug.Log("[UIGameRenderer] ENCOUNTERING - 몬스터 등장");
                break;
                
            case CombatPhase.COMBAT:
                // 전투 시작 - 몬스터 charging → idle로 전환
                _playerAnimState = PlayerAnimState.idle;
                _monsterAnimState = MonsterAnimState.idle;
                if (_monsterElement != null)
                {
                    _monsterElement.style.display = DisplayStyle.Flex;
                    _monsterElement.style.right = Length.Percent(12); // 최종 위치
                    _monsterElement.style.bottom = Length.Percent(12);
                }
                if (_playerElement != null) _playerElement.style.display = DisplayStyle.Flex;
                Debug.Log("[UIGameRenderer] COMBAT - 전투 시작");
                // 몬스터 HP 바 표시
                if (_monsterHPBarContainer != null) _monsterHPBarContainer.style.display = DisplayStyle.Flex;
                if (_monsterNameLabel != null) _monsterNameLabel.style.display = DisplayStyle.Flex;
                break;
                
            case CombatPhase.VICTORY:
                // 승리 - 몬스터 dead 애니메이션, HP 바 숨김
                _monsterAnimState = MonsterAnimState.dead;
                _monsterDeadAnimComplete = false;
                // 플레이어는 idle 상태로 (멈춰있는 모습)
                _playerAnimState = PlayerAnimState.idle;
                if (_monsterHPBarContainer != null) _monsterHPBarContainer.style.display = DisplayStyle.None;
                if (_monsterNameLabel != null) _monsterNameLabel.style.display = DisplayStyle.None;
                Debug.Log("[UIGameRenderer] VICTORY - 몬스터 죽음, 플레이어 idle");
                break;
                
            case CombatPhase.DEFEATED:
                // 패배 - 플레이어 dead 애니메이션
                _playerAnimState = PlayerAnimState.dead;
                _playerDeadAnimComplete = false;
                Debug.Log("[UIGameRenderer] DEFEATED - 플레이어 죽음");
                break;
        }
    }
    
    private void OnGameLoaded()
    {
        UpdateBackground();
    }
    
    private void UpdateBackground()
    {
        if (_gameState == null || _backgroundElement == null) return;
        
        int stage = _gameState.Stage.currentStage;
        bool isBoss = stage % 10 == 0;
        
        SetBackground(isBoss ? BG_BOSS_PATH : BG_NORMAL_PATH);
    }
    
    private void SetBackground(string path)
    {
        if (_backgroundElement == null) return;
        
        Texture2D texture = LoadTexture(path);
        if (texture != null)
        {
            _backgroundElement.style.backgroundImage = texture;
        }
    }
    
    // ========== CombatSystem 콜백 (Web 버전의 트리거 대응) ==========
    
    /// <summary>
    /// 플레이어 공격 애니메이션 시작 (CombatSystem에서 호출)
    /// </summary>
    public void OnPlayerAttackStart()
    {
        _playerAnimState = PlayerAnimState.attacking;
    }
    
    /// <summary>
    /// 몬스터 피격 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterHit()
    {
        _monsterAnimState = MonsterAnimState.hit;
    }
    
    /// <summary>
    /// 몬스터 데미지 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterDamaged(float damage, bool isCrit)
    {
        ShowDamageText(damage, isCrit);
    }
    
    /// <summary>
    /// 몬스터 죽음 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterDead()
    {
        _monsterAnimState = MonsterAnimState.dead;
        _monsterDeadAnimComplete = false;
    }
    
    /// <summary>
    /// 몬스터 공격 애니메이션 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterAttack()
    {
        // 몬스터 공격 애니메이션 (현재는 상태만 변경)
    }
    
    /// <summary>
    /// 플레이어 피격 (CombatSystem에서 호출)
    /// </summary>
    public void OnPlayerHit()
    {
        _playerAnimState = PlayerAnimState.hit;
    }
    
    /// <summary>
    /// 몬스터 등장 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterAppearing()
    {
        _monsterAnimState = MonsterAnimState.appearing;
    }
    
    /// <summary>
    /// 몬스터 돌진 (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterCharging()
    {
        _monsterAnimState = MonsterAnimState.charging;
    }
    
    /// <summary>
    /// 몬스터 Idle (CombatSystem에서 호출)
    /// </summary>
    public void OnMonsterIdle()
    {
        _monsterAnimState = MonsterAnimState.idle;
    }
    
    /// <summary>
    /// 플레이어 죽음 (CombatSystem에서 호출)
    /// </summary>
    public void OnPlayerDead()
    {
        _playerAnimState = PlayerAnimState.dead;
    }
    
    // ========== 데미지 텍스트 (Web 버전과 동일) ==========
    
    private void ShowDamageText(float damage, bool isCrit)
    {
        if (_monsterElement == null) return;
        
        // TODO: UI Toolkit에서 데미지 텍스트 렌더링 (현재는 단순 로그)
        // Web 버전에서는 Canvas에 텍스트를 그리지만, UI Toolkit에서는 VisualElement 기반이므로 별도 처리 필요
        Debug.Log($"[UIGameRenderer] 데미지: {(isCrit ? "CRIT! " : "")}{Mathf.RoundToInt(damage)}");
    }
    
    private void UpdateDamageTexts()
    {
        float now = Time.time * 1000f;
        
        // 만료된 텍스트 제거
        _damageTexts.RemoveAll(dt => now - dt.createdAt >= dt.duration);
    }
}