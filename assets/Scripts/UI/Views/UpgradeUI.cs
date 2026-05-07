using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 업그레이드 UI 전담 클래스 (Web 버전과 동일한 카드 리스트 레이아웃)
/// </summary>
public class UpgradeUIClass : MonoBehaviour
{
    private IGameState _gameState;
    
    private string _currentTab = "gold";
    
    private VisualElement _root;
    private ScrollView _scrollView;
    private VisualElement _upgradeContainer;
    
    // 탭 버튼들
    private Button _upgradeTabGold;
    private Button _upgradeTabStat;
    private Button _upgradeTabGem;
    private Button _upgradeTabRebirth;
    
    // 헤더 표시 요소들
    private Label _upgradePointsDisplay;
    private Label _upgradeGoldDisplay;
    private Label _upgradeGemsDisplay;
    
    // 스탯 정의 (Web 버전과 동일)
    private Dictionary<string, StatDefinition> _statDefinitions;
    
    // 보석 업그레이드 정의
    private Dictionary<string, GemUpgradeDefinition> _gemUpgradeDefinitions;
    
    private void Awake()
    {
        try
        {
            InjectDependencies();
            DefineStats();
            DefineGemUpgrades();
            // Debug.Log("UpgradeUIClass.Awake() - DI 성공");
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"UpgradeUIClass.Awake() - DI 실패: {e.Message}");
        }
    }
    
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;
        _gameState = Bootstrap.Container.Resolve<IGameState>();
    }
    
    private void DefineStats()
    {
        _statDefinitions = new Dictionary<string, StatDefinition>
        {
            {
                "attack", new StatDefinition
                {
                    name = "공격력",
                    maxLevel = null,
                    goldCostBase = 100,
                    statCost = 1,
                    baseValue = 2,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 2)}"
                }
            },
            {
                "defense", new StatDefinition
                {
                    name = "방어력",
                    maxLevel = null,
                    goldCostBase = 80,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}"
                }
            },
            {
                "hp", new StatDefinition
                {
                    name = "체력",
                    maxLevel = null,
                    goldCostBase = 50,
                    statCost = 1,
                    baseValue = 10,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 10)}"
                }
            },
            {
                "hpRegen", new StatDefinition
                {
                    name = "HP 회복",
                    maxLevel = null,
                    goldCostBase = 60,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}/sec"
                }
            },
            {
                "attackSpeed", new StatDefinition
                {
                    name = "공격속도",
                    maxLevel = 50,
                    goldCostBase = 150,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) =>
                    {
                        var val = CalculateStatValue(level, 1);
                        return $"+{val}% (×{(100 + val) / 100:F2})";
                    }
                }
            },
            {
                "critChance", new StatDefinition
                {
                    name = "치명타 확률",
                    maxLevel = 500,
                    goldCostBase = 120,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 0.2f):F1}%"
                }
            },
            {
                "critDamage", new StatDefinition
                {
                    name = "치명타 데미지",
                    maxLevel = null,
                    goldCostBase = 100,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold", "stat" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}%"
                }
            },
            {
                "decisiveChance", new StatDefinition
                {
                    name = "결정타 확률",
                    maxLevel = 500,
                    goldCostBase = 200,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold" },
                    getValue = (level) => $"+{CalculateStatValue(level, 0.2f):F1}%",
                    unlockCondition = () => _gameState != null && GetGoldUpgradeLevel("critChance") >= 500,
                    unlockMessage = "치명타 확률 100% 필요"
                }
            },
            {
                "decisiveDamage", new StatDefinition
                {
                    name = "결정타 데미지",
                    maxLevel = null,
                    goldCostBase = 200,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}%",
                    unlockCondition = () => _gameState != null && GetGoldUpgradeLevel("critChance") >= 500,
                    unlockMessage = "치명타 확률 100% 필요"
                }
            },
            {
                "goldBonus", new StatDefinition
                {
                    name = "골드 획득량",
                    maxLevel = 100,
                    goldCostBase = 300,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}%"
                }
            },
            {
                "expBonus", new StatDefinition
                {
                    name = "경험치 획득량",
                    maxLevel = 100,
                    goldCostBase = 300,
                    statCost = 1,
                    baseValue = 1,
                    tabs = new[] { "gold" },
                    getValue = (level) => $"+{CalculateStatValue(level, 1)}%"
                }
            }
        };
    }
    
    private void DefineGemUpgrades()
    {
        _gemUpgradeDefinitions = new Dictionary<string, GemUpgradeDefinition>
        {
            {
                "offlineBonus", new GemUpgradeDefinition
                {
                    name = "오프라인 보상 증가",
                    description = "오프라인 보상 2% 증가",
                    maxLevel = null,
                    gemCostBase = 10,
                    getValue = (level) => $"+{(level + 1) * 2}%"
                }
            },
            {
                "critDamage", new GemUpgradeDefinition
                {
                    name = "치명타 피해 증가",
                    description = "치명타 피해 2% 증가",
                    maxLevel = null,
                    gemCostBase = 15,
                    getValue = (level) => $"+{(level + 1) * 2}%"
                }
            },
            {
                "autoCombatDamage", new GemUpgradeDefinition
                {
                    name = "자동 전투 강화",
                    description = "자동 전투 시 데미지 2% 증가",
                    maxLevel = 50,
                    gemCostBase = 20,
                    getValue = (level) => $"+{Math.Min(100, (level + 1) * 2)}%"
                }
            },
            {
                "rebirthBonus", new GemUpgradeDefinition
                {
                    name = "환생 보너스",
                    description = "환생 시 보너스 포인트 1개 추가",
                    maxLevel = 10,
                    gemCostBase = 50,
                    getValue = (level) => $"+{level + 1} 포인트"
                }
            },
            {
                "dropRate", new GemUpgradeDefinition
                {
                    name = "드롭 확률 업",
                    description = "레어 아이템 드롭률 증가 (등급별 차등)",
                    maxLevel = 20,
                    gemCostBase = 25,
                    getValue = (level) => $"일반:{Math.Max(10, 70 - (int)(1.1 * level))}%, 고급:{20 + (int)(0.2 * level)}%, 희귀:{7 + (int)(0.4 * level)}%"
                }
            },
            {
                "baseStats", new GemUpgradeDefinition
                {
                    name = "기본 스탯 증가",
                    description = "공격력/방어력/체력 1% 증가",
                    maxLevel = null,
                    gemCostBase = 15,
                    getValue = (level) => $"+{(level + 1) * 1}%"
                }
            }
        };
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        _upgradeTabGold = _root.Q<Button>("UpgradeTabGold");
        _upgradeTabStat = _root.Q<Button>("UpgradeTabStat");
        _upgradeTabGem = _root.Q<Button>("UpgradeTabGem");
        _upgradeTabRebirth = _root.Q<Button>("UpgradeTabRebirth");
        
        _scrollView = _root.Q<ScrollView>("UpgradeGrid");
        _upgradeContainer = _scrollView;
        
        // ScrollView 설정
        if (_scrollView != null)
        {
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        
        // 헤더 표시 요소들
        _upgradePointsDisplay = _root.Q<Label>("UpgradePointsDisplay");
        _upgradeGoldDisplay = _root.Q<Label>("UpgradeGoldDisplay");
        _upgradeGemsDisplay = _root.Q<Label>("UpgradeGemsDisplay");
        
        SetupTabs();
        
        // 초기 디스플레이 업데이트 (탭 클릭 없이 바로 표시)
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void SetupTabs()
    {
        if (_upgradeTabGold != null)
            _upgradeTabGold.clicked += () => OnTabClicked("gold", _upgradeTabGold);
        if (_upgradeTabStat != null)
            _upgradeTabStat.clicked += () => OnTabClicked("stat", _upgradeTabStat);
        if (_upgradeTabGem != null)
            _upgradeTabGem.clicked += () => OnTabClicked("gem", _upgradeTabGem);
        if (_upgradeTabRebirth != null)
            _upgradeTabRebirth.clicked += () => OnTabClicked("rebirth", _upgradeTabRebirth);
    }
    
    private void OnTabClicked(string tabType, Button clickedTab)
    {
        _currentTab = tabType;
        ResetTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void ResetTabButtons()
    {
        if (_upgradeTabGold != null) _upgradeTabGold.RemoveFromClassList("active");
        if (_upgradeTabStat != null) _upgradeTabStat.RemoveFromClassList("active");
        if (_upgradeTabGem != null) _upgradeTabGem.RemoveFromClassList("active");
        if (_upgradeTabRebirth != null) _upgradeTabRebirth.RemoveFromClassList("active");
    }
    
    /// <summary>
    /// 디스플레이 업데이트 (골드, 포인트, 보석 표시)
    /// </summary>
    public void UpdateDisplay()
    {
        if (_upgradeGoldDisplay != null)
        {
            if (_currentTab == "gold")
            {
                var gold = _gameState.Player.gold; // Player.gold 사용 (CombatSystem과 일치)
                _upgradeGoldDisplay.text = $"💰 {gold:N0}";
                _upgradeGoldDisplay.style.display = DisplayStyle.Flex;
            }
            else
            {
                _upgradeGoldDisplay.style.display = DisplayStyle.None;
            }
        }
        
        if (_upgradePointsDisplay != null)
        {
            if (_currentTab == "stat")
            {
                var points = _gameState.Player.statPoints;
                _upgradePointsDisplay.text = $"⭐ {points}";
                _upgradePointsDisplay.style.display = DisplayStyle.Flex;
            }
            else if (_currentTab == "rebirth")
            {
                var bonusPoints = _gameState.Rebirth.bonusPoints;
                _upgradePointsDisplay.text = $"🎁 {bonusPoints}"; // 환생 포인트는 🎁로 (보석과 구분)
                _upgradePointsDisplay.style.display = DisplayStyle.Flex;
            }
            else
            {
                _upgradePointsDisplay.style.display = DisplayStyle.None;
            }
        }
        
        if (_upgradeGemsDisplay != null)
        {
            if (_currentTab == "gem")
            {
                var gems = _gameState.Player.gems;
                _upgradeGemsDisplay.text = $"💎 {gems:N0}";
                _upgradeGemsDisplay.style.display = DisplayStyle.Flex;
            }
            else
            {
                _upgradeGemsDisplay.style.display = DisplayStyle.None;
            }
        }
    }
    
    /// <summary>
    /// 업그레이드 그리드 새로고침
    /// </summary>
    public void RefreshUpgradeGrid()
    {
        if (_upgradeContainer == null || _gameState == null) return;
        
        // 컨테이너 비우기
        _upgradeContainer.Clear();
        
        switch (_currentTab)
        {
            case "gold":
                PopulateGoldUpgrades();
                break;
            case "stat":
                PopulateStatUpgrades();
                break;
            case "gem":
                PopulateGemUpgrades();
                break;
            case "rebirth":
                PopulateRebirthUpgrades();
                break;
        }
        
        // Debug.Log($"업그레이드 그리드 업데이트: {_currentTab}");
    }
    
    private void PopulateGoldUpgrades()
    {
        foreach (var kvp in _statDefinitions)
        {
            if (!kvp.Value.tabs.Contains("gold")) continue;
            
            // 잠금 조건 확인
            if (kvp.Value.unlockCondition != null && !kvp.Value.unlockCondition())
            {
                var lockedItem = CreateUpgradeCard(
                    "???",
                    "🔒 해금 필요",
                    kvp.Value.unlockMessage ?? "조건 미충족",
                    0,
                    "골드",
                    false,
                    false,
                    null
                );
                _upgradeContainer.Add(lockedItem);
                continue;
            }
            
            var level = GetGoldUpgradeLevel(kvp.Key);
            var isMaxLevel = kvp.Value.maxLevel.HasValue && level >= kvp.Value.maxLevel.Value;
            var cost = isMaxLevel ? 0 : CalculateGoldCost(kvp.Key, level);
            var hasCurrency = _gameState.Player.gold >= cost;
            var currentValue = kvp.Value.getValue(level);
            var nextValue = isMaxLevel ? "" : kvp.Value.getValue(level + 1);
            
            var item = CreateUpgradeCard(
                kvp.Value.name,
                $"Lv.{level}",
                $"{currentValue}{(isMaxLevel ? "" : $" → {nextValue}")}",
                cost,
                "G",  // 골드 표기 (간결하게)
                hasCurrency && !isMaxLevel,
                isMaxLevel,
                () => PurchaseGoldUpgrade(kvp.Key)
            );
            _upgradeContainer.Add(item);
        }
    }
    
    private void PopulateStatUpgrades()
    {
        foreach (var kvp in _statDefinitions)
        {
            if (!kvp.Value.tabs.Contains("stat")) continue;
            
            var level = GetStatUpgradeLevel(kvp.Key);
            var isMaxLevel = kvp.Value.maxLevel.HasValue && level >= kvp.Value.maxLevel.Value;
            var cost = kvp.Value.statCost;
            var hasCurrency = _gameState.Player.statPoints >= cost;
            var currentValue = kvp.Value.getValue(level);
            var nextValue = isMaxLevel ? "" : kvp.Value.getValue(level + 1);
            
            var item = CreateUpgradeCard(
                kvp.Value.name,
                $"Lv.{level}",
                $"{currentValue}{(isMaxLevel ? "" : $" → {nextValue}")}",
                cost,
                "SP",  // 스탯 포인트 표기 (간결하게)
                hasCurrency && !isMaxLevel,
                isMaxLevel,
                () => PurchaseStatUpgrade(kvp.Key)
            );
            _upgradeContainer.Add(item);
        }
    }
    
    private void PopulateGemUpgrades()
    {
        foreach (var kvp in _gemUpgradeDefinitions)
        {
            var upgradeData = GetGemUpgradeData(kvp.Key);
            var isUnlocked = upgradeData.unlocked;
            var level = upgradeData.level;
            var isMaxLevel = kvp.Value.maxLevel.HasValue && level >= kvp.Value.maxLevel.Value;
            
            if (!isUnlocked)
            {
                // 해금 전
                var unlockCost = kvp.Value.gemCostBase;
                var canAfford = _gameState.Player.gems >= unlockCost;
                
                var item = CreateGemUpgradeCard(
                    kvp.Value.name,
                    "🔒 해금 필요",
                    kvp.Value.description,
                    unlockCost,
                    canAfford,
                    false,
                    () => UnlockGemUpgrade(kvp.Key)
                );
                _upgradeContainer.Add(item);
            }
            else
            {
                // 해금 후
                var upgradeCost = isMaxLevel ? 0 : CalculateGemCost(kvp.Value.gemCostBase, level);
                var canAfford = _gameState.Player.gems >= upgradeCost && !isMaxLevel;
                var currentValue = kvp.Value.getValue(level);
                var nextValue = isMaxLevel ? "" : kvp.Value.getValue(level + 1);
                
                var item = CreateGemUpgradeCard(
                    kvp.Value.name,
                    isMaxLevel ? "최대 레벨" : $"Lv.{level}",
                    $"{currentValue}{(isMaxLevel ? "" : $" → {nextValue}")}",
                    upgradeCost,
                    canAfford,
                    isMaxLevel,
                    isMaxLevel ? (Action)null : () => UpgradeGemUpgrade(kvp.Key, level)
                );
                _upgradeContainer.Add(item);
            }
        }
    }
    
    private void PopulateRebirthUpgrades()
    {
        // 환생 가능 여부 확인 (RebirthSystem 기준)
        bool canRebirth = RebirthSystem.Instance != null && RebirthSystem.Instance.CanRebirth();
        var playerLevel = _gameState.Player.level;
        var minLevel = RebirthSystem.Instance != null ? GameConfig.MinRebirthLevel : 50;
        var rebirthCount = _gameState.Rebirth.rebirthCount;
        
        // 환생 섹션
        var rebirthSection = CreateRebirthSection(canRebirth, rebirthCount, playerLevel, minLevel);
        _upgradeContainer.Add(rebirthSection);
        
        // 구분선
        var divider = new VisualElement();
        divider.style.height = 1;
        divider.style.backgroundColor = new Color(1, 1, 1, 0.1f);
        divider.style.marginTop = 10;
        divider.style.marginBottom = 10;
        _upgradeContainer.Add(divider);
        
        // 환생 업그레이드 목록 (보너스 포인트 사용)
        var rebirthUpgrades = GetRebirthUpgradeDefinitions();
        foreach (var upgrade in rebirthUpgrades)
        {
            var currentLevel = GetRebirthUpgradeLevel(upgrade.key);
            var isMaxLevel = currentLevel >= upgrade.maxLevel;
            var canPurchase = _gameState.Rebirth.bonusPoints >= upgrade.costPerLevel && !isMaxLevel;
            
            var item = CreateRebirthUpgradeCard(
                upgrade.name,
                $"Lv.{currentLevel}/{upgrade.maxLevel}",
                upgrade.description,
                upgrade.costPerLevel,
                canPurchase,
                isMaxLevel,
                () => PurchaseRebirthUpgrade(upgrade.key)
            );
            item.userData = "bp"; // 보너스 포인트 표시용
            _upgradeContainer.Add(item);
        }
    }
    
    private VisualElement CreateRebirthSection(bool canRebirth, int rebirthCount, int playerLevel, int minLevel)
    {
        var section = new VisualElement();
        section.style.flexDirection = FlexDirection.Column;
        section.style.alignItems = Align.Center;
        section.style.paddingLeft = 15;
        section.style.paddingRight = 15;
        section.style.paddingTop = 20;
        section.style.paddingBottom = 20;
        section.style.backgroundColor = new Color(0.1f, 0.1f, 0.18f);
        section.style.borderTopLeftRadius = 12;
        section.style.borderTopRightRadius = 12;
        section.style.borderBottomLeftRadius = 12;
        section.style.borderBottomRightRadius = 12;
        section.style.marginBottom = 10;
        
        if (canRebirth)
        {
            var bonusCount = RebirthSystem.Instance != null ? RebirthSystem.Instance.GetBonusPointsPreview() : 0;
            
            var title = new Label("🔄 환생 가능!");
            title.style.fontSize = 28;
            title.style.color = new Color(1, 0.84f, 0);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);
            
            var info1 = new Label($"현재 레벨: {playerLevel} / 최소 레벨: {minLevel}");
            info1.style.fontSize = 20;
            info1.style.color = new Color(0.69f, 0.69f, 0.69f);
            section.Add(info1);
            
            var info2 = new Label($"환생 횟수: {rebirthCount}회");
            info2.style.fontSize = 20;
            info2.style.color = new Color(0.69f, 0.69f, 0.69f);
            section.Add(info2);
            
            var bonus = new Label($"획득 보너스: 💎 {bonusCount}개");
            bonus.style.fontSize = 22;
            bonus.style.color = new Color(0.29f, 0.93f, 0.5f);
            section.Add(bonus);
            
            var note = new Label("* 환생 시 레벨 1로 초기화, 장비/인벤토리 유지, 보너스 포인트 획득");
            note.style.fontSize = 16;
            note.style.color = new Color(0.53f, 0.53f, 0.53f);
            note.style.marginTop = 5;
            section.Add(note);
            
            var btn = new Button(() => PerformRebirth());
            btn.text = "🔄 환생하기";
            btn.style.fontSize = 28;
            btn.style.paddingLeft = 30;
            btn.style.paddingRight = 30;
            btn.style.paddingTop = 15;
            btn.style.paddingBottom = 15;
            btn.style.backgroundColor = new Color(1, 0.84f, 0);
            btn.style.color = Color.black;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            btn.style.marginTop = 15;
            section.Add(btn);
        }
        else
        {
            var progress = Math.Min(100, (playerLevel * 100.0f / minLevel));
            
            var title = new Label("🔒 환생 잠김");
            title.style.fontSize = 28;
            title.style.color = new Color(0.4f, 0.4f, 0.4f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);
            
            var info1 = new Label($"현재 레벨: {playerLevel} / 최소 레벨: {minLevel}");
            info1.style.fontSize = 20;
            info1.style.color = new Color(0.69f, 0.69f, 0.69f);
            section.Add(info1);
            
            var info2 = new Label($"환생 횟수: {rebirthCount}회");
            info2.style.fontSize = 20;
            info2.style.color = new Color(0.69f, 0.69f, 0.69f);
            section.Add(info2);
            
            // 진행바
            var progressBarBg = new VisualElement();
            progressBarBg.style.width = Length.Percent(80);
            progressBarBg.style.maxWidth = 300;
            progressBarBg.style.height = 8;
            progressBarBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            progressBarBg.style.borderTopLeftRadius = 4;
            progressBarBg.style.borderTopRightRadius = 4;
            progressBarBg.style.borderBottomLeftRadius = 4;
            progressBarBg.style.borderBottomRightRadius = 4;
            progressBarBg.style.marginTop = 10;
            progressBarBg.style.marginBottom = 10;
            
            var progressBarFill = new VisualElement();
            progressBarFill.style.width = Length.Percent(progress);
            progressBarFill.style.height = 8;
            progressBarFill.style.backgroundColor = new Color(0.29f, 0.93f, 0.5f);
            progressBarFill.style.borderTopLeftRadius = 4;
            progressBarFill.style.borderTopRightRadius = 4;
            progressBarFill.style.borderBottomLeftRadius = 4;
            progressBarFill.style.borderBottomRightRadius = 4;
            progressBarBg.Add(progressBarFill);
            
            section.Add(progressBarBg);
            
            var note = new Label($"레벨 {minLevel}이 되어야 환생할 수 있습니다");
            note.style.fontSize = 18;
            note.style.color = new Color(0.53f, 0.53f, 0.53f);
            section.Add(note);
        }
        
        return section;
    }
    
    /// <summary>
    /// 업그레이드 카드 생성 (골드/스탯 탭용)
    /// </summary>
    private VisualElement CreateUpgradeCard(string name, string level, string stats, int cost, string costType, bool canBuy, bool isMaxLevel, Action onClick)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new Color(0.14f, 0.14f, 0.26f);
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 10;
        
        // 상단 행: 이름 + 레벨
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        
        var nameLabel = new Label(name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.flexGrow = 1;
        topRow.Add(nameLabel);
        
        var levelLabel = new Label(level);
        levelLabel.style.fontSize = 22;
        levelLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.69f, 0.69f, 0.69f);
        topRow.Add(levelLabel);
        
        container.Add(topRow);
        
        // 스탯 행
        var statsLabel = new Label(stats);
        statsLabel.style.fontSize = 20;
        statsLabel.style.color = new Color(0.69f, 0.69f, 0.69f);
        statsLabel.style.marginTop = 5;
        container.Add(statsLabel);
        
        // 하단 행: 비용 + 구매 버튼
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.justifyContent = Justify.SpaceBetween;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.marginTop = 10;
        
        var costLabel = new Label(isMaxLevel ? "최대 레벨" : $"{cost}{costType}");
        costLabel.style.fontSize = 22;
        costLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(1, 0.84f, 0);
        bottomRow.Add(costLabel);
        
        if (!isMaxLevel && onClick != null)
        {
            var buyBtn = new Button(onClick);
            buyBtn.text = "구매";
            buyBtn.style.fontSize = 32;
            buyBtn.style.paddingLeft = 30;
            buyBtn.style.paddingRight = 30;
            buyBtn.style.paddingTop = 12;
            buyBtn.style.paddingBottom = 12;
            buyBtn.style.backgroundColor = canBuy ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
            buyBtn.style.color = canBuy ? Color.black : Color.gray;
            buyBtn.style.borderTopLeftRadius = 8;
            buyBtn.style.borderTopRightRadius = 8;
            buyBtn.style.borderBottomLeftRadius = 8;
            buyBtn.style.borderBottomRightRadius = 8;
            buyBtn.SetEnabled(canBuy);
            bottomRow.Add(buyBtn);
        }
        
        container.Add(bottomRow);
        
        return container;
    }
    
    /// <summary>
    /// 보석 업그레이드 카드 생성
    /// </summary>
    private VisualElement CreateGemUpgradeCard(string name, string level, string stats, int cost, bool canBuy, bool isMaxLevel, Action onClick)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new Color(0.14f, 0.14f, 0.26f);
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 10;
        
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        
        var nameLabel = new Label(name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.flexGrow = 1;
        topRow.Add(nameLabel);
        
        var levelLabel = new Label(level);
        levelLabel.style.fontSize = 22;
        levelLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.69f, 0.69f, 0.69f);
        topRow.Add(levelLabel);
        
        container.Add(topRow);
        
        var statsLabel = new Label(stats);
        statsLabel.style.fontSize = 20;
        statsLabel.style.color = new Color(0.69f, 0.69f, 0.69f);
        statsLabel.style.marginTop = 5;
        container.Add(statsLabel);
        
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.justifyContent = Justify.SpaceBetween;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.marginTop = 10;
        
        var costLabel = new Label(isMaxLevel ? "최대 레벨" : $"💎 {cost}");
        costLabel.style.fontSize = 22;
        costLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.29f, 0.62f, 1);
        bottomRow.Add(costLabel);
        
        if (!isMaxLevel && onClick != null)
        {
            var buyBtn = new Button(onClick);
            buyBtn.text = isMaxLevel ? "완료" : "구매";
            buyBtn.style.fontSize = 32;
            buyBtn.style.paddingLeft = 30;
            buyBtn.style.paddingRight = 30;
            buyBtn.style.paddingTop = 12;
            buyBtn.style.paddingBottom = 12;
            buyBtn.style.backgroundColor = canBuy ? new Color(0.29f, 0.62f, 1) : new Color(0.3f, 0.3f, 0.3f);
            buyBtn.style.color = canBuy ? Color.white : Color.gray;
            buyBtn.style.borderTopLeftRadius = 8;
            buyBtn.style.borderTopRightRadius = 8;
            buyBtn.style.borderBottomLeftRadius = 8;
            buyBtn.style.borderBottomRightRadius = 8;
            buyBtn.SetEnabled(canBuy);
            bottomRow.Add(buyBtn);
        }
        
        container.Add(bottomRow);
        
        return container;
    }
    
    /// <summary>
    /// 환생 업그레이드 카드 생성
    /// </summary>
    private VisualElement CreateRebirthUpgradeCard(string name, string level, string description, int cost, bool canBuy, bool isMaxLevel, Action onClick)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new Color(0.14f, 0.14f, 0.26f);
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 10;
        
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        
        var nameLabel = new Label(name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.flexGrow = 1;
        topRow.Add(nameLabel);
        
        var levelLabel = new Label(level);
        levelLabel.style.fontSize = 22;
        levelLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.69f, 0.69f, 0.69f);
        topRow.Add(levelLabel);
        
        container.Add(topRow);
        
        var descLabel = new Label(description);
        descLabel.style.fontSize = 20;
        descLabel.style.color = new Color(0.69f, 0.69f, 0.69f);
        descLabel.style.marginTop = 5;
        container.Add(descLabel);
        
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.justifyContent = Justify.SpaceBetween;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.marginTop = 10;
        
        var costLabel = new Label(isMaxLevel ? "완성!" : $"{cost}pt");
        costLabel.style.fontSize = 22;
        costLabel.style.color = isMaxLevel ? new Color(0.29f, 0.93f, 0.5f) : new Color(1, 0.84f, 0);
        bottomRow.Add(costLabel);
        
        if (!isMaxLevel && onClick != null)
        {
            var buyBtn = new Button(onClick);
            buyBtn.text = isMaxLevel ? "완성" : "구매";
            buyBtn.style.fontSize = 32;
            buyBtn.style.paddingLeft = 30;
            buyBtn.style.paddingRight = 30;
            buyBtn.style.paddingTop = 12;
            buyBtn.style.paddingBottom = 12;
            buyBtn.style.backgroundColor = canBuy ? new Color(1, 0.84f, 0) : new Color(0.3f, 0.3f, 0.3f);
            buyBtn.style.color = canBuy ? Color.black : Color.gray;
            buyBtn.style.borderTopLeftRadius = 8;
            buyBtn.style.borderTopRightRadius = 8;
            buyBtn.style.borderBottomLeftRadius = 8;
            buyBtn.style.borderBottomRightRadius = 8;
            buyBtn.SetEnabled(canBuy);
            bottomRow.Add(buyBtn);
        }
        
        container.Add(bottomRow);
        
        return container;
    }
    
    // ==================== 유틸리티 메서드들 ====================
    
    private int GetGoldUpgradeLevel(string key)
    {
        if (_gameState.Player.goldUpgrades.ContainsKey(key))
            return _gameState.Player.goldUpgrades[key];
        return 0;
    }
    
    private int GetStatUpgradeLevel(string key)
    {
        if (_gameState.Player.statUpgrades.ContainsKey(key))
            return _gameState.Player.statUpgrades[key];
        return 0;
    }
    
    private GemUpgradeItemData GetGemUpgradeData(string key)
    {
        // 기존 GemUpgradeData(RebirthData.cs)에서 해당 키의 레벨을 읽음
        var gemData = _gameState.GemUpgrades;
        var result = new GemUpgradeItemData();
        
        switch (key)
        {
            case "offlineBonus":
                result.unlocked = gemData.offlineRewardLevel > 0;
                result.level = gemData.offlineRewardLevel;
                break;
            case "critDamage":
                result.unlocked = gemData.critDamageLevel > 0;
                result.level = gemData.critDamageLevel;
                break;
            case "autoCombatDamage":
                result.unlocked = gemData.autoBattleLevel > 0;
                result.level = gemData.autoBattleLevel;
                break;
            case "rebirthBonus":
                result.unlocked = gemData.rebirthBonusLevel > 0;
                result.level = gemData.rebirthBonusLevel;
                break;
            case "dropRate":
                result.unlocked = gemData.dropRateLevel > 0;
                result.level = gemData.dropRateLevel;
                break;
            case "baseStats":
                result.unlocked = gemData.statBonusLevel > 0;
                result.level = gemData.statBonusLevel;
                break;
            default:
                result.unlocked = false;
                result.level = 0;
                break;
        }
        
        return result;
    }
    
    private int GetRebirthUpgradeLevel(string key)
    {
        if (_gameState.Rebirth.upgrades.ContainsKey(key))
            return _gameState.Rebirth.upgrades[key];
        return 0;
    }
    
    private float CalculateStatValue(int level, float baseValue)
    {
        float total = 0;
        for (int i = 0; i < level; i++)
        {
            total += GetEfficiencyMultiplier(i);
        }
        return total * baseValue;
    }
    
    private float GetEfficiencyMultiplier(int lvl)
    {
        if (lvl < 10) return 1.0f;
        if (lvl < 20) return 1.5f;
        if (lvl < 30) return 2.0f;
        if (lvl < 40) return 2.5f;
        return 3.0f;
    }
    
    private int CalculateGoldCost(string key, int currentLevel)
    {
        var baseCost = _statDefinitions[key].goldCostBase;
        if (currentLevel == 0) return baseCost;
        
        int cost = baseCost;
        for (int i = 1; i <= currentLevel; i++)
        {
            if (i % 10 == 0)
            {
                cost = Mathf.CeilToInt(cost * 1.5f);
                cost = Mathf.CeilToInt(cost / 10f) * 10;
            }
            else
            {
                cost = Mathf.CeilToInt(cost * 1.05f);
                cost = Mathf.CeilToInt(cost / 10f) * 10;
            }
        }
        return Math.Max(cost, 10);
    }
    
    private int CalculateGemCost(int baseCost, int currentLevel)
    {
        return Mathf.FloorToInt(baseCost * Mathf.Pow(1.15f, currentLevel));
    }
    
    private int CalculateRebirthBonus()
    {
        // 간단한 계산: 플레이어 레벨 / 10
        return _gameState.Player.level / 10;
    }
    
    private List<RebirthUpgradeDef> GetRebirthUpgradeDefinitions()
    {
        return new List<RebirthUpgradeDef>
        {
            new RebirthUpgradeDef { key = "damageBonus", name = "데미지 보너스", description = "데미지 5% 증가", maxLevel = 10, costPerLevel = 1 },
            new RebirthUpgradeDef { key = "goldBonus", name = "골드 보너스", description = "골드 획득량 5% 증가", maxLevel = 10, costPerLevel = 1 },
            new RebirthUpgradeDef { key = "expBonus", name = "경험치 보너스", description = "경험치 획득량 5% 증가", maxLevel = 10, costPerLevel = 1 },
            new RebirthUpgradeDef { key = "critBonus", name = "치명타 보너스", description = "치명타 확률 2% 증가", maxLevel = 10, costPerLevel = 2 },
            new RebirthUpgradeDef { key = "dropBonus", name = "드롭 보너스", description = "드롭률 1% 증가", maxLevel = 5, costPerLevel = 3 }
        };
    }
    
    // ==================== 구매 액션들 ====================
    
    private void PurchaseGoldUpgrade(string key)
    {
        var level = GetGoldUpgradeLevel(key);
        var cost = CalculateGoldCost(key, level);
        
        if (_gameState.Player.gold < cost) return;
        if (_statDefinitions[key].maxLevel.HasValue && level >= _statDefinitions[key].maxLevel.Value) return;
        
        _gameState.Player.gold -= cost;
        _gameState.Player.goldUpgrades[key] = level + 1;
        
        // Debug.Log($"골드 업그레이드 구매: {key} → Lv.{level + 1}");
        
        EventBus.Instance?.Emit(GameEvents.GOLD_CHANGED);
        EventBus.Instance?.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void PurchaseStatUpgrade(string key)
    {
        var level = GetStatUpgradeLevel(key);
        var cost = _statDefinitions[key].statCost;
        
        if (_gameState.Player.statPoints < cost) return;
        if (_statDefinitions[key].maxLevel.HasValue && level >= _statDefinitions[key].maxLevel.Value) return;
        
        _gameState.Player.statPoints -= cost;
        _gameState.Player.statUpgrades[key] = level + 1;
        
        EventBus.Instance?.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void UnlockGemUpgrade(string key)
    {
        var cost = _gemUpgradeDefinitions[key].gemCostBase;
        if (_gameState.Player.gems < cost) return;
        
        _gameState.Player.gems -= cost;
        
        // 기존 GemUpgradeData 필드에 레벨 설정
        SetGemUpgradeLevel(key, 1);
        
        EventBus.Instance?.Emit(GameEvents.GEM_CHANGED);
        EventBus.Instance?.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void UpgradeGemUpgrade(string key, int currentLevel)
    {
        var cost = CalculateGemCost(_gemUpgradeDefinitions[key].gemCostBase, currentLevel);
        if (_gameState.Player.gems < cost) return;
        if (_gemUpgradeDefinitions[key].maxLevel.HasValue && currentLevel >= _gemUpgradeDefinitions[key].maxLevel.Value) return;
        
        _gameState.Player.gems -= cost;
        SetGemUpgradeLevel(key, currentLevel + 1);
        
        EventBus.Instance?.Emit(GameEvents.GEM_CHANGED);
        EventBus.Instance?.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    /// <summary>
    /// 보석 업그레이드 레벨 설정 (기존 GemUpgradeData 필드에 맞춤)
    /// </summary>
    private void SetGemUpgradeLevel(string key, int level)
    {
        var gemData = _gameState.GemUpgrades;
        switch (key)
        {
            case "offlineBonus": gemData.offlineRewardLevel = level; break;
            case "critDamage": gemData.critDamageLevel = level; break;
            case "autoCombatDamage": gemData.autoBattleLevel = level; break;
            case "rebirthBonus": gemData.rebirthBonusLevel = level; break;
            case "dropRate": gemData.dropRateLevel = level; break;
            case "baseStats": gemData.statBonusLevel = level; break;
        }
    }
    
    private void PerformRebirth()
    {
        if (RebirthSystem.Instance == null || !RebirthSystem.Instance.CanRebirth()) return;
        
        RebirthSystem.Instance.PerformRebirth();
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    private void PurchaseRebirthUpgrade(string key)
    {
        var level = GetRebirthUpgradeLevel(key);
        var defs = GetRebirthUpgradeDefinitions();
        var def = defs.First(d => d.key == key);
        
        if (_gameState.Rebirth.bonusPoints < def.costPerLevel) return;
        if (level >= def.maxLevel) return;
        
        _gameState.Rebirth.bonusPoints -= def.costPerLevel;
        _gameState.Rebirth.upgrades[key] = level + 1;
        
        EventBus.Instance?.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        UpdateDisplay();
        RefreshUpgradeGrid();
    }
    
    // ==================== 데이터 구조체들 ====================
    
    private class StatDefinition
    {
        public string name;
        public int? maxLevel;
        public int goldCostBase;
        public int statCost;
        public float baseValue;
        public string[] tabs;
        public Func<int, string> getValue;
        public Func<bool> unlockCondition;
        public string unlockMessage;
    }
    
    private class GemUpgradeDefinition
    {
        public string name;
        public string description;
        public int? maxLevel;
        public int gemCostBase;
        public Func<int, string> getValue;
    }
    
    private class GemUpgradeItemData
    {
        public bool unlocked = false;
        public int level = 0;
    }
    
    private class RebirthUpgradeDef
    {
        public string key;
        public string name;
        public string description;
        public int maxLevel;
        public int costPerLevel;
    }
}
