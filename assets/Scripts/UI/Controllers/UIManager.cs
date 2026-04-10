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
    
    // ========== 인벤토리 아이템 그리드 ==========
    
    /// <summary>
    /// 인벤토리 그리드 새로고침
    /// </summary>
    private void RefreshInventoryGrid()
    {
        if (_inventoryItems == null) return;
        
        // 기존 아이템 모두 제거
        _inventoryItems.Clear();
        
        if (GameState.Instance == null) return;
        
        var state = GameState.Instance;
        string filterType = _currentInventoryTab; // "weapon", "armor", "accessory", "boots"
        
        // 현재 탭에 맞는 아이템만 필터링
        foreach (var item in state.inventory.items)
        {
            if (!MatchesInventoryTab(item.id, filterType))
                continue;
            
            // 아이템 버튼 생성
            var itemBtn = CreateInventoryItemButton(item);
            _inventoryItems.Add(itemBtn);
        }
        
        Debug.Log($"인벤토리 그리드 업데이트: {_inventoryItems.childCount}개 아이템 ({filterType})");
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
        container.RegisterCallback<PointerEvent>(evt => OnInventoryItemClicked(item, evt));
        
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
    private void OnInventoryItemClicked(ItemData item, PointerEvent evt)
    {
        Debug.Log($"인벤토리 아이템 클릭: {item.name}");
        ShowItemTooltip(item, evt.position);
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
    
    // ========== 업그레이드 그리드 ==========
    
    /// <summary>
    /// 업그레이드 그리드 새로고침
    /// </summary>
    private void RefreshUpgradeGrid()
    {
        if (_upgradeGrid == null) return;
        
        // 기존 아이템 모두 제거
        _upgradeGrid.Clear();
        
        if (GameState.Instance == null) return;
        
        string tabType = _currentUpgradeTab;
        
        // 탭별 업그레이드 항목 생성
        switch (tabType)
        {
            case "gold":
                CreateGoldUpgradeItems();
                break;
            case "stat":
                CreateStatUpgradeItems();
                break;
            case "gem":
                CreateGemUpgradeItems();
                break;
            case "rebirth":
                CreateRebirthUpgradeItems();
                break;
        }
        
        Debug.Log($"업그레이드 그리드 업데이트: {_upgradeGrid.childCount}개 항목 ({tabType})");
    }
    
    /// <summary>
    /// 골드 업그레이드 항목 생성
    /// </summary>
    private void CreateGoldUpgradeItems()
    {
        if (_upgradeGrid == null) return;
        
        // 더미 데이터 - 실제 구현 시 GameState의 업그레이드 시스템과 연동
        string[] goldUpgrades = new string[] { "공격력 증가", "방어력 증가", "체력 증가", "이동속도 증가" };
        
        foreach (var upgradeName in goldUpgrades)
        {
            var item = CreateUpgradeItem(upgradeName, "골드", 100, "골드로 스탯 증가");
            _upgradeGrid.Add(item);
        }
    }
    
    /// <summary>
    /// 스탯 업그레이드 항목 생성
    /// </summary>
    private void CreateStatUpgradeItems()
    {
        if (_upgradeGrid == null) return;
        
        string[] statUpgrades = new string[] { "STR 증가", "DEX 증가", "INT 증가", "LUK 증가" };
        
        foreach (var upgradeName in statUpgrades)
        {
            var item = CreateUpgradeItem(upgradeName, "스탯 포인트", 1, "스탯 포인트로 능력치 증가");
            _upgradeGrid.Add(item);
        }
    }
    
    /// <summary>
    /// 보석 업그레이드 항목 생성
    /// </summary>
    private void CreateGemUpgradeItems()
    {
        if (_upgradeGrid == null) return;
        
        string[] gemUpgrades = new string[] { "전설 등급 무기", "전설 등급 방어구", "희귀 등급 장신구" };
        
        foreach (var upgradeName in gemUpgrades)
        {
            var item = CreateUpgradeItem(upgradeName, "보석", 50, "보석으로 고급 아이템 구매");
            _upgradeGrid.Add(item);
        }
    }
    
    /// <summary>
    /// 환생 업그레이드 항목 생성
    /// </summary>
    private void CreateRebirthUpgradeItems()
    {
        if (_upgradeGrid == null) return;
        
        var item = CreateUpgradeItem("환생하기", "레벨 100", 1, "레벨 100 도달 시 환생 가능");
        _upgradeGrid.Add(item);
    }
    
    /// <summary>
    /// 업그레이드 항목 UI 생성
    /// </summary>
    private VisualElement CreateUpgradeItem(string name, string costType, int cost, string description)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.SpaceBetween;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        
        // 이름
        var nameLabel = new Label(name);
        nameLabel.style.fontSize = 28;
        nameLabel.style.flexGrow = 1;
        container.Add(nameLabel);
        
        // 비용
        var costLabel = new Label($"{costType}: {cost}");
        costLabel.style.fontSize = 22;
        costLabel.style.color = new StyleColor(Color.yellow);
        costLabel.style.marginRight = 15;
        container.Add(costLabel);
        
        // 구매 버튼
        var buyBtn = new Button(() => OnUpgradePurchase(name, costType, cost));
        buyBtn.text = "구매";
        buyBtn.style.fontSize = 24;
        buyBtn.style.paddingLeft = 20;
        buyBtn.style.paddingRight = 20;
        container.Add(buyBtn);
        
        return container;
    }
    
    /// <summary>
    /// 업그레이드 구매 이벤트
    /// </summary>
    private void OnUpgradePurchase(string name, string costType, int cost)
    {
        Debug.Log($"업그레이드 구매: {name} (비용: {costType} {cost})");
        // 실제 구매 로직 구현
    }
    
    // ========== 미션 그리드 ==========
    
    /// <summary>
    /// 미션 그리드 새로고침
    /// </summary>
    private void RefreshMissionsGrid()
    {
        if (_missionsGrid == null) return;
        
        // 기존 아이템 모두 제거
        _missionsGrid.Clear();
        
        if (GameState.Instance == null) return;
        
        string tabType = _currentMissionsTab;
        
        // 탭별 미션 생성
        switch (tabType)
        {
            case "daily":
                CreateDailyMissions();
                break;
            case "weekly":
                CreateWeeklyMissions();
                break;
        }
        
        Debug.Log($"미션 그리드 업데이트: {_missionsGrid.childCount}개 미션 ({tabType})");
    }
    
    /// <summary>
    /// 일일 미션 생성
    /// </summary>
    private void CreateDailyMissions()
    {
        if (_missionsGrid == null) return;
        
        var missions = new[]
        {
            new { name = "몬스터 50마리 처치", progress = 30, target = 50, reward = "골드 1000" },
            new { name = "스테이지 10 클리어", progress = 5, target = 10, reward = "보석 5" },
            new { name = "아이템 5개 합성", progress = 2, target = 5, reward = "보석 3" },
        };
        
        foreach (var mission in missions)
        {
            var item = CreateMissionItem(mission.name, mission.progress, mission.target, mission.reward);
            _missionsGrid.Add(item);
        }
    }
    
    /// <summary>
    /// 주간 미션 생성
    /// </summary>
    private void CreateWeeklyMissions()
    {
        if (_missionsGrid == null) return;
        
        var missions = new[]
        {
            new { name = "보스 5마리 처치", progress = 1, target = 5, reward = "전설 등급 아이템" },
            new { name = "누적 골드 100만 획득", progress = 500000, target = 1000000, reward = "보석 50" },
        };
        
        foreach (var mission in missions)
        {
            var item = CreateMissionItem(mission.name, mission.progress, mission.target, mission.reward);
            _missionsGrid.Add(item);
        }
    }
    
    /// <summary>
    /// 미션 항목 UI 생성
    /// </summary>
    private VisualElement CreateMissionItem(string name, int progress, int target, string reward)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        
        // 미션 이름
        var nameLabel = new Label(name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(nameLabel);
        
        // 진행도
        var progressLabel = new Label($"{progress}/{target}");
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
        
        float percent = target > 0 ? (float)progress / target : 0;
        var progressBarFill = new VisualElement();
        progressBarFill.style.height = 20;
        progressBarFill.style.backgroundColor = new StyleColor(new Color(0.29f, 0.62f, 1f));
        progressBarFill.style.borderTopLeftRadius = 10;
        progressBarFill.style.borderTopRightRadius = 10;
        progressBarFill.style.borderBottomLeftRadius = 10;
        progressBarFill.style.borderBottomRightRadius = 10;
        progressBarFill.style.width = Length.Percent(Mathf.Min(percent * 100, 100));
        progressBarBg.Add(progressBarFill);
        container.Add(progressBarBg);
        
        // 보상
        var rewardLabel = new Label($"보상: {reward}");
        rewardLabel.style.fontSize = 22;
        rewardLabel.style.color = new StyleColor(Color.yellow);
        rewardLabel.style.marginTop = 8;
        container.Add(rewardLabel);
        
        return container;
    }
}