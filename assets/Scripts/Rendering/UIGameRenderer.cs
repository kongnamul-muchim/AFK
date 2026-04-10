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
        // 의존성 주입
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
        _gameView = gameView;
        CreateRenderElements();
    }
    
    private void CreateRenderElements()
    {
        if (_gameView == null) return;
        
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
        
        // 플레이어 (왼쪽)
        _playerElement = new VisualElement();
        _playerElement.name = "PlayerSprite";
        _playerElement.style.position = Position.Absolute;
        _playerElement.style.left = Length.Percent(20);
        _playerElement.style.bottom = Length.Percent(20);
        _playerElement.style.width = 100;
        _playerElement.style.height = 100;
        _playerElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        _gameView.Add(_playerElement);
        
        // 몬스터 (오른쪽)
        _monsterElement = new VisualElement();
        _monsterElement.name = "MonsterSprite";
        _monsterElement.style.position = Position.Absolute;
        _monsterElement.style.right = Length.Percent(20);
        _monsterElement.style.bottom = Length.Percent(20);
        _monsterElement.style.width = 100;
        _monsterElement.style.height = 100;
        _monsterElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        _gameView.Add(_monsterElement);
        
        // 초기 배경 설정
        SetBackground(BG_NORMAL_PATH);
    }
    
    private void OnPhaseChanged()
    {
        if (_gameState == null) return;
        
        CombatPhase phase = CombatSystem.Instance.CurrentPhase;
        
        switch (phase)
        {
            case CombatPhase.ENCOUNTERING:
                // 몬스터 등장
                UpdateMonsterSprite();
                break;
            case CombatPhase.COMBAT:
                // 전투 중
                break;
            case CombatPhase.VICTORY:
                // 승리 - 몬스터 숨기기
                if (_monsterElement != null)
                    _monsterElement.style.display = DisplayStyle.None;
                break;
            case CombatPhase.MOVING:
                // 다음 스테이지 이동
                if (_monsterElement != null)
                    _monsterElement.style.display = DisplayStyle.Flex;
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
            _backgroundElement.style.backgroundImage = texture;
        }
    }
    
    private void UpdateMonsterSprite()
    {
        if (_monsterElement == null) return;
        
        Texture2D texture = LoadTexture(MONSTER_SPRITE_PATH);
        if (texture != null)
        {
            _monsterElement.style.backgroundImage = texture;
            _monsterElement.style.display = DisplayStyle.Flex;
        }
    }
    
    private Texture2D LoadTexture(string path)
    {
        // Resources 폴더에서 로드
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning($"[UIGameRenderer] 텍스처를 찾을 수 없음: {path}");
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
        _playerElement.style.left = Length.Percent(25);
        
        // 0.2초 후 복귀
        Invoke(nameof(ResetPlayerPosition), 0.2f);
    }
    
    private void ResetPlayerPosition()
    {
        if (_playerElement != null)
            _playerElement.style.left = Length.Percent(20);
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
