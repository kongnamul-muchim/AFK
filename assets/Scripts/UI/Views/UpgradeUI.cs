using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 업그레이드 UI 전담 클래스
/// UIManager에서 업그레이드 관련 로직을 분리 (SRP 준수)
/// </summary>
public class UpgradeUIClass : MonoBehaviour
{
    [SerializeField] private VisualElement _upgradeContainer;
    
    private IGameState _gameState;
    private ILogger _logger;
    
    private List<UpgradeItemData> _upgradeItemList = new List<UpgradeItemData>();
    private string _currentTab = "gold";
    
    private VisualElement _root;
    private ListView _listView;
    
    // 탭 버튼들
    private Button _upgradeTabGold;
    private Button _upgradeTabStat;
    private Button _upgradeTabGem;
    private Button _upgradeTabRebirth;
    
    private void Awake()
    {
        InjectDependencies();
    }
    
    private void InjectDependencies()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
        _logger = serviceLocator.Get<ILogger>();
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        _upgradeTabGold = _root.Q<Button>("UpgradeTabGold");
        _upgradeTabStat = _root.Q<Button>("UpgradeTabStat");
        _upgradeTabGem = _root.Q<Button>("UpgradeTabGem");
        _upgradeTabRebirth = _root.Q<Button>("UpgradeTabRebirth");
        
        _upgradeContainer = _root.Q<VisualElement>("UpgradeGrid");
        
