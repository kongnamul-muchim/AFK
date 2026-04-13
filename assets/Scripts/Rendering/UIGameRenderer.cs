using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 게임 뷰 렌더러
/// SpriteRenderer 대신 VisualElement로 캐릭터/몬스터를 표시합니다.
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
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    
    // 이미지 경로 (Web 버전과 동일)
    private const string PLAYER_SPRITE_PATH = "images/characters/player_spritesheet_0";
    private const string MONSTER_SPRITE_PATH = "images/monsters/slime_spritesheet_0";
    private const string BG_NORMAL_PATH = "images/backgrounds/background_normal";
    private const string BG_BOSS_PATH = "images/backgrounds/background_boss";
    
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
        // 초기에는 resolvedStyle이 0일 수 있으므로 기본값 사용
        float viewWidth = _gameView.resolvedStyle.width;
        float viewHeight = _gameView.resolvedStyle.height;
        
        // NaN 또는 0인 경우 기본값 사용
        if (viewHeight <= 0 || float.IsNaN(viewHeight)) viewHeight = 600;
        if (viewWidth <= 0 || float.IsNaN(viewWidth)) viewWidth = 800;
        
        float spriteSize = viewHeight * 0.35f; // 화면 높이의 35%
        Debug.Log($"[UIGameRenderer] GameView 크기: {viewWidth}x{viewHeight}, 스프라이트 크기: {spriteSize}");
        
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
        
        // 플레이어 (왼쪽)
        _playerElement = new VisualElement();
        _playerElement.name = "PlayerSprite";
        _playerElement.style.position = Position.Absolute;
        _playerElement.style.left = Length.Percent(15);
        _playerElement.style.bottom = Length.Percent(15);
        _playerElement.style.width = spriteSize;
        _playerElement.style.height = spriteSize;
        _playerElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        _playerElement.style.backgroundColor = new Color(1, 0, 0, 0.3f); // 디버그: 빨간 반투명
        _gameView.Add(_playerElement);
        Debug.Log("[UIGameRenderer] 플레이어 요소 추가 완료");
        
        // 몬스터 (오른쪽)
        _monsterElement = new VisualElement();
        _monsterElement.name = "MonsterSprite";
        _monsterElement.style.position = Position.Absolute;
        _monsterElement.style.right = Length.Percent(15);
        _monsterElement.style.bottom = Length.Percent(15);
        _monsterElement.style.width = spriteSize;
        _monsterElement.style.height = spriteSize;
        _monsterElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        _monsterElement.style.backgroundColor = new Color(0, 1, 0, 0.3f); // 디버그: 초록 반투명
        _gameView.Add(_monsterElement);
        Debug.Log("[UIGameRenderer] 몬스터 요소 추가 완료");
        
        // 초기 텍스처 로드 (즉시 시도)
        Debug.Log("[UIGameRenderer] 초기 텍스처 로드 시도...");
        LoadAndSetPlayerSprite();
        LoadAndSetMonsterSprite();
        
        // 초기 배경 설정
        SetBackground(BG_NORMAL_PATH);
        Debug.Log("[UIGameRenderer] 초기 배경 설정 완료");
    }
    
    private void LoadAndSetPlayerSprite()
    {
        if (_playerElement == null) return;
        
        Texture2D texture = LoadTexture(PLAYER_SPRITE_PATH);
        if (texture != null)
        {
            _playerElement.style.backgroundImage = texture;
            _playerElement.style.display = DisplayStyle.Flex;
            Debug.Log($"[UIGameRenderer] 초기 플레이어 스프라이트 설정 완료");
        }
    }
    
    private void LoadAndSetMonsterSprite()
    {
        if (_monsterElement == null) return;
        
        Texture2D texture = LoadTexture(MONSTER_SPRITE_PATH);
        if (texture != null)
        {
            _monsterElement.style.backgroundImage = texture;
            Debug.Log($"[UIGameRenderer] 초기 몬스터 스프라이트 설정 완료");
        }
    }
    
    private void OnPhaseChanged()
    {
        if (_gameState == null) return;
        
        CombatPhase phase = CombatSystem.Instance.CurrentPhase;
        Debug.Log($"[UIGameRenderer] 페이즈 변경: {phase}");
        
        switch (phase)
        {
            case CombatPhase.ENCOUNTERING:
                // 몬스터 등장
                UpdateMonsterSprite();
                UpdatePlayerSprite();
                break;
            case CombatPhase.COMBAT:
                // 전투 중 - 몬스터 표시
                if (_monsterElement != null)
                    _monsterElement.style.display = DisplayStyle.Flex;
                break;
            case CombatPhase.VICTORY:
                // 승리 - 몬스터 숨기기
                Debug.Log("[UIGameRenderer] 몬스터 숨김 (승리)");
                if (_monsterElement != null)
                    _monsterElement.style.display = DisplayStyle.None;
                break;
            case CombatPhase.MOVING:
                // 다음 스테이지 이동 - 몬스터 다시 표시 (새 몬스터 등장 준비)
                Debug.Log("[UIGameRenderer] 몬스터 표시 (다음 스테이지)");
                if (_monsterElement != null)
                    _monsterElement.style.display = DisplayStyle.Flex;
                break;
            case CombatPhase.IDLE:
                // 대기 상태
                break;
        }
    }
    
    private void OnGameLoaded()
    {
        // 게임 로드 시 배경 업데이트
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
            Debug.Log($"[UIGameRenderer] 배경 텍스처 로드 성공: {texture.width}x{texture.height}");
            _backgroundElement.style.backgroundImage = texture;
            Debug.Log($"[UIGameRenderer] 배경 설정: {path}");
        }
        else
        {
            Debug.LogError($"[UIGameRenderer] 배경 텍스처를 로드할 수 없음: {path}");
        }
    }
    
    private void UpdateMonsterSprite()
    {
        if (_monsterElement == null) return;
        
        Texture2D texture = LoadTexture(MONSTER_SPRITE_PATH);
        if (texture != null)
        {
            Debug.Log($"[UIGameRenderer] 몬스터 텍스처 로드 성공: {texture.width}x{texture.height}");
            _monsterElement.style.backgroundImage = texture;
            _monsterElement.style.display = DisplayStyle.Flex;
            Debug.Log($"[UIGameRenderer] 몬스터 스프라이트 설정: {MONSTER_SPRITE_PATH}");
        }
        else
        {
            Debug.LogError($"[UIGameRenderer] 몬스터 텍스처를 로드할 수 없음: {MONSTER_SPRITE_PATH}");
        }
    }
    
    private void UpdatePlayerSprite()
    {
        if (_playerElement == null) return;
        
        Texture2D texture = LoadTexture(PLAYER_SPRITE_PATH);
        if (texture != null)
        {
            Debug.Log($"[UIGameRenderer] 플레이어 텍스처 로드 성공: {texture.width}x{texture.height}");
            _playerElement.style.backgroundImage = texture;
            _playerElement.style.display = DisplayStyle.Flex;
            Debug.Log($"[UIGameRenderer] 플레이어 스프라이트 설정: {PLAYER_SPRITE_PATH}");
        }
        else
        {
            Debug.LogError($"[UIGameRenderer] 플레이어 텍스처를 로드할 수 없음: {PLAYER_SPRITE_PATH}");
        }
    }
    
    private Texture2D LoadTexture(string path)
    {
        // Resources 폴더에서 로드 (확장자 제외)
        // path 예: "images/characters/player_spritesheet_0"
        Debug.Log($"[UIGameRenderer] 텍스처 로드 시도: {path}");
        Debug.Log($"[UIGameRenderer] Resources.Load 경로 확인: Assets/Resources/{path}.png");
        
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning($"[UIGameRenderer] 텍스처를 찾을 수 없음: {path}");
            Debug.LogWarning($"[UIGameRenderer] Assets/Resources/{path}.png 파일 존재 여부 확인");
            
            // 대체 경로 시도
            string altPath = path.Replace("images/", "");
            Debug.Log($"[UIGameRenderer] 대체 경로 시도: {altPath}");
            Texture2D altTexture = Resources.Load<Texture2D>(altPath);
            if (altTexture != null)
            {
                Debug.Log($"[UIGameRenderer] 대체 경로로 로드 성공: {altPath}");
                return altTexture;
            }
            else
            {
                Debug.LogError($"[UIGameRenderer] 대체 경로로도 실패: {altPath}");
            }
        }
        else
        {
            Debug.Log($"[UIGameRenderer] 텍스처 로드 성공: {texture.width}x{texture.height}");
        }
        return texture;
    }
    
    /// <summary>
    /// 플레이어 공격 애니메이션
    /// </summary>
    public void PlayPlayerAttack()
    {
        if (_playerElement == null) return;
        
        // 간단히 앞으로 이동하는 애니메이션
        var originalLeft = _playerElement.style.left;
        _playerElement.style.left = Length.Percent(20);
        
        // 0.2초 후 복귀
        Invoke(nameof(ResetPlayerPosition), 0.2f);
    }
    
    private void ResetPlayerPosition()
    {
        if (_playerElement != null)
            _playerElement.style.left = Length.Percent(15);
    }
    
    /// <summary>
    /// 몬스터 피격 애니메이션
    /// </summary>
    public void PlayMonsterHit()
    {
        if (_monsterElement == null) return;
        
        // 간단히 흔들리는 효과
        _monsterElement.style.opacity = 0.5f;
        Invoke(nameof(ResetMonsterOpacity), 0.1f);
    }
    
    private void ResetMonsterOpacity()
    {
        if (_monsterElement != null)
            _monsterElement.style.opacity = 1f;
    }
}
