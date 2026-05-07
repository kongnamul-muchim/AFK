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
        EventBus.Instance.On(GameEvents.GAME_LOADED, OnGameLoaded);
        EventBus.Instance.On(GameEvents.STAGE_ENTERED, OnStageEntered);
        EventBus.Instance.On(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
        EventBus.Instance.On(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
        EventBus.Instance.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.On(GameEvents.ITEM_EQUIPPED, OnEquipmentChanged);
        EventBus.Instance.On(GameEvents.ITEM_UNEQUIPPED, OnEquipmentChanged);
    }
    
    private void OnDisable()
    {
        EventBus.Instance.Off(GameEvents.GAME_LOADED, OnGameLoaded);
        EventBus.Instance.Off(GameEvents.STAGE_ENTERED, OnStageEntered);
        EventBus.Instance.Off(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
        EventBus.Instance.Off(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
        EventBus.Instance.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.Off(GameEvents.ITEM_EQUIPPED, OnEquipmentChanged);
        EventBus.Instance.Off(GameEvents.ITEM_UNEQUIPPED, OnEquipmentChanged);
    }
    
    private void OnStageEntered()
    {
        if (_gameState == null) return;
        UpdateStage(_gameState.Stage.currentStage);
    }

    private void OnEquipmentChanged()
    {
        _inventoryUI?.RefreshInventoryGrid();
    }
    
    /// <summary>
    /// 플레이어 스탯 변경 시 HP 바, 레벨, EXP, SP, 골드 업데이트
    /// </summary>
    private void OnPlayerStatChanged()
    {
        if (_gameState == null) return;
        UpdatePlayerLevel(_gameState.Player.level);
        UpdateHP(_gameState.Player.currentHP, _gameState.GetTotalHealth());
        UpdateEXP(_gameState.Player.experience, _gameState.GetExpToNextLevel());
        UpdateStatPoints(_gameState.Player.statPoints);
        UpdateGold((int)_gameState.Player.gold);
        UpdateStatisticsDisplay();
    }
    
    /// <summary>
    /// 골드 변경 시 호출
    /// </summary>
    private void OnGoldChanged()
    {
        if (_gameState == null) return;
        UpdateGold((int)_gameState.Player.gold);
    }
    
    /// <summary>
    /// 레벨업 시 호출 (알림, 이펙트 등)
    /// </summary>
    private void OnPlayerLevelUp()
    {
        if (_gameState == null) return;
        // Debug.Log($"[UIManager] 레벨업! Lv.{_gameState.Player.level}");
        // 레벨업 알림 표시 (웹 버전과 동일하게)
        UpdatePlayerLevel(_gameState.Player.level);
    }
    
    private void InitializeUI()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            // Debug.LogError("UIDocument 컴포넌트가 없습니다!");
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
        
        // Debug.Log("UIManager 초기화 완료!");
    }
    
    /// <summary>
    /// DI 설정 (DIContainer 사용)
    /// </summary>
    private void InitializeDI()
    {
        if (Bootstrap.Container == null) return;

        _gameState = Bootstrap.Container.Resolve<IGameState>();
        _eventBus = Bootstrap.Container.Resolve<IEventBus>();
        _logger = Bootstrap.Container.Resolve<IGameLogger>();
        
        // 플레이어 스탯 변경 이벤트 구독 (DI 완료 후)
        EventBus.Instance.On(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
        EventBus.Instance.On(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
        EventBus.Instance.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
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
        // Debug.Log($"InventoryUI 생성됨: {_inventoryUI != null}");
        _inventoryUI.Initialize(_root);
        
        // UpgradeUI 초기화
        var upgradeUIGo = new GameObject("UpgradeUI");
        upgradeUIGo.transform.SetParent(transform);
        _upgradeUI = upgradeUIGo.AddComponent<UpgradeUIClass>();
        // Debug.Log($"UpgradeUI 생성됨: {_upgradeUI != null}");
        _upgradeUI.Initialize(_root);
        
        // MissionsUI 초기화
        var missionsUIGo = new GameObject("MissionsUI");
        missionsUIGo.transform.SetParent(transform);
        _missionsUI = missionsUIGo.AddComponent<MissionsUIClass>();
        // Debug.Log($"MissionsUI 생성됨: {_missionsUI != null}");
        _missionsUI.Initialize(_root);
        
        // GemShopUI 초기화
        var gemShopUIGo = new GameObject("GemShopUI");
        gemShopUIGo.transform.SetParent(transform);
        _gemShopUI = gemShopUIGo.AddComponent<GemShopUIClass>();
        // Debug.Log($"GemShopUI 생성됨: {_gemShopUI != null}");
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
            _inventoryBtn.clicked += () => { AudioManager.Instance?.PlayButtonClick(); OnInventoryClicked(); };
        if (_upgradeBtn != null)
            _upgradeBtn.clicked += () => { AudioManager.Instance?.PlayButtonClick(); OnUpgradeClicked(); };
        if (_dailyMissionsBtn != null)
            _dailyMissionsBtn.clicked += () => { AudioManager.Instance?.PlayButtonClick(); OnDailyMissionsClicked(); };
        if (_gemShopBtn != null)
            _gemShopBtn.clicked += () => { AudioManager.Instance?.PlayButtonClick(); OnGemShopClicked(); };
        
        // 모달 닫기 버튼 설정
        SetupModalCloseButtons();

        // 설정 슬라이더 이벤트
        SetupSettingsSliders();

        // 데이터 내보내기/가져오기/초기화 버튼
        SetupDataButtons();

        // 튜토리얼 "다음" 버튼
        var tutorialNextBtn = _root.Q<Button>("TutorialNext");
        if (tutorialNextBtn != null)
        {
            tutorialNextBtn.clicked += () =>
            {
                HideTutorial();
            };
        }
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
            closeOfflineBtn.clicked += () =>
            {
                OfflineRewardSystem.Instance.ClaimRewards();
                HideModal(_offlineRewardModal);
            };

        // ★★★ 확인 버튼 핸들러 (Web 버전과 동일)
        var claimRewardBtn = _root.Q<Button>("ClaimRewardBtn");
        if (claimRewardBtn != null)
            claimRewardBtn.clicked += () =>
            {
                OfflineRewardSystem.Instance.ClaimRewards();
                HideModal(_offlineRewardModal);
            };
        
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
        // Debug.Log("게임 로딩 완료 - 게임 화면으로 전환");
        
        // 튜토리얼 여부 확인 (레벨 1, 경험치 0)
        Invoke(nameof(TransitionToGame), 0.5f);
    }
    
    private void TransitionToGame()
    {
        HideLoadingScreen();
        UpdateAllUI();
        CheckOfflineRewards();
    }
    
    // 전체 UI 업데이트
    private void UpdateAllUI()
    {
        if (_gameState == null) return;
        
        UpdatePlayerLevel(_gameState.Player.level);
        UpdateHP(_gameState.Player.currentHP, _gameState.GetTotalHealth());
        UpdateGold((int)_gameState.Player.gold);
        UpdateEXP(_gameState.Player.experience, _gameState.GetExpToNextLevel());
        UpdateStage(_gameState.Stage.currentStage);

        if (_autoRepeatBtn != null)
        {
            if (_gameState.Stage.autoRepeat)
                _autoRepeatBtn.AddToClassList("active");
            else
                _autoRepeatBtn.RemoveFromClassList("active");
        }
        
        // Debug.Log("UI 업데이트 완료");
    }
    
    private void SetupSettingsSliders()
    {
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                if (_sfxVolumeValue != null)
                    _sfxVolumeValue.text = $"{evt.newValue:F0}%";
                AudioManager.Instance?.SetSFXVolume(evt.newValue / 100f);
            });
        }
        
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                if (_bgmVolumeValue != null)
                    _bgmVolumeValue.text = $"{evt.newValue:F0}%";
                AudioManager.Instance?.SetBGMVolume(evt.newValue / 100f);
            });
        }
    }

    /// <summary>
    /// 데이터 내보내기/가져오기/초기화 버튼 설정
    /// </summary>
    private void SetupDataButtons()
    {
        var exportBtn = _root.Q<Button>("ExportDataBtn");
        if (exportBtn != null)
        {
            exportBtn.clicked += OnExportDataClicked;
        }

        var importBtn = _root.Q<Button>("ImportDataBtn");
        if (importBtn != null)
        {
            importBtn.clicked += OnImportDataClicked;
        }

        var resetBtn = _root.Q<Button>("ResetDataBtn");
        if (resetBtn != null)
        {
            resetBtn.clicked += OnResetDataClicked;
        }
    }

    /// <summary>
    /// 데이터 내보내기 (JSON 파일로 저장)
    /// </summary>
    private void OnExportDataClicked()
    {
        // 현재 게임 상태 저장 후 내보내기
        SaveManager.Instance?.Save(GameState.Instance);
        string json = SaveManager.Instance?.ExportSave();
        if (string.IsNullOrEmpty(json))
        {
            _logger?.Warn("내보낼 데이터가 없습니다.");
            return;
        }

        // 클립보드에 JSON 복사
        GUIUtility.systemCopyBuffer = json;
        _logger?.Info("게임 데이터가 클립보드에 복사되었습니다.");

        // 파일 저장 다이얼로그 (Unity Editor 전용)
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanel(
            "데이터 내보내기",
            Application.dataPath,
            "afk_save_backup.json",
            "json"
        );
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            _logger?.Info($"데이터 내보내기 완료: {path}");
        }
