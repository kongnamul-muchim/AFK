using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// UI 관리 시스템 - 로딩, HUD, 모달 관리 (SRP 원칙 준수를 위해 하나의 책임만 가짐)
/// 인벤토리/업그레이드/미션 UI는 별도 InventoryUI, UpgradeUI, MissionsUI로 분리
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    private UIDocument _uiDocument;
    private VisualElement _root;
    
    // DI를 위한 의존성 주입
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    // 로딩 화면 요소
    private VisualElement _loadingScreen;
    private Label _loadingPercent;
    private VisualElement _loadingBarFill;
    private Button _loadingRetry;
    
    // 게임 컨테이너
    private VisualElement _gameContainer;
    
    // HUD 요소
    private Label _playerLevel;
    private VisualElement _hpBarFill;
    private Label _hpText;
    private Label _stageText;
    private Button _autoRepeatBtn;
    private Label _goldText;
    private Button _statisticsBtn;
    private Button _settingsBtn;
    
    // HUD 추가 요소
    private VisualElement _expBarFill;
    private Label _expText;
    private Label _statPoints;
    
    // 메뉴 버튼
    private Button _inventoryBtn;
    private Button _upgradeBtn;
    private Button _dailyMissionsBtn;
    private Button _gemShopBtn;
    
    // 모달 UI
    private VisualElement _inventoryModal;
    private VisualElement _settingsModal;
    private VisualElement _upgradeModal;
    private VisualElement _dailyMissionsModal;
    private VisualElement _gemShopModal;
    private VisualElement _offlineRewardModal;
    private VisualElement _statisticsModal;
    private VisualElement _tutorialOverlay;
    
    // 설정 슬라이더
    private Slider _sfxVolumeSlider;
    private Slider _bgmVolumeSlider;
    private Label _sfxVolumeValue;
    private Label _bgmVolumeValue;
    
    // 서브 UI 컴포넌트
    private InventoryUIClass _inventoryUI;
    private UpgradeUIClass _upgradeUI;
    private MissionsUIClass _missionsUI;
    private GemShopUIClass _gemShopUI;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        InitializeUI();
    }
    
    private void OnEnable()
    {
        // 게임 로딩 완료 이벤트 구독
        EventBus.Instance.On(GameEvents.GAME_LOADED, OnGameLoaded);
    }
    
    private void OnDisable()
    {
        EventBus.Instance.Off(GameEvents.GAME_LOADED, OnGameLoaded);
        EventBus.Instance.Off(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
        EventBus.Instance.Off(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
    }
    
    /// <summary>
    /// 플레이어 스탯 변경 시 HP 바, 레벨, EXP 업데이트
    /// </summary>
    private void OnPlayerStatChanged()
    {
        if (_gameState == null) return;
        Debug.Log($"[UIManager] 스탯 업데이트! HP={_gameState.Player.currentHP}/{_gameState.GetTotalHealth()}, Lv={_gameState.Player.level}");
        UpdatePlayerLevel(_gameState.Player.level);
        UpdateHP(_gameState.Player.currentHP, _gameState.GetTotalHealth());
        UpdateEXP(_gameState.Player.experience, _gameState.GetExpToNextLevel());
    }
    
    /// <summary>
    /// 레벨업 시 호출 (알림, 이펙트 등)
    /// </summary>
    private void OnPlayerLevelUp()
    {
        if (_gameState == null) return;
        Debug.Log($"[UIManager] 레벨업! Lv.{_gameState.Player.level}");
        // 레벨업 알림 표시 (웹 버전과 동일하게)
        UpdatePlayerLevel(_gameState.Player.level);
    }
    
    private void InitializeUI()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument 컴포넌트가 없습니다!");
            return;
        }
        
        _root = _uiDocument.rootVisualElement;
        
        // DI 설정: ServiceLocator를 통한 의존성 주입
        InitializeDI();
        
        BindUIElements();
        SetupEvents();
        SetupSettingsSliders();
        
        // 서브 UI 컴포넌트 초기화
        InitializeSubUIComponents();
        
        // 초기 상태: 로딩 화면 표시
        ShowLoadingScreen();
        
        // GameState가 이미 초기화되었다면 게임 화면으로 전환
        if (GameState.Instance != null)
        {
            Invoke(nameof(TransitionToGame), 0.5f);
        }
        
        Debug.Log("UIManager 초기화 완료!");
    }
    
    /// <summary>
    /// DI 설정 (ServiceLocator 사용)
    /// </summary>
    private void InitializeDI()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
        _eventBus = serviceLocator.Get<IEventBus>();
        _logger = serviceLocator.Get<IGameLogger>();
        
        // 플레이어 스탯 변경 이벤트 구독 (DI 완료 후)
        EventBus.Instance.On(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
        EventBus.Instance.On(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
    }
    
    /// <summary>
    /// 서브 UI 컴포넌트 초기화
    /// </summary>
    private void InitializeSubUIComponents()
    {
        Debug.Log("InitializeSubUIComponents 시작");
        
        // InventoryUI 초기화
        var inventoryUIGo = new GameObject("InventoryUI");
        inventoryUIGo.transform.SetParent(transform);
        _inventoryUI = inventoryUIGo.AddComponent<InventoryUIClass>();
        Debug.Log($"InventoryUI 생성됨: {_inventoryUI != null}");
        _inventoryUI.Initialize(_root);
        
        // UpgradeUI 초기화
        var upgradeUIGo = new GameObject("UpgradeUI");
        upgradeUIGo.transform.SetParent(transform);
        _upgradeUI = upgradeUIGo.AddComponent<UpgradeUIClass>();
        Debug.Log($"UpgradeUI 생성됨: {_upgradeUI != null}");
        _upgradeUI.Initialize(_root);
        
        // MissionsUI 초기화
        var missionsUIGo = new GameObject("MissionsUI");
        missionsUIGo.transform.SetParent(transform);
        _missionsUI = missionsUIGo.AddComponent<MissionsUIClass>();
        Debug.Log($"MissionsUI 생성됨: {_missionsUI != null}");
        _missionsUI.Initialize(_root);
        
        // GemShopUI 초기화
        var gemShopUIGo = new GameObject("GemShopUI");
        gemShopUIGo.transform.SetParent(transform);
        _gemShopUI = gemShopUIGo.AddComponent<GemShopUIClass>();
        Debug.Log($"GemShopUI 생성됨: {_gemShopUI != null}");
        _gemShopUI.Initialize(_root);
        
        // ModalManager 초기화
        ModalManager.Instance?.Initialize(_root);
        
        // TooltipManager 초기화
        TooltipManager.Instance?.Initialize(_root);
        
        // CombatLogManager 초기화
        CombatLogManager.Instance?.Initialize(_root);
        
        // ItemDropEffectManager 초기화
        ItemDropEffectManager.Instance?.Initialize(_root, _eventBus);
        
        // UIGameRenderer 초기화
        var gameView = _root.Q<VisualElement>("GameView");
        UIGameRenderer.Instance?.Initialize(gameView);
        
        Debug.Log("InitializeSubUIComponents 완료");
    }
    
    private void BindUIElements()
    {
        // 로딩 화면 요소
        _loadingScreen = _root.Q<VisualElement>("LoadingScreen");
        _loadingPercent = _root.Q<Label>("LoadingPercent");
        _loadingBarFill = _root.Q<VisualElement>("LoadingBarFill");
        _loadingRetry = _root.Q<Button>("LoadingRetry");
        
        // 게임 컨테이너
        _gameContainer = _root.Q<VisualElement>("GameContainer");
        
        // HUD 요소
        _playerLevel = _root.Q<Label>("PlayerLevel");
        _hpBarFill = _root.Q<VisualElement>("HPBarFill");
        _hpText = _root.Q<Label>("HPText");
        _stageText = _root.Q<Label>("StageText");
        _autoRepeatBtn = _root.Q<Button>("AutoRepeatBtn");
        _goldText = _root.Q<Label>("GoldText");
        _statisticsBtn = _root.Q<Button>("StatisticsBtn");
        _settingsBtn = _root.Q<Button>("SettingsBtn");
        
        // HUD 추가 요소
        _expBarFill = _root.Q<VisualElement>("EXPBarFill");
        _expText = _root.Q<Label>("EXPText");
        _statPoints = _root.Q<Label>("StatPoints");
        
        // 메뉴 버튼
        _inventoryBtn = _root.Q<Button>("InventoryBtn");
        _upgradeBtn = _root.Q<Button>("UpgradeBtn");
        _dailyMissionsBtn = _root.Q<Button>("DailyMissionsBtn");
        _gemShopBtn = _root.Q<Button>("GemShopBtn");
        
        // 모달 UI
        _inventoryModal = _root.Q<VisualElement>("InventoryModal");
        _settingsModal = _root.Q<VisualElement>("SettingsModal");
        _upgradeModal = _root.Q<VisualElement>("UpgradeModal");
        _dailyMissionsModal = _root.Q<VisualElement>("DailyMissionsModal");
        _gemShopModal = _root.Q<VisualElement>("GemShopModal");
        _offlineRewardModal = _root.Q<VisualElement>("OfflineRewardModal");
        _statisticsModal = _root.Q<VisualElement>("StatisticsModal");
        _tutorialOverlay = _root.Q<VisualElement>("TutorialOverlay");
        
        // 설정 슬라이더
        _sfxVolumeSlider = _root.Q<Slider>("SFXVolumeSlider");
        _bgmVolumeSlider = _root.Q<Slider>("BGMVolumeSlider");
        _sfxVolumeValue = _root.Q<Label>("SFXVolumeValue");
        _bgmVolumeValue = _root.Q<Label>("BGMVolumeValue");
    }
    
    private void SetupEvents()
    {
        // 로딩 화면 버튼
        if (_loadingRetry != null)
            _loadingRetry.clicked += OnLoadingRetryClicked;
        
        // HUD 버튼
        if (_autoRepeatBtn != null)
            _autoRepeatBtn.clicked += OnAutoRepeatClicked;
        
        if (_statisticsBtn != null)
            _statisticsBtn.clicked += OnStatisticsClicked;
        
        if (_settingsBtn != null)
            _settingsBtn.clicked += OnSettingsClicked;
        
        // 메뉴 버튼 - 서브 UI 컴포넌트 연결
        if (_inventoryBtn != null)
            _inventoryBtn.clicked += OnInventoryClicked;
        
        if (_upgradeBtn != null)
            _upgradeBtn.clicked += OnUpgradeClicked;
        
        if (_dailyMissionsBtn != null)
            _dailyMissionsBtn.clicked += OnDailyMissionsClicked;
        
        if (_gemShopBtn != null)
            _gemShopBtn.clicked += OnGemShopClicked;
        
        // 모달 닫기 버튼 설정
        SetupModalCloseButtons();
    }
    
    private void SetupModalCloseButtons()
    {
        var closeInventoryBtn = _root.Q<Button>("CloseInventoryBtn");
        if (closeInventoryBtn != null)
            closeInventoryBtn.clicked += () => HideModal(_inventoryModal);
        
        var closeSettingsBtn = _root.Q<Button>("CloseSettingsBtn");
        if (closeSettingsBtn != null)
            closeSettingsBtn.clicked += () => HideModal(_settingsModal);
        
        var closeUpgradeBtn = _root.Q<Button>("CloseUpgradeBtn");
        if (closeUpgradeBtn != null)
            closeUpgradeBtn.clicked += () => HideModal(_upgradeModal);
        
        var closeMissionsBtn = _root.Q<Button>("CloseMissionsBtn");
        if (closeMissionsBtn != null)
            closeMissionsBtn.clicked += () => HideModal(_dailyMissionsModal);
        
        var closeGemShopBtn = _root.Q<Button>("CloseGemShopBtn");
        if (closeGemShopBtn != null)
            closeGemShopBtn.clicked += () => HideModal(_gemShopModal);
        
        var closeOfflineBtn = _root.Q<Button>("CloseOfflineBtn");
        if (closeOfflineBtn != null)
            closeOfflineBtn.clicked += () => HideModal(_offlineRewardModal);
        
        var closeStatisticsBtn = _root.Q<Button>("CloseStatisticsBtn");
        if (closeStatisticsBtn != null)
            closeStatisticsBtn.clicked += () => HideModal(_statisticsModal);
    }
    
    private void OnLoadingProgress()
    {
        // 로딩 진행 표시 - ProgressBar 업데이트
        // 실제 로딩 진행률은 별도 시스템에서 처리
    }
    
    private void OnGameLoaded()
    {
        // 게임 로딩 완료 - 게임 화면으로 전환
        Debug.Log("게임 로딩 완료 - 게임 화면으로 전환");
        
        // 튜토리얼 여부 확인 (레벨 1, 경험치 0)
        Invoke(nameof(TransitionToGame), 0.5f);
    }
    
    private void TransitionToGame()
    {
        HideLoadingScreen();
        UpdateAllUI();
        Debug.Log("게임 화면으로 전환 완료");
    }
    
    // 전체 UI 업데이트
    private void UpdateAllUI()
    {
        if (_gameState == null) return;
        
        // 플레이어 정보 업데이트
        UpdatePlayerLevel(_gameState.Player.level);
        UpdateHP(_gameState.Player.currentHP, _gameState.Player.maxHP);
        UpdateGold((int)_gameState.Player.gold);
        UpdateEXP(_gameState.Player.experience, _gameState.GetExpToNextLevel());
        
        // 스테이지 정보 업데이트
        UpdateStage(_gameState.Stage.currentStage);
        
        Debug.Log("UI 업데이트 완료");
    }
    
    private void SetupSettingsSliders()
    {
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                if (_sfxVolumeValue != null)
                    _sfxVolumeValue.text = $"{evt.newValue:F0}%";
                // 실제 볼륨 조절은 AudioManager에서 처리
            });
        }
        
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                if (_bgmVolumeValue != null)
                    _bgmVolumeValue.text = $"{evt.newValue:F0}%";
                // 실제 BGM 볼륨 조절은 AudioManager에서 처리
            });
        }
    }
    
    // 로딩 화면 표시
    public void ShowLoadingScreen()
    {
        if (_loadingScreen != null)
            _loadingScreen.style.display = DisplayStyle.Flex;
        if (_gameContainer != null)
            _gameContainer.style.display = DisplayStyle.None;
    }
    
    public void HideLoadingScreen()
    {
        if (_loadingScreen != null)
            _loadingScreen.style.display = DisplayStyle.None;
        if (_gameContainer != null)
            _gameContainer.style.display = DisplayStyle.Flex;
    }
    
    public void UpdateLoadingProgress(float progress)
    {
        if (_loadingPercent != null)
            _loadingPercent.text = $"{progress:F0}%";
        if (_loadingBarFill != null)
            _loadingBarFill.style.width = Length.Percent(progress);
    }
    
    public void ShowLoadingRetry()
    {
        if (_loadingRetry != null)
            _loadingRetry.style.display = DisplayStyle.Flex;
    }
    
    // HUD 업데이트
    public void UpdatePlayerLevel(int level)
    {
        if (_playerLevel != null)
            _playerLevel.text = $"Lv.{level}";
    }
    
    public void UpdateHP(float current, float max)
    {
        if (_hpText != null)
            _hpText.text = $"{current:F0}/{max:F0}";
        if (_hpBarFill != null)
        {
            float percent = max > 0 ? current / max : 0;
            _hpBarFill.style.width = Length.Percent(percent * 100);
        }
    }
    
    public void UpdateStage(int stage)
    {
        if (_stageText != null)
            _stageText.text = $"Stage {stage}";
    }
    
    public void UpdateGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = gold.ToString("N0");
    }
    
    public void UpdateEXP(float current, float max)
    {
        if (_expText != null)
            _expText.text = $"Exp: {current:F0}/{max:F0}";
        if (_expBarFill != null)
        {
            float percent = max > 0 ? current / max : 0;
            _expBarFill.style.width = Length.Percent(percent * 100);
        }
    }
    
    public void UpdateStatPoints(int points)
    {
        if (_statPoints != null)
            _statPoints.text = $"SP: {points}";
    }
    
    // 모달 관리
    public void ShowModal(VisualElement modal)
    {
        HideAllModals();
        if (modal != null)
            modal.style.display = DisplayStyle.Flex;
    }
    
    public void HideModal(VisualElement modal)
    {
        if (modal != null)
            modal.style.display = DisplayStyle.None;
    }
    
    public void HideAllModals()
    {
        HideModal(_inventoryModal);
        HideModal(_settingsModal);
        HideModal(_upgradeModal);
        HideModal(_dailyMissionsModal);
        HideModal(_gemShopModal);
        HideModal(_offlineRewardModal);
        HideModal(_statisticsModal);
        HideModal(_tutorialOverlay);
    }
    
    // 튜토리얼 관리
    public void ShowTutorial()
    {
        if (_tutorialOverlay != null)
            _tutorialOverlay.style.display = DisplayStyle.Flex;
    }
    
    public void HideTutorial()
    {
        if (_tutorialOverlay != null)
            _tutorialOverlay.style.display = DisplayStyle.None;
    }
    
    public void UpdateTutorialMessage(string message)
    {
        var tutorialMessage = _root.Q<Label>("TutorialMessage");
        if (tutorialMessage != null)
            tutorialMessage.text = message;
    }
    
    // 오프라인 보상 표시
    public void ShowOfflineReward(float hours, int kills, int gold, int exp)
    {
        var offlineTime = _root.Q<Label>("OfflineTime");
        var offlineKills = _root.Q<Label>("OfflineKills");
        var offlineGold = _root.Q<Label>("OfflineGold");
        var offlineExp = _root.Q<Label>("OfflineExp");
        
        if (offlineTime != null)
            offlineTime.text = $"오프라인 시간: {hours:F1} 시간";
        if (offlineKills != null)
            offlineKills.text = $"처치한 몬스터: {kills}마리";
        if (offlineGold != null)
            offlineGold.text = $"획득한 골드: {gold:N0}";
        if (offlineExp != null)
            offlineExp.text = $"획득한 경험치: {exp:N0}";
        
        ShowModal(_offlineRewardModal);
    }
    
    // 이벤트 핸들러
    private void OnLoadingRetryClicked()
    {
        Debug.Log("로딩 재시작");
        // 로딩 재시작 로직
    }
    
    private void OnAutoRepeatClicked()
    {
        Debug.Log("자동 반복 모드 토글!");
        // 자동 반복 모드 토글 로직
    }
    
    private void OnStatisticsClicked()
    {
        Debug.Log("통계 표시!");
        ShowModal(_statisticsModal);
        UpdateStatisticsDisplay();
    }
    
    private void OnSettingsClicked()
    {
        Debug.Log("설정!");
        ShowModal(_settingsModal);
    }
    
    private void OnInventoryClicked()
    {
        Debug.Log($"인벤토리! _inventoryUI is null: {_inventoryUI == null}");
        if (_inventoryUI == null)
            Debug.LogError("InventoryUI is null!");
        _inventoryUI?.RefreshInventoryGrid(); // 서브 InventoryUI로 위임
        ShowModal(_inventoryModal);
    }
    
    private void OnUpgradeClicked()
    {
        Debug.Log($"업그레이드! _upgradeUI is null: {_upgradeUI == null}");
        if (_upgradeUI == null)
            Debug.LogError("UpgradeUI is null!");
        _upgradeUI?.RefreshUpgradeGrid(); // 서브 UpgradeUI로 위임
        ShowModal(_upgradeModal);
    }
    
    private void OnDailyMissionsClicked()
    {
        Debug.Log($"미션! _missionsUI is null: {_missionsUI == null}");
        if (_missionsUI == null)
            Debug.LogError("MissionsUI is null!");
        _missionsUI?.RefreshMissionsGrid(); // 서브 MissionsUI로 위임
        ShowModal(_dailyMissionsModal);
    }
    
    private void OnGemShopClicked()
    {
        Debug.Log("젬 상점!");
        ShowModal(_gemShopModal);
    }
    
    // 통계 정보 업데이트
    private void UpdateStatisticsDisplay()
    {
        // TODO: 실제 통계 데이터로 업데이트
        var statsLevelValue = _root.Q<Label>("StatsLevelValue");
        var statsAtkValue = _root.Q<Label>("StatsAtkValue");
        var statsDefValue = _root.Q<Label>("StatsDefValue");
        var statsHPValue = _root.Q<Label>("StatsHPValue");
        
        if (statsLevelValue != null) statsLevelValue.text = "1";
        if (statsAtkValue != null) statsAtkValue.text = "10";
        if (statsDefValue != null) statsDefValue.text = "5";
        if (statsHPValue != null) statsHPValue.text = "100";
    }
    
    // ========== 인벤토리 UI 관련 (Infinity Scroll) ==========
    
    // 인벤토리 아이템 목록
    private List<ItemData> _inventoryItemList = new List<ItemData>();
    
    /// <summary>
    /// 인벤토리 새로고침 (ListView 기반 Infinity Scroll)
    /// </summary>
    private void RefreshInventoryGrid()
    {
        if (_inventoryUI != null)
        {
            _inventoryUI.RefreshInventoryGrid();
        }
    }
    
    // ========== UI 패널 열기/닫기 ==========
    
    /// <summary>
    /// 인벤토리 패널 열기
    /// </summary>
    private void OpenInventoryPanel()
    {
        if (_inventoryModal != null)
        {
            _inventoryModal.style.display = DisplayStyle.Flex;
            _inventoryUI?.RefreshInventoryGrid();
        }
    }
    
    /// <summary>
    /// 인벤토리 패널 닫기
    /// </summary>
    private void CloseInventoryPanel()
    {
        if (_inventoryModal != null)
        {
            _inventoryModal.style.display = DisplayStyle.None;
        }
    }
    
    /// <summary>
    /// 업그레이드 패널 열기
    /// </summary>
    private void OpenUpgradePanel()
    {
        if (_upgradeModal != null)
        {
            _upgradeModal.style.display = DisplayStyle.Flex;
            _upgradeUI?.RefreshUpgradeGrid();
        }
    }
    
    /// <summary>
    /// 업그레이드 패널 닫기
    /// </summary>
    private void CloseUpgradePanel()
    {
        if (_upgradeModal != null)
        {
            _upgradeModal.style.display = DisplayStyle.None;
        }
    }
    
    /// <summary>
    /// 미션 패널 열기
    /// </summary>
    private void OpenMissionsPanel()
    {
        if (_dailyMissionsModal != null)
        {
            _dailyMissionsModal.style.display = DisplayStyle.Flex;
            _missionsUI?.RefreshMissionsGrid();
        }
    }
    
    /// <summary>
    /// 미션 패널 닫기
    /// </summary>
    private void CloseMissionsPanel()
    {
        if (_dailyMissionsModal != null)
        {
            _dailyMissionsModal.style.display = DisplayStyle.None;
        }
    }
    
    /// <summary>
    /// 설정 패널 열기
    /// </summary>
    private void OpenSettingsPanel()
    {
        if (_settingsModal != null)
        {
            _settingsModal.style.display = DisplayStyle.Flex;
        }
    }
    
    /// <summary>
    /// 설정 패널 닫기
    /// </summary>
    private void CloseSettingsPanel()
    {
        if (_settingsModal != null)
        {
            _settingsModal.style.display = DisplayStyle.None;
        }
    }
    
    // ========== 게임 로딩 완료 후 처리 ==========
    
    private void OnGameLoadedComplete()
    {
        _logger.Info("게임 로딩 완료 - UI 초기화");
        
        // 튜토리얼 초기화 (첫 플레이 여부)
        if (_gameState.Player.level == 1 && _gameState.Player.experience == 0)
        {
            // 튜토리얼 시스템 초기화
        }
        
        // 오프라인 보상 확인
        CheckOfflineRewards();
    }
    
    private void CheckOfflineRewards()
    {
        // 오프라인 보상 시스템 확인
        // 오프라인 시간이 있으면 오프라인 보상 모달 표시
    }
    
    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 아이템 아이콘 매핑 (아이콘은 실제 에셋으로 대체 필요)
    /// </summary>
    private string GetItemIcon(ItemData item)
    {
        if (item.id.Contains("sword") || item.id.Contains("weapon")) return "⚔️";
        if (item.id.Contains("armor")) return "🛡️";
        if (item.id.Contains("boots") || item.id.Contains("shoes")) return "👢";
        if (item.id.Contains("accessory") || item.id.Contains("ring")) return "💍";
        return "📦";
    }
    
    /// <summary>
    /// 아이템 이름 잘라내기
    /// </summary>
    private string TruncateItemName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return name.Length > 12 ? name.Substring(0, 12) + "..." : name;
    }
    
    /// <summary>
    /// 아이템 등급별 색상 반환
    /// </summary>
    private StyleColor GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return new StyleColor(Color.white);
            case 1: return new StyleColor(new Color(0.3f, 0.7f, 1f));
            case 2: return new StyleColor(new Color(0.6f, 0.3f, 1f));
            case 3: return new StyleColor(new Color(1f, 0.8f, 0.2f));
            case 4: return new StyleColor(new Color(1f, 0.4f, 0.1f));
            default: return new StyleColor(Color.white);
        }
    }
}
