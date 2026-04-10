using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    private UIDocument _uiDocument;
    private VisualElement _root;
    
    // 로딩 화면
    private VisualElement _loadingScreen;
    private Label _loadingPercent;
    private VisualElement _loadingBarFill;
    private Button _loadingRetry;
    
    // 게임 컨테이너
    private VisualElement _gameContainer;
    
    // HUD 상단
    private Label _playerLevel;
    private VisualElement _hpBarFill;
    private Label _hpText;
    private Label _stageText;
    private Button _autoRepeatBtn;
    private Label _goldText;
    private Button _statisticsBtn;
    private Button _settingsBtn;
    
    // HUD 하단
    private VisualElement _expBarFill;
    private Label _expText;
    private Label _statPoints;
    
    // 메뉴 버튼
    private Button _inventoryBtn;
    private Button _upgradeBtn;
    private Button _dailyMissionsBtn;
    private Button _gemShopBtn;
    
    // 모달들
    private VisualElement _inventoryModal;
    private VisualElement _settingsModal;
    private VisualElement _upgradeModal;
    private VisualElement _dailyMissionsModal;
    private VisualElement _gemShopModal;
    private VisualElement _offlineRewardModal;
    private VisualElement _statisticsModal;
    private VisualElement _tutorialOverlay;
    
    // 인벤토리 탭
    private Button _tabWeapon;
    private Button _tabArmor;
    private Button _tabAccessory;
    private Button _tabBoots;
    private VisualElement _inventoryItems;
    private string _currentInventoryTab = "weapon";
    
    // 설정 슬라이더
    private Slider _sfxVolumeSlider;
    private Slider _bgmVolumeSlider;
    private Label _sfxVolumeValue;
    private Label _bgmVolumeValue;
    
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
    }
    
    private void InitializeUI()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        _root = _uiDocument.rootVisualElement;
        BindUIElements();
        SetupEvents();
        SetupSettingsSliders();
        
        // 초기 상태: 로딩 화면 표시
        ShowLoadingScreen();
        
        // GameState가 이미 초기화되어 있으면 바로 게임 화면으로 전환
        if (GameState.Instance != null)
        {
            Invoke(nameof(TransitionToGame), 0.5f);
        }
        
        Debug.Log("UIManager 초기화 완료!");
    }
    
    private void BindUIElements()
    {
        // 로딩 화면
        _loadingScreen = _root.Q<VisualElement>("LoadingScreen");
        _loadingPercent = _root.Q<Label>("LoadingPercent");
        _loadingBarFill = _root.Q<VisualElement>("LoadingBarFill");
        _loadingRetry = _root.Q<Button>("LoadingRetry");
        
        // 게임 컨테이너
        _gameContainer = _root.Q<VisualElement>("GameContainer");
        
        // HUD 상단
        _playerLevel = _root.Q<Label>("PlayerLevel");
        _hpBarFill = _root.Q<VisualElement>("HPBarFill");
        _hpText = _root.Q<Label>("HPText");
        _stageText = _root.Q<Label>("StageText");
        _autoRepeatBtn = _root.Q<Button>("AutoRepeatBtn");
        _goldText = _root.Q<Label>("GoldText");
        _statisticsBtn = _root.Q<Button>("StatisticsBtn");
        _settingsBtn = _root.Q<Button>("SettingsBtn");
        
        // HUD 하단
        _expBarFill = _root.Q<VisualElement>("EXPBarFill");
        _expText = _root.Q<Label>("EXPText");
        _statPoints = _root.Q<Label>("StatPoints");
        
        // 메뉴 버튼
        _inventoryBtn = _root.Q<Button>("InventoryBtn");
        _upgradeBtn = _root.Q<Button>("UpgradeBtn");
        _dailyMissionsBtn = _root.Q<Button>("DailyMissionsBtn");
        _gemShopBtn = _root.Q<Button>("GemShopBtn");
        
        // 모달들
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
        
        // 인벤토리 탭
        _tabWeapon = _root.Q<Button>("TabWeapon");
        _tabArmor = _root.Q<Button>("TabArmor");
        _tabAccessory = _root.Q<Button>("TabAccessory");
        _tabBoots = _root.Q<Button>("TabBoots");
        _inventoryItems = _root.Q<VisualElement>("InventoryItems");
    }
    
    private void SetupEvents()
    {
        // 로딩 재시도
        if (_loadingRetry != null)
            _loadingRetry.clicked += OnLoadingRetryClicked;
        
        // HUD 버튼들
        if (_autoRepeatBtn != null)
            _autoRepeatBtn.clicked += OnAutoRepeatClicked;
        
        if (_statisticsBtn != null)
            _statisticsBtn.clicked += OnStatisticsClicked;
        
        if (_settingsBtn != null)
            _settingsBtn.clicked += OnSettingsClicked;
        
        // 메뉴 버튼들
        if (_inventoryBtn != null)
            _inventoryBtn.clicked += OnInventoryClicked;
        
        if (_upgradeBtn != null)
            _upgradeBtn.clicked += OnUpgradeClicked;
        
        if (_dailyMissionsBtn != null)
            _dailyMissionsBtn.clicked += OnDailyMissionsClicked;
        
        if (_gemShopBtn != null)
            _gemShopBtn.clicked += OnGemShopClicked;
        
        // 인벤토리 탭
        SetupInventoryTabs();
        
        // 모달 닫기 버튼들
        SetupModalCloseButtons();
    }
    
    private void SetupInventoryTabs()
    {
        if (_tabWeapon != null)
            _tabWeapon.clicked += () => OnInventoryTabClicked("weapon", _tabWeapon);
        
        if (_tabArmor != null)
            _tabArmor.clicked += () => OnInventoryTabClicked("armor", _tabArmor);
        
        if (_tabAccessory != null)
            _tabAccessory.clicked += () => OnInventoryTabClicked("accessory", _tabAccessory);
        
        if (_tabBoots != null)
            _tabBoots.clicked += () => OnInventoryTabClicked("boots", _tabBoots);
    }
    
    private void OnInventoryTabClicked(string tabType, Button clickedTab)
    {
        Debug.Log($"인벤토리 탭 변경: {tabType}");
        _currentInventoryTab = tabType;
        
        // 모든 탭 버튼 활성/비활성 처리
        ResetTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        
        // TODO: 선택한 탭에 맞는 아이템 그리드 업데이트
        // 현재는 탭 전환만 구현
    }
    
    private void ResetTabButtons()
    {
        if (_tabWeapon != null) _tabWeapon.RemoveFromClassList("active");
        if (_tabArmor != null) _tabArmor.RemoveFromClassList("active");
        if (_tabAccessory != null) _tabAccessory.RemoveFromClassList("active");
        if (_tabBoots != null) _tabBoots.RemoveFromClassList("active");
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
        // 로딩 진행 중 - ProgressBar 업데이트 등
        // 추후 로딩 시스템 구현 시 사용
    }
    
    private void OnGameLoaded()
    {
        // 게임 로딩 완료 - 로딩 화면 숨기고 게임 화면 표시
        Debug.Log("게임 로딩 완료 - 게임 화면으로 전환");
        
        // 잠시 딜레이 후 전환 (로딩 효과)
        Invoke(nameof(TransitionToGame), 0.5f);
    }
    
    private void TransitionToGame()
    {
        HideLoadingScreen();
        UpdateAllUI();
        Debug.Log("게임 화면으로 전환 완료");
    }
    
    // 모든 UI 업데이트
    private void UpdateAllUI()
    {
        if (GameState.Instance == null) return;
        
        var state = GameState.Instance;
        
        // 플레이어 정보
        UpdatePlayerLevel(state.player.level);
        UpdateHP(state.player.currentHP, state.player.maxHP);
        UpdateGold((int)state.player.gold);
        UpdateEXP(state.player.experience, state.GetExpToNextLevel());
        
        // 스테이지 정보
        UpdateStage(state.stage.currentStage);
        
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
                // 실제 사운드 볼륨 설정은 AudioManager에서 처리
            });
        }
        
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                if (_bgmVolumeValue != null)
                    _bgmVolumeValue.text = $"{evt.newValue:F0}%";
                // 실제 BGM 볼륨 설정은 AudioManager에서 처리
            });
        }
    }
    
    // 로딩 화면 제어
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
    
    // 모달 제어
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
    
    // 튜토리얼 제어
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
            offlineTime.text = $"접속하지 않은 시간: {hours:F1} 시간";
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
        Debug.Log("로딩 재시도!");
        // 로딩 재시작 로직
    }
    
    private void OnAutoRepeatClicked()
    {
        Debug.Log("자동반복 모드 토글!");
        // 자동반복 모드 토글 로직
    }
    
    private void OnStatisticsClicked()
    {
        Debug.Log("스탯 정보!");
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
        Debug.Log("인벤토리!");
        ShowModal(_inventoryModal);
    }
    
    private void OnUpgradeClicked()
    {
        Debug.Log("업그레이드!");
        ShowModal(_upgradeModal);
    }
    
    private void OnDailyMissionsClicked()
    {
        Debug.Log("미션!");
        ShowModal(_dailyMissionsModal);
    }
    
    private void OnGemShopClicked()
    {
        Debug.Log("보석 상점!");
        ShowModal(_gemShopModal);
    }
    
    // 통계 디스플레이 업데이트
    private void UpdateStatisticsDisplay()
    {
        // TODO: 실제 게임 데이터로 업데이트
        var statsLevelValue = _root.Q<Label>("StatsLevelValue");
        var statsAtkValue = _root.Q<Label>("StatsAtkValue");
        var statsDefValue = _root.Q<Label>("StatsDefValue");
        var statsHPValue = _root.Q<Label>("StatsHPValue");
        
        if (statsLevelValue != null) statsLevelValue.text = "1";
        if (statsAtkValue != null) statsAtkValue.text = "10";
        if (statsDefValue != null) statsDefValue.text = "5";
        if (statsHPValue != null) statsHPValue.text = "100";
    }
}