#endif
    }

    /// <summary>
    /// 데이터 가져오기 (JSON 파일에서 로드)
    /// </summary>
    private void OnImportDataClicked()
    {
        string json = null;

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "데이터 가져오기",
            Application.dataPath,
            "json"
        );
        
        if (string.IsNullOrEmpty(path))
            return;

        if (!System.IO.File.Exists(path))
        {
            _logger?.Warn("파일을 찾을 수 없습니다.");
            return;
        }

        json = System.IO.File.ReadAllText(path);
#else
        // WebGL/모바일: 클립보드에서 가져오기
        json = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(json))
        {
            _logger?.Warn("클립보드에 데이터가 없습니다.");
            return;
        }
#endif

        if (!string.IsNullOrEmpty(json))
        {
            SaveManager.Instance?.ImportSave(json);
            _logger?.Info("데이터 가져오기 성공! 게임을 다시 시작합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// 데이터 초기화
    /// </summary>
    private void OnResetDataClicked()
    {
        _logger?.Warn("데이터 초기화 버튼 클릭됨");
        // 실제 초기화는 확인 후 진행
        // 간단한 구현: 저장 파일 삭제 후 게임 재시작
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            _logger?.Info("데이터가 초기화되었습니다. 게임을 다시 시작합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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
    public void ShowOfflineReward(float hours, int itemCount, int gold, int exp)
    {
        var offlineTime = _root.Q<Label>("OfflineTime");
        var offlineKills = _root.Q<Label>("OfflineKills");
        var offlineGold = _root.Q<Label>("OfflineGold");
        var offlineExp = _root.Q<Label>("OfflineExp");
        
        if (offlineTime != null)
            offlineTime.text = $"접속하지 않은 시간: {hours:F1} 시간";
        if (offlineKills != null)
            offlineKills.text = itemCount > 0 ? $"획득한 아이템: {itemCount}개" : "획득한 아이템: 없음";
        if (offlineGold != null)
            offlineGold.text = $"획득한 골드: {gold:N0}";
        if (offlineExp != null)
            offlineExp.text = $"획득한 경험치: {exp:N0}";
        
        Debug.Log($"[Offline] ShowOfflineReward called - hours={hours:F2}, gold={gold}, exp={exp}");
        ShowModal(_offlineRewardModal);
        if (_offlineRewardModal != null)
            _offlineRewardModal.BringToFront();
    }
    
    // 이벤트 핸들러
    private void OnLoadingRetryClicked()
    {
        // Debug.Log("로딩 재시작");
        // 로딩 재시작 로직
    }
    
    private void OnAutoRepeatClicked()
    {
        if (_gameState == null) return;
        
        // Web 버전과 동일: GameState 직접 토글 (CombatSystem에 의존하지 않음)
        var stage = _gameState.Stage;
        stage.autoRepeat = !stage.autoRepeat;
        _gameState.Stage = stage;
        
        // CombatSystem에 전파 (가능한 경우)
        if (CombatSystem.Instance != null)
            CombatSystem.Instance.SetAutoRepeatMode(stage.autoRepeat);
        
        if (_autoRepeatBtn != null)
        {
            if (stage.autoRepeat)
                _autoRepeatBtn.AddToClassList("active");
            else
                _autoRepeatBtn.RemoveFromClassList("active");
        }
            
        _logger.Info($"자동 반복 모드: {stage.autoRepeat}");
    }
    
    private void OnStatisticsClicked()
    {
        // Debug.Log("통계 표시!");
        ShowModal(_statisticsModal);
        UpdateStatisticsDisplay();
    }
    
    private void OnSettingsClicked()
    {
        // Debug.Log("설정!");
        ShowModal(_settingsModal);
    }
    
    private void OnInventoryClicked()
    {
        _inventoryUI?.RefreshInventoryGrid();
        ShowModal(_inventoryModal);
    }
    
    private void OnUpgradeClicked()
    {
        _upgradeUI?.RefreshUpgradeGrid();
        ShowModal(_upgradeModal);
    }
    
    private void OnDailyMissionsClicked()
    {
        _missionsUI?.RefreshMissionsGrid();
        ShowModal(_dailyMissionsModal);
    }
    
    private void OnGemShopClicked()
    {
        // Debug.Log("젬 상점!");
        ShowModal(_gemShopModal);
    }
    
    // 통계 정보 업데이트
    private void UpdateStatisticsDisplay()
    {
        var statsLevelValue = _root.Q<Label>("StatsLevelValue");
        var statsAtkValue = _root.Q<Label>("StatsAtkValue");
        var statsDefValue = _root.Q<Label>("StatsDefValue");
        var statsHPValue = _root.Q<Label>("StatsHPValue");
        var statsGoldValue = _root.Q<Label>("StatsGoldValue");
        var statsGemsValue = _root.Q<Label>("StatsGemsValue");
        var statsKillsValue = _root.Q<Label>("StatsKillsValue");
        
        if (statsLevelValue != null) statsLevelValue.text = _gameState.Player.level.ToString();
        if (statsAtkValue != null) statsAtkValue.text = _gameState.GetTotalAttack().ToString();
        if (statsDefValue != null) statsDefValue.text = _gameState.GetTotalDefense().ToString();
        if (statsHPValue != null) statsHPValue.text = _gameState.GetTotalHealth().ToString();
        if (statsGoldValue != null) statsGoldValue.text = _gameState.Player.gold.ToString("N0");
        if (statsGemsValue != null) statsGemsValue.text = _gameState.Player.gems.ToString();
        if (statsKillsValue != null) statsKillsValue.text = _gameState.Stats.totalKills.ToString("N0");
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
        var offlineSystem = OfflineRewardSystem.Instance;
        if (offlineSystem == null) { Debug.Log("[Offline] OfflineRewardSystem.Instance is null"); return; }

        float offlineSeconds = offlineSystem.CalculateOfflineTime();
        Debug.Log($"[Offline] offlineSeconds={offlineSeconds}");

        if (offlineSeconds <= 0) { Debug.Log("[Offline] offlineSeconds <= 0, returning"); return; }

        var rewards = offlineSystem.CalculateRewards(offlineSeconds);
        Debug.Log($"[Offline] rewards: gold={rewards.gold}, exp={rewards.experience}, items={rewards.items?.Length}");

        if (offlineSeconds < 60f)
        {
            Debug.Log($"[Offline] offline too short ({offlineSeconds}s < 60s), returning");
            return;
        }

        int itemCount = rewards.items != null ? rewards.items.Length : 0;
        ShowOfflineReward(
            offlineSeconds / 3600f,
            itemCount,
            (int)rewards.gold,
            (int)rewards.experience
        );
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
