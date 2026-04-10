using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

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
    
    // 업그레이드 탭
    private Button _upgradeTabGold;
    private Button _upgradeTabStat;
    private Button _upgradeTabGem;
    private Button _upgradeTabRebirth;
    private VisualElement _upgradeGrid;
    private string _currentUpgradeTab = "gold";
    
    // 미션 탭
    private Button _missionsTabDaily;
    private Button _missionsTabWeekly;
    private VisualElement _missionsGrid;
    private string _currentMissionsTab = "daily";
    
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
        
        // 업그레이드 탭
        _upgradeTabGold = _root.Q<Button>("UpgradeTabGold");
        _upgradeTabStat = _root.Q<Button>("UpgradeTabStat");
        _upgradeTabGem = _root.Q<Button>("UpgradeTabGem");
        _upgradeTabRebirth = _root.Q<Button>("UpgradeTabRebirth");
        _upgradeGrid = _root.Q<VisualElement>("UpgradeGrid");
        
        // 미션 탭
        _missionsTabDaily = _root.Q<Button>("MissionsTabDaily");
        _missionsTabWeekly = _root.Q<Button>("MissionsTabWeekly");
        _missionsGrid = _root.Q<VisualElement>("MissionsGrid");
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
        
        // 업그레이드 탭
        SetupUpgradeTabs();
        
        // 미션 탭
        SetupMissionsTabs();
        
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
        ResetInventoryTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        
        // 선택한 탭에 맞는 아이템 그리드 업데이트
        RefreshInventoryGrid();
    }
    
    private void ResetInventoryTabButtons()
    {
        if (_tabWeapon != null) _tabWeapon.RemoveFromClassList("active");
        if (_tabArmor != null) _tabArmor.RemoveFromClassList("active");
        if (_tabAccessory != null) _tabAccessory.RemoveFromClassList("active");
        if (_tabBoots != null) _tabBoots.RemoveFromClassList("active");
    }
    
    private void SetupUpgradeTabs()
    {
        if (_upgradeTabGold != null)
            _upgradeTabGold.clicked += () => OnUpgradeTabClicked("gold", _upgradeTabGold);
        
        if (_upgradeTabStat != null)
            _upgradeTabStat.clicked += () => OnUpgradeTabClicked("stat", _upgradeTabStat);
        
        if (_upgradeTabGem != null)
            _upgradeTabGem.clicked += () => OnUpgradeTabClicked("gem", _upgradeTabGem);
        
        if (_upgradeTabRebirth != null)
            _upgradeTabRebirth.clicked += () => OnUpgradeTabClicked("rebirth", _upgradeTabRebirth);
    }
    
    private void OnUpgradeTabClicked(string tabType, Button clickedTab)
    {
        Debug.Log($"업그레이드 탭 변경: {tabType}");
        _currentUpgradeTab = tabType;
        
        ResetUpgradeTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        
        // 선택한 탭에 맞는 업그레이드 항목 표시
        RefreshUpgradeGrid();
    }
    
    private void ResetUpgradeTabButtons()
    {
        if (_upgradeTabGold != null) _upgradeTabGold.RemoveFromClassList("active");
        if (_upgradeTabStat != null) _upgradeTabStat.RemoveFromClassList("active");
        if (_upgradeTabGem != null) _upgradeTabGem.RemoveFromClassList("active");
        if (_upgradeTabRebirth != null) _upgradeTabRebirth.RemoveFromClassList("active");
    }
    
    private void SetupMissionsTabs()
    {
        if (_missionsTabDaily != null)
            _missionsTabDaily.clicked += () => OnMissionsTabClicked("daily", _missionsTabDaily);
        
        if (_missionsTabWeekly != null)
            _missionsTabWeekly.clicked += () => OnMissionsTabClicked("weekly", _missionsTabWeekly);
    }
    
    private void OnMissionsTabClicked(string tabType, Button clickedTab)
    {
        Debug.Log($"미션 탭 변경: {tabType}");
        _currentMissionsTab = tabType;
        
        ResetMissionsTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        
        // 선택한 탭에 맞는 미션 목록 표시
        RefreshMissionsGrid();
    }
    
    private void ResetMissionsTabButtons()
    {
        if (_missionsTabDaily != null) _missionsTabDaily.RemoveFromClassList("active");
        if (_missionsTabWeekly != null) _missionsTabWeekly.RemoveFromClassList("active");
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
        RefreshInventoryGrid(); // 인벤토리 아이템 그리드 업데이트
        ShowModal(_inventoryModal);
    }
    
    private void OnUpgradeClicked()
    {
        Debug.Log("업그레이드!");
        RefreshUpgradeGrid(); // 업그레이드 그리드 업데이트
        ShowModal(_upgradeModal);
    }
    
    private void OnDailyMissionsClicked()
    {
        Debug.Log("미션!");
        RefreshMissionsGrid(); // 미션 그리드 업데이트
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
    
    // ========== 인벤토리 아이템 리스트 (Infinity Scroll) ==========
    
    // 인벤토리 아이템 소스 리스트
    private List<ItemData> _inventoryItemList = new List<ItemData>();
    
    /// <summary>
    /// 인벤토리 리스트 새로고침 (ListView 기반 Infinity Scroll)
    /// </summary>
    private void RefreshInventoryGrid()
    {
        var listView = _inventoryItems as ListView;
        if (listView == null) return;
        
        if (GameState.Instance == null) return;
        
        string filterType = _currentInventoryTab;
        
        // 필터링된 아이템 리스트 생성
        _inventoryItemList.Clear();
        foreach (var item in GameState.Instance.inventory.items)
        {
            if (!MatchesInventoryTab(item.id, filterType))
                continue;
            _inventoryItemList.Add(item);
        }
        
        // ListView 설정
        listView.itemsSource = _inventoryItemList;
        listView.makeItem = MakeInventoryItem;
        listView.bindItem = BindInventoryItem;
        listView.itemHeight = 50; // 아이템 높이 설정
        
        Debug.Log($"인벤토리 리스트 업데이트: {_inventoryItemList.Count}개 아이템 ({filterType})");
    }
    
    /// <summary>
    /// 인벤토리 아이템 VisualElement 생성 (makeItem)
    /// </summary>
    private VisualElement MakeInventoryItem()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.style.paddingLeft = 10;
        container.style.paddingRight = 10;
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 8;
        container.style.borderTopRightRadius = 8;
        container.style.borderBottomLeftRadius = 8;
        container.style.borderBottomRightRadius = 8;
        container.style.marginBottom = 4;
        
        // 아이템 아이콘
        var iconLabel = new Label();
        iconLabel.style.fontSize = 28;
        iconLabel.style.minWidth = 40;
        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(iconLabel);
        
        // 아이템 이름
        var nameLabel = new Label();
        nameLabel.style.fontSize = 20;
        nameLabel.style.flexGrow = 1;
        nameLabel.style.paddingLeft = 10;
        nameLabel.style.paddingRight = 10;
        container.Add(nameLabel);
        
        // 수량
        var qtyLabel = new Label();
        qtyLabel.style.fontSize = 16;
        qtyLabel.style.color = new StyleColor(Color.gray);
        qtyLabel.style.minWidth = 60;
        qtyLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        container.Add(qtyLabel);
        
        return container;
    }
    
    /// <summary>
    /// 인벤토리 아이템 바인딩 (bindItem)
    /// </summary>
    private void BindInventoryItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _inventoryItemList.Count) return;
        
        var item = _inventoryItemList[index];
        
        var iconLabel = element.Q<Label>("ItemIcon");
        var nameLabel = element.Q<Label>("ItemName");
        var qtyLabel = element.Q<Label>("ItemQuantity");
        
        // Q로 못 찾으면 자식 레이블들을 순서대로 매칭
        if (iconLabel == null && element.childCount >= 3)
        {
            iconLabel = element[0] as Label;
            nameLabel = element[1] as Label;
            qtyLabel = element[2] as Label;
        }
        
        if (iconLabel != null) iconLabel.text = GetItemIcon(item);
        if (nameLabel != null)
        {
            nameLabel.text = TruncateItemName(item.name);
            nameLabel.style.color = GetGradeColor(item.grade);
        }
        if (qtyLabel != null) qtyLabel.text = item.quantity > 1 ? $"x{item.quantity}" : "";
        
        // 클릭 이벤트
        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) OnInventoryItemClicked(item, evt);
            if (evt.button == 1) OnInventoryItemRightClick(item);
        });
    }
    
    /// <summary>
    /// 아이템 ID가 현재 탭에 해당하는지 확인
    /// </summary>
    private bool MatchesInventoryTab(string itemId, string tabType)
    {
        switch (tabType)
        {
            case "weapon":
                return itemId.Contains("sword") || itemId.Contains("weapon") || itemId.Contains("bow") || itemId.Contains("staff");
            case "armor":
                return itemId.Contains("armor") || itemId.Contains("helmet") || itemId.Contains("chest") || itemId.Contains("gloves");
            case "accessory":
                return itemId.Contains("ring") || itemId.Contains("necklace") || itemId.Contains("accessory");
            case "boots":
                return itemId.Contains("boots") || itemId.Contains("shoes");
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 인벤토리 아이템 버튼 생성
    /// </summary>
    private VisualElement CreateInventoryItemButton(ItemData item)
    {
        var container = new VisualElement();
        container.AddToClassList("inventory-item-container");
        container.style.flexDirection = FlexDirection.Column;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        container.style.minWidth = 80;
        container.style.minHeight = 80;
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;
        container.style.paddingTop = 5;
        container.style.paddingBottom = 5;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f)); // --color-bg-tertiary
        container.style.borderTopLeftRadius = 8;
        container.style.borderTopRightRadius = 8;
        container.style.borderBottomLeftRadius = 8;
        container.style.borderBottomRightRadius = 8;
        
        // 아이템 아이콘 (텍스트로 대체)
        var iconLabel = new Label(GetItemIcon(item));
        iconLabel.style.fontSize = 32;
        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(iconLabel);
        
        // 아이템 이름 (짧게)
        var nameLabel = new Label(TruncateItemName(item.name));
        nameLabel.style.fontSize = 14;
        nameLabel.style.color = GetGradeColor(item.grade);
        nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(nameLabel);
        
        // 수량 레이블 (2개 이상일 때만)
        if (item.quantity > 1)
        {
            var qtyLabel = new Label($"x{item.quantity}");
            qtyLabel.style.fontSize = 12;
            qtyLabel.style.color = new StyleColor(Color.gray);
            qtyLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            container.Add(qtyLabel);
        }
        
        // 클릭 이벤트 - 아이템 툴팁 표시
        container.RegisterCallback<MouseDownEvent>(evt => OnInventoryItemClicked(item, evt));
        
        // 오른쪽 클릭 - 합성
        container.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 1) // 오른쪽 클릭
            {
                OnInventoryItemRightClick(item);
                evt.StopPropagation();
            }
        });
        
        return container;
    }
    
    /// <summary>
    /// 아이템 아이콘 문자열 가져오기
    /// </summary>
    private string GetItemIcon(ItemData item)
    {
        if (item.id.Contains("sword") || item.id.Contains("weapon")) return "⚔️";
        if (item.id.Contains("armor") || item.id.Contains("helmet") || item.id.Contains("chest")) return "🛡️";
        if (item.id.Contains("ring") || item.id.Contains("necklace") || item.id.Contains("accessory")) return "💍";
        if (item.id.Contains("boots") || item.id.Contains("shoes")) return "👢";
        if (item.id.Contains("bow")) return "🏹";
        if (item.id.Contains("staff")) return "🪄";
        return "📦";
    }
    
    /// <summary>
    /// 아이템 이름 짧게 자르기
    /// </summary>
    private string TruncateItemName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        // "일반 검_1_1234" -> "일반 검"
        int underscoreIndex = name.IndexOf('_');
        if (underscoreIndex > 0)
            return name.Substring(0, underscoreIndex);
        return name.Length > 6 ? name.Substring(0, 6) + "..." : name;
    }
    
    /// <summary>
    /// 등급에 따른 색상 가져오기
    /// </summary>
    private Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return new Color(0.8f, 0.8f, 0.8f); // 일반 - 회색
            case 1: return new Color(0.2f, 0.8f, 0.2f); // 고급 - 초록
            case 2: return new Color(0.2f, 0.6f, 1f);   // 희귀 - 파랑
            case 3: return new Color(1f, 0.6f, 0.2f);   // 영웅 - 주황
            case 4: return new Color(1f, 0.4f, 0.8f);   // 전설 - 분홍
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 인벤토리 아이템 클릭 이벤트
    /// </summary>
    private void OnInventoryItemClicked(ItemData item, MouseDownEvent evt)
    {
        Debug.Log($"인벤토리 아이템 클릭: {item.name}");
        ShowItemTooltip(item, evt.mousePosition);
    }
    
    /// <summary>
    /// 인벤토리 아이템 오른쪽 클릭 (합성)
    /// </summary>
    private void OnInventoryItemRightClick(ItemData item)
    {
        Debug.Log($"인벤토리 아이템 우클릭 (합성): {item.name}");
        
        if (InventorySystem.Instance != null)
        {
            bool success = InventorySystem.Instance.Synthesize(item.id, item.grade);
            if (success)
            {
                RefreshInventoryGrid(); // 그리드 새로고침
                GameLogger.Info($"{item.name} 합성 성공!");
            }
            else
            {
                GameLogger.Warn("합성 조건을 만족하지 못합니다 (5개 필요).");
            }
        }
    }
    
    /// <summary>
    /// 아이템 툴팁 표시
    /// </summary>
    private void ShowItemTooltip(ItemData item, Vector2 position)
    {
        var tooltip = _root.Q<VisualElement>("ItemTooltip");
        if (tooltip == null) return;
        
        var tooltipName = _root.Q<Label>("TooltipName");
        var tooltipGrade = _root.Q<Label>("TooltipGrade");
        
        if (tooltipName != null) tooltipName.text = item.name;
        if (tooltipGrade != null)
        {
            string[] gradeNames = { "일반", "고급", "희귀", "영웅", "전설" };
            tooltipGrade.text = item.grade < gradeNames.Length ? gradeNames[item.grade] : "알 수 없음";
            tooltipGrade.style.color = GetGradeColor(item.grade);
        }
        
        // 툴팁 위치 설정
        tooltip.style.left = position.x;
        tooltip.style.top = position.y;
        tooltip.style.display = DisplayStyle.Flex;
        
        // 2초 후 자동 숨김
        Invoke(nameof(HideItemTooltip), 2f);
    }
    
    /// <summary>
    /// 아이템 툴팁 숨기기
    /// </summary>
    private void HideItemTooltip()
    {
        var tooltip = _root.Q<VisualElement>("ItemTooltip");
        if (tooltip != null)
            tooltip.style.display = DisplayStyle.None;
    }
    
    // ========== 업그레이드 리스트 ==========
    
    // 업그레이드 아이템 소스 리스트
    private List<UpgradeItemData> _upgradeItemList = new List<UpgradeItemData>();
    
    /// <summary>
    /// 업그레이드 아이템 데이터
    /// </summary>
    private struct UpgradeItemData
    {
        public string name;
        public string costType;
        public int cost;
        public string description;
    }
    
    /// <summary>
    /// 업그레이드 그리드 새로고침 (ListView 기반)
    /// </summary>
    private void RefreshUpgradeGrid()
    {
        var listView = _upgradeGrid as ListView;
        if (listView == null) return;
        
        if (GameState.Instance == null) return;
        
        string tabType = _currentUpgradeTab;
        
        // 업그레이드 아이템 리스트 생성
        _upgradeItemList.Clear();
        
        switch (tabType)
        {
            case "gold":
                _upgradeItemList.Add(new UpgradeItemData { name = "공격력 증가", costType = "골드", cost = 100, description = "골드로 공격력 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "방어력 증가", costType = "골드", cost = 100, description = "골드로 방어력 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "체력 증가", costType = "골드", cost = 100, description = "골드로 체력 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "이동속도 증가", costType = "골드", cost = 100, description = "골드로 이동속도 증가" });
                break;
            case "stat":
                _upgradeItemList.Add(new UpgradeItemData { name = "STR 증가", costType = "스탯 포인트", cost = 1, description = "스탯 포인트로 STR 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "DEX 증가", costType = "스탯 포인트", cost = 1, description = "스탯 포인트로 DEX 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "INT 증가", costType = "스탯 포인트", cost = 1, description = "스탯 포인트로 INT 증가" });
                _upgradeItemList.Add(new UpgradeItemData { name = "LUK 증가", costType = "스탯 포인트", cost = 1, description = "스탯 포인트로 LUK 증가" });
                break;
            case "gem":
                _upgradeItemList.Add(new UpgradeItemData { name = "전설 등급 무기", costType = "보석", cost = 50, description = "보석으로 전설 무기 구매" });
                _upgradeItemList.Add(new UpgradeItemData { name = "전설 등급 방어구", costType = "보석", cost = 50, description = "보석으로 전설 방어구 구매" });
                _upgradeItemList.Add(new UpgradeItemData { name = "희귀 등급 장신구", costType = "보석", cost = 30, description = "보석으로 희귀 장신구 구매" });
                break;
            case "rebirth":
                _upgradeItemList.Add(new UpgradeItemData { name = "환생하기", costType = "레벨 100", cost = 1, description = "레벨 100 도달 시 환생 가능" });
                break;
        }
        
        // ListView 설정
        listView.itemsSource = _upgradeItemList;
        listView.makeItem = MakeUpgradeItem;
        listView.bindItem = BindUpgradeItem;
        listView.itemHeight = 60; // 업그레이드 아이템 높이
        
        Debug.Log($"업그레이드 리스트 업데이트: {_upgradeItemList.Count}개 항목 ({tabType})");
    }
    
    /// <summary>
    /// 업그레이드 아이템 VisualElement 생성 (makeItem)
    /// </summary>
    private VisualElement MakeUpgradeItem()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.SpaceBetween;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 12;
        container.style.paddingBottom = 12;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 6;
        
        // 이름
        var nameLabel = new Label();
        nameLabel.style.fontSize = 28;
        nameLabel.style.flexGrow = 1;
        container.Add(nameLabel);
        
        // 비용
        var costLabel = new Label();
        costLabel.style.fontSize = 22;
        costLabel.style.color = new StyleColor(Color.yellow);
        costLabel.style.marginRight = 15;
        container.Add(costLabel);
        
        // 구매 버튼
        var buyBtn = new Button();
        buyBtn.text = "구매";
        buyBtn.style.fontSize = 24;
        buyBtn.style.paddingLeft = 20;
        buyBtn.style.paddingRight = 20;
        container.Add(buyBtn);
        
        return container;
    }
    
    /// <summary>
    /// 업그레이드 아이템 바인딩 (bindItem)
    /// </summary>
    private void BindUpgradeItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _upgradeItemList.Count) return;
        
        var item = _upgradeItemList[index];
        
        if (element.childCount >= 3)
        {
            var nameLabel = element[0] as Label;
            var costLabel = element[1] as Label;
            var buyBtn = element[2] as Button;
            
            if (nameLabel != null) nameLabel.text = item.name;
            if (costLabel != null) costLabel.text = $"{item.costType}: {item.cost}";
            if (buyBtn != null)
            {
                buyBtn.clicked -= null; // 이벤트 초기화 (실제로는 매번 새로 바인딩)
                buyBtn.clicked += () => OnUpgradePurchase(item.name, item.costType, item.cost);
            }
        }
    }
    
    /// <summary>
    /// 업그레이드 구매 이벤트
    /// </summary>
    private void OnUpgradePurchase(string name, string costType, int cost)
    {
        Debug.Log($"업그레이드 구매: {name} (비용: {costType} {cost})");
        // 실제 구매 로직 구현
    }
    
    // ========== 미션 리스트 ==========
    
    // 미션 아이템 소스 리스트
    private List<MissionItemData> _missionItemList = new List<MissionItemData>();
    
    /// <summary>
    /// 미션 아이템 데이터
    /// </summary>
    private struct MissionItemData
    {
        public string name;
        public int progress;
        public int target;
        public string reward;
    }
    
    /// <summary>
    /// 미션 그리드 새로고침 (ListView 기반)
    /// </summary>
    private void RefreshMissionsGrid()
    {
        var listView = _missionsGrid as ListView;
        if (listView == null) return;
        
        if (GameState.Instance == null) return;
        
        string tabType = _currentMissionsTab;
        
        // 미션 아이템 리스트 생성
        _missionItemList.Clear();
        
        switch (tabType)
        {
            case "daily":
                _missionItemList.Add(new MissionItemData { name = "몬스터 50마리 처치", progress = 30, target = 50, reward = "골드 1000" });
                _missionItemList.Add(new MissionItemData { name = "스테이지 10 클리어", progress = 5, target = 10, reward = "보석 5" });
                _missionItemList.Add(new MissionItemData { name = "아이템 5개 합성", progress = 2, target = 5, reward = "보석 3" });
                break;
            case "weekly":
                _missionItemList.Add(new MissionItemData { name = "보스 5마리 처치", progress = 1, target = 5, reward = "전설 등급 아이템" });
                _missionItemList.Add(new MissionItemData { name = "누적 골드 100만 획득", progress = 500000, target = 1000000, reward = "보석 50" });
                break;
        }
        
        // ListView 설정
        listView.itemsSource = _missionItemList;
        listView.makeItem = MakeMissionItem;
        listView.bindItem = BindMissionItem;
        listView.itemHeight = 120; // 미션 아이템 높이 (진행도 바 포함)
        
        Debug.Log($"미션 리스트 업데이트: {_missionItemList.Count}개 미션 ({tabType})");
    }
    
    /// <summary>
    /// 미션 아이템 VisualElement 생성 (makeItem)
    /// </summary>
    private VisualElement MakeMissionItem()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 12;
        container.style.paddingBottom = 12;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 6;
        
        // 미션 이름
        var nameLabel = new Label();
        nameLabel.style.fontSize = 26;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(nameLabel);
        
        // 진행도
        var progressLabel = new Label();
        progressLabel.style.fontSize = 20;
        progressLabel.style.color = new StyleColor(Color.gray);
        container.Add(progressLabel);
        
        // 진행도 바
        var progressBarBg = new VisualElement();
        progressBarBg.style.height = 20;
        progressBarBg.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.3f));
        progressBarBg.style.borderTopLeftRadius = 10;
        progressBarBg.style.borderTopRightRadius = 10;
        progressBarBg.style.borderBottomLeftRadius = 10;
        progressBarBg.style.borderBottomRightRadius = 10;
        progressBarBg.style.marginTop = 8;
        
        var progressBarFill = new VisualElement();
        progressBarFill.style.height = 20;
        progressBarFill.style.backgroundColor = new StyleColor(new Color(0.29f, 0.62f, 1f));
        progressBarFill.style.borderTopLeftRadius = 10;
        progressBarFill.style.borderTopRightRadius = 10;
        progressBarFill.style.borderBottomLeftRadius = 10;
        progressBarFill.style.borderBottomRightRadius = 10;
        progressBarFill.style.width = Length.Percent(50); // bindItem에서 업데이트
        progressBarBg.Add(progressBarFill);
        container.Add(progressBarBg);
        
        // 보상
        var rewardLabel = new Label();
        rewardLabel.style.fontSize = 22;
        rewardLabel.style.color = new StyleColor(Color.yellow);
        rewardLabel.style.marginTop = 8;
        container.Add(rewardLabel);
        
        return container;
    }
    
    /// <summary>
    /// 미션 아이템 바인딩 (bindItem)
    /// </summary>
    private void BindMissionItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _missionItemList.Count) return;
        
        var mission = _missionItemList[index];
        
        if (element.childCount >= 4)
        {
            var nameLabel = element[0] as Label;
            var progressLabel = element[1] as Label;
            var progressBarBg = element[2] as VisualElement;
            var rewardLabel = element[3] as Label;
            
            if (nameLabel != null) nameLabel.text = mission.name;
            if (progressLabel != null) progressLabel.text = $"{mission.progress}/{mission.target}";
            if (progressBarBg != null && progressBarBg.childCount > 0)
            {
                var progressBarFill = progressBarBg[0] as VisualElement;
                if (progressBarFill != null)
                {
                    float percent = mission.target > 0 ? Mathf.Min((float)mission.progress / mission.target * 100, 100) : 0;
                    progressBarFill.style.width = Length.Percent(percent);
                }
            }
            if (rewardLabel != null) rewardLabel.text = $"보상: {mission.reward}";
        }
    }
}