        SetupTabs();
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
        RefreshUpgradeGrid();
    }
    
    private void ResetTabButtons()
    {
        if (_upgradeTabGold != null) _upgradeTabGold.RemoveFromClassList("active");
        if (_upgradeTabStat != null) _upgradeTabStat.RemoveFromClassList("active");
        if (_upgradeTabGem != null) _upgradeTabGem.RemoveFromClassList("active");
        if (_upgradeTabRebirth != null) _upgradeTabRebirth.RemoveFromClassList("active");
    }
    
    public void RefreshUpgradeGrid()
    {
        if (_upgradeContainer == null || _gameState == null) return;
        
        var listView = _upgradeContainer as ListView;
        if (listView == null) return;
        
        _upgradeItemList.Clear();
        
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
                PopulateRebirthOption();
                break;
        }
        
        listView.itemsSource = null;
        listView.makeItem = null;
        listView.bindItem = null;
        
        listView.itemsSource = _upgradeItemList;
        listView.makeItem = MakeUpgradeItem;
        listView.bindItem = BindUpgradeItem;
        listView.fixedItemHeight = 80;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
        listView.showBoundCollectionSize = false;
        listView.reorderable = false;
        
        listView.Rebuild();
        
        _logger?.Debug($"업그레이드 리스트 업데이트: {_upgradeItemList.Count}개 항목 ({_currentTab})");
    }
    
    private void PopulateGoldUpgrades()
    {
        var goldUpgrades = _gameState.Player.goldUpgrades.ToDictionary();
        foreach (var kvp in goldUpgrades)
        {
            string statName = GetStatName(kvp.Key);
            int level = kvp.Value;
            int cost = GetUpgradeCost(kvp.Key, level);
            _upgradeItemList.Add(new UpgradeItemData
            {
                name = $"{statName} (Lv.{level})",
                costType = "골드",
                cost = cost,
                description = GetUpgradeDescription(kvp.Key, level)
            });
        }
    }
    
    private void PopulateStatUpgrades()
    {
        var statUpgrades = _gameState.Player.statUpgrades.ToDictionary();
        int statPoints = _gameState.Player.statPoints;
        
        foreach (var kvp in statUpgrades)
        {
            string statName = GetStatName(kvp.Key);
            int level = kvp.Value;
            _upgradeItemList.Add(new UpgradeItemData
            {
                name = $"{statName} (Lv.{level})",
                costType = "스탯 포인트",
                cost = 1,
                description = $"남은 SP: {statPoints}"
            });
        }
    }
    
    private void PopulateGemUpgrades()
    {
        int gemCount = _gameState.Player.gems;
        _upgradeItemList.Add(new UpgradeItemData { name = "전설 등급 무기", costType = "보석", cost = 50, description = $"보석: {gemCount:N0}" });
        _upgradeItemList.Add(new UpgradeItemData { name = "전설 등급 방어구", costType = "보석", cost = 50, description = $"보석: {gemCount:N0}" });
        _upgradeItemList.Add(new UpgradeItemData { name = "희귀 등급 장신구", costType = "보석", cost = 30, description = $"보석: {gemCount:N0}" });
    }
    
    private void PopulateRebirthOption()
    {
        int playerLevel = _gameState.Player.level;
        int rebirthCount = _gameState.Rebirth.rebirthCount;
        _upgradeItemList.Add(new UpgradeItemData
        {
            name = $"환생하기 ({rebirthCount}회)",
            costType = $"레벨 {playerLevel}/100",
            cost = 1,
            description = "레벨 100 도달 시 환생 가능"
        });
    }
    
    private VisualElement MakeUpgradeItem()
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
        container.style.marginBottom = 10;
        
        var nameLabel = new Label();
        nameLabel.style.fontSize = 28;
        nameLabel.style.flexGrow = 1;
        nameLabel.style.color = new StyleColor(Color.white);
        container.Add(nameLabel);
        
        var costLabel = new Label();
        costLabel.style.fontSize = 22;
        costLabel.style.color = new StyleColor(Color.yellow);
        costLabel.style.marginRight = 15;
        container.Add(costLabel);
        
        var buyBtn = new Button();
        buyBtn.text = "구매";
        buyBtn.style.fontSize = 24;
        buyBtn.style.paddingLeft = 20;
        buyBtn.style.paddingRight = 20;
        container.Add(buyBtn);
        
        return container;
    }
    
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
                buyBtn.clicked += () => OnUpgradePurchase(item.name, item.costType, item.cost);
            }
        }
    }
    
    private void OnUpgradePurchase(string name, string costType, int cost)
    {
        _logger?.Debug($"업그레이드 구매: {name} (비용: {costType} {cost})");
        // 실제 구매 로직 구현
    }
    
    private string GetStatName(string key)
    {
        switch (key)
        {
            case "attack": return "공격력";
            case "defense": return "방어력";
            case "hp": return "체력";
            case "hpRegen": return "HP 회복";
            case "attackSpeed": return "공격속도";
            case "critChance": return "치명타 확률";
            case "critDamage": return "치명타 데미지";
            case "decisiveChance": return "결정타 확률";
            case "decisiveDamage": return "결정타 데미지";
            case "goldBonus": return "골드 획득량";
            case "expBonus": return "경험치 획득량";
            default: return key;
        }
    }
    
    private int GetUpgradeCost(string statKey, int level)
    {
        switch (statKey)
        {
            case "attack": return 100 * (level + 1);
            case "defense": return 80 * (level + 1);
            case "hp": return 50 * (level + 1);
            case "hpRegen": return 60 * (level + 1);
            case "attackSpeed": return 150 * (level + 1);
            case "critChance": return 120 * (level + 1);
            case "critDamage": return 100 * (level + 1);
            case "decisiveChance": return 200 * (level + 1);
            case "decisiveDamage": return 200 * (level + 1);
            case "goldBonus": return 300 * (level + 1);
            case "expBonus": return 250 * (level + 1);
            default: return 100 * (level + 1);
        }
    }
    
    private string GetUpgradeDescription(string statKey, int level)
    {
        float value = CalculateUpgradeValue(statKey, level);
        
        switch (statKey)
        {
            case "attack": return $"+{value}";
            case "defense": return $"+{value}";
            case "hp": return $"+{value}";
            case "hpRegen": return $"+{value}/sec";
            case "attackSpeed": return $"+{value}%";
            case "critChance": return $"+{value}%";
            case "critDamage": return $"+{value}%";
            case "decisiveChance": return $"+{value}%";
            case "decisiveDamage": return $"+{value}%";
            case "goldBonus": return $"+{value}%";
            case "expBonus": return $"+{value}%";
            default: return $"+{value}";
        }
    }
    
    private float CalculateUpgradeValue(string statKey, int level)
    {
        float GetEfficiencyMultiplier(int lvl)
        {
            if (lvl < 10) return 1.0f;
            if (lvl < 20) return 1.5f;
            if (lvl < 30) return 2.0f;
            if (lvl < 40) return 2.5f;
            return 3.0f;
        }
        
        float CalcUpgradeValue(int lvl)
        {
            float total = 0;
            for (int i = 0; i < lvl; i++)
            {
                total += GetEfficiencyMultiplier(i);
            }
            return total;
        }
        
        float value = CalcUpgradeValue(level);
        
        switch (statKey)
        {
            case "attack": return value * 2;
            case "defense": return value * 1;
            case "hp": return value * 10;
            case "hpRegen": return value * 1;
            case "attackSpeed": return value * 1;
            case "critChance": return value * 0.2f;
            case "critDamage": return value * 1;
            case "decisiveChance": return value * 0.2f;
            case "decisiveDamage": return value * 1;
            case "goldBonus": return value * 1;
            case "expBonus": return value * 1;
            default: return value;
        }
    }
    
    private struct UpgradeItemData
    {
        public string name;
        public string costType;
        public int cost;
        public string description;
    }
}
