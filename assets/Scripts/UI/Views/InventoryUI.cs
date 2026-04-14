using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 인벤토리 UI 전담 클래스 (Web 버전과 동일한 카드 그리드 레이아웃)
/// </summary>
public class InventoryUIClass : MonoBehaviour
{
    private IGameState _gameState;
    
    private List<Dictionary<string, object>> _allItemsData = new List<Dictionary<string, object>>();
    
    private string _currentTab = "weapon";
    
    private VisualElement _root;
    private VisualElement _inventoryContainer;
    private ScrollView _scrollView;
    
    // 탭 버튼들
    private Button _tabWeapon;
    private Button _tabArmor;
    private Button _tabAccessory;
    private Button _tabBoots;

    private void Awake()
    {
        try
        {
            InjectDependencies();
            Debug.Log("InventoryUIClass.Awake() - DI 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventoryUIClass.Awake() - DI 실패: {e.Message}");
        }
    }
    
    private void InjectDependencies()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        // 탭 버튼 찾기
        _tabWeapon = _root.Q<Button>("TabWeapon");
        _tabArmor = _root.Q<Button>("TabArmor");
        _tabAccessory = _root.Q<Button>("TabAccessory");
        _tabBoots = _root.Q<Button>("TabBoots");
        
        // ScrollView와 아이템 컨테이너 찾기
        _scrollView = _root.Q<ScrollView>("InventoryScrollContainer");
        _inventoryContainer = _root.Q<VisualElement>("InventoryItems");
        
        // ScrollView 설정
        if (_scrollView != null)
        {
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        
        SetupTabs();
    }
    
    private void SetupTabs()
    {
        if (_tabWeapon != null)
            _tabWeapon.clicked += () => OnTabClicked("weapon", _tabWeapon);
        if (_tabArmor != null)
            _tabArmor.clicked += () => OnTabClicked("armor", _tabArmor);
        if (_tabAccessory != null)
            _tabAccessory.clicked += () => OnTabClicked("accessory", _tabAccessory);
        if (_tabBoots != null)
            _tabBoots.clicked += () => OnTabClicked("boots", _tabBoots);
    }
    
    private void OnTabClicked(string tabType, Button clickedTab)
    {
        _currentTab = tabType;
        ResetTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        RefreshInventoryGrid();
    }
    
    private void ResetTabButtons()
    {
        if (_tabWeapon != null) _tabWeapon.RemoveFromClassList("active");
        if (_tabArmor != null) _tabArmor.RemoveFromClassList("active");
        if (_tabAccessory != null) _tabAccessory.RemoveFromClassList("active");
        if (_tabBoots != null) _tabBoots.RemoveFromClassList("active");
    }

    /// <summary>
    /// 인벤토리 리스트 새로고침 (자동 줄바꿈 그리드)
    /// </summary>
    public void RefreshInventoryGrid()
    {
        if (_inventoryContainer == null || _gameState == null) return;

        // 컨테이너 비우기
        _inventoryContainer.Clear();
        
        // CSV 데이터에서 현재 탭의 아이템만 필터링
        _allItemsData = DataLoader.Load("items");
        var tabItems = _allItemsData.Where(item => 
        {
            var type = item["type"].ToString().ToLower();
            return type == _currentTab;
        }).ToList();
        
        // 아이템을 베이스 이름별로 그룹화
        var groupedItems = GroupItemsByBase(tabItems);
        
        // 각 아이템을 개별 카드로 생성 (자동 줄바꿈)
        foreach (var group in groupedItems)
        {
            foreach (var item in group.Value)
            {
                var slot = CreateItemSlot(item);
                slot.style.marginRight = 4;
                slot.style.marginBottom = 4;
                // 화면 폭을 5등분 (정확히 5개씩 행 배치)
                slot.style.flexGrow = 1;
                slot.style.flexShrink = 0;
                slot.style.flexBasis = new StyleLength(new Length(18, LengthUnit.Percent));
                _inventoryContainer.Add(slot);
            }
        }
        
        Debug.Log($"인벤토리 그리드 업데이트: {groupedItems.Count}개 그룹 ({_currentTab})");
    }
    
    /// <summary>
    /// 아이템을 베이스 이름별로 그룹화
    /// </summary>
    private Dictionary<string, List<Dictionary<string, object>>> GroupItemsByBase(
        List<Dictionary<string, object>> items)
    {
        var groups = new Dictionary<string, List<Dictionary<string, object>>>();
        
        foreach (var item in items)
        {
            var baseName = item["name"].ToString();
            
            if (!groups.ContainsKey(baseName))
            {
                groups[baseName] = new List<Dictionary<string, object>>();
            }
            groups[baseName].Add(item);
        }
        
        // 각 그룹을 희귀도 순서대로 정렬
        var rarityOrder = new Dictionary<string, int>
        {
            { "common", 0 }, { "rare", 1 }, { "epic", 2 }, { "legendary", 3 }, { "mythic", 4 }
        };
        
        foreach (var key in groups.Keys.ToList())
        {
            groups[key] = groups[key].OrderBy(item =>
            {
                var rarity = item["rarity"].ToString().ToLower();
                return rarityOrder.ContainsKey(rarity) ? rarityOrder[rarity] : 5;
            }).ToList();
        }
        
        return groups;
    }
    
    /// <summary>
    /// 아이템 슬롯 생성 (정사각형 카드)
    /// </summary>
    private VisualElement CreateItemSlot(Dictionary<string, object> item)
    {
        var slot = new VisualElement();
        slot.style.flexDirection = FlexDirection.Column;
        slot.style.alignItems = Align.Center;
        slot.style.justifyContent = Justify.Center;
        slot.style.paddingLeft = 4;
        slot.style.paddingRight = 4;
        slot.style.paddingTop = 4;
        slot.style.paddingBottom = 4;
        slot.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.18f));
        slot.style.borderTopLeftRadius = 8;
        slot.style.borderTopRightRadius = 8;
        slot.style.borderBottomLeftRadius = 8;
        slot.style.borderBottomRightRadius = 8;
        slot.style.borderLeftWidth = 2;
        slot.style.borderRightWidth = 2;
        slot.style.borderTopWidth = 2;
        slot.style.borderBottomWidth = 2;
        // 정사각형: flex basis로 너비 조절, minHeight로 높이도 같게
        slot.style.flexBasis = new StyleLength(new Length(18, LengthUnit.Percent));
        slot.style.minWidth = 80;
        slot.style.minHeight = 80;
        
        // 정사각형 유지를 위해 실제 너비에 맞춰 높이 설정 (런타임)
        slot.RegisterCallback<GeometryChangedEvent>((evt) =>
        {
            var newHeight = slot.resolvedStyle.width;
            slot.style.minHeight = newHeight;
            slot.style.maxHeight = newHeight;
        });
        
        var itemId = item["id"].ToString();
        var itemName = item["name"].ToString();
        var rarity = item["rarity"].ToString().ToLower();
        
        // 보유 수량 확인
        var ownedItem = _gameState.Inventory.items.FirstOrDefault(i => i.id == itemId);
        var count = ownedItem.count;
        
        // 발견 여부 확인
        var discovered = _gameState.Inventory.discoveredItems.Contains(itemId);
        
        if (count > 0)
        {
            slot.AddToClassList("has-item");
            slot.AddToClassList(rarity);
            slot.style.borderLeftColor = GetRarityColor(rarity);
            slot.style.borderRightColor = GetRarityColor(rarity);
            slot.style.borderTopColor = GetRarityColor(rarity);
            slot.style.borderBottomColor = GetRarityColor(rarity);
            
            // 아이템 아이콘 (이모지) - 크게
            var iconLabel = new Label(GetItemEmoji(item));
            iconLabel.style.fontSize = 40;
            slot.Add(iconLabel);
            
            // 수량
            var countLabel = new Label($"{count}");
            countLabel.style.fontSize = 16;
            countLabel.style.color = Color.white;
            countLabel.style.position = Position.Absolute;
            countLabel.style.bottom = 4;
            countLabel.style.right = 4;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            slot.Add(countLabel);
            
            slot.RegisterCallback<ClickEvent>(evt => OnItemClicked(itemId));
            slot.RegisterCallback<ContextClickEvent>(evt => OnItemRightClicked(itemId, evt));
            
            // 툴팁 이벤트 (PointerEnter/Leave가 더 안정적)
            slot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                int grade = GetRarityGrade(rarity);
                TooltipManager.Instance?.ShowItemTooltip(itemName, grade, evt.position);
            });
            slot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                TooltipManager.Instance?.HideItemTooltip();
            });
        }
        else if (discovered)
        {
            slot.AddToClassList("discovered");
            slot.AddToClassList(rarity);
            slot.style.opacity = 0.85f;
            slot.style.borderLeftColor = GetRarityColor(rarity);
            slot.style.borderRightColor = GetRarityColor(rarity);
            slot.style.borderTopColor = GetRarityColor(rarity);
            slot.style.borderBottomColor = GetRarityColor(rarity);
            
            var iconLabel = new Label(GetItemEmoji(item));
            iconLabel.style.fontSize = 40;
            slot.Add(iconLabel);
            
            var countLabel = new Label("0");
            countLabel.style.fontSize = 16;
            countLabel.style.color = Color.gray;
            countLabel.style.position = Position.Absolute;
            countLabel.style.bottom = 4;
            countLabel.style.right = 4;
            slot.Add(countLabel);
            
            slot.RegisterCallback<ClickEvent>(evt => OnItemClicked(itemId));
            
            // 발견된 아이템도 툴팁 지원
            slot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                int grade = GetRarityGrade(rarity);
                TooltipManager.Instance?.ShowItemTooltip(itemName, grade, evt.position);
            });
            slot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                TooltipManager.Instance?.HideItemTooltip();
            });
        }
        else
        {
            slot.AddToClassList("locked");
            slot.style.opacity = 0.4f;
            slot.style.borderLeftColor = Color.gray;
            slot.style.borderRightColor = Color.gray;
            slot.style.borderTopColor = Color.gray;
            slot.style.borderBottomColor = Color.gray;
            
            var lockLabel = new Label("🔒");
            lockLabel.style.fontSize = 32;
            slot.Add(lockLabel);
        }
        
        return slot;
    }
    
    /// <summary>
    /// 희귀도별 색상 반환
    /// </summary>
    private Color GetRarityColor(string rarity)
    {
        switch (rarity)
        {
            case "common": return new Color(0.61f, 0.64f, 0.69f);
            case "rare": return new Color(0.23f, 0.51f, 0.96f);
            case "epic": return new Color(0.66f, 0.33f, 0.97f);
            case "legendary": return new Color(0.96f, 0.62f, 0.04f);
            case "mythic": return new Color(0.93f, 0.27f, 0.27f);
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 희귀도별 등급 번호 반환 (0=common, 1=rare, 2=epic, 3=legendary, 4=mythic)
    /// </summary>
    private int GetRarityGrade(string rarity)
    {
        switch (rarity)
        {
            case "common": return 0;
            case "rare": return 1;
            case "epic": return 2;
            case "legendary": return 3;
            case "mythic": return 4;
            default: return 0;
        }
    }
    
    /// <summary>
    /// 아이템 타입별 이모지 반환
    /// </summary>
    private string GetItemEmoji(Dictionary<string, object> item)
    {
        var type = item["type"].ToString().ToLower();
        switch (type)
        {
            case "weapon": return "⚔️";
            case "armor": return "🛡️";
            case "boots": return "👢";
            case "accessory": return "💍";
            default: return "📦";
        }
    }
    
    /// <summary>
    /// 아이템 클릭 이벤트 (장착)
    /// </summary>
    private void OnItemClicked(string itemId)
    {
        Debug.Log($"아이템 클릭: {itemId}");
        
        // 아이템 정보 가져오기
        var item = _allItemsData.FirstOrDefault(i => i["id"].ToString() == itemId);
        if (item == null) return;
        
        int grade = System.Convert.ToInt32(item["grade"]);
        
        // InventorySystem으로 장비 장착
        bool success = InventorySystem.Instance.EquipItem(itemId, grade);
        if (success)
        {
            Debug.Log($"아이템 장착 성공: {itemId}");
            RefreshInventoryGrid(); // UI 새로고침
        }
    }
    
    /// <summary>
    /// 아이템 우클릭 이벤트 (합성)
    /// </summary>
    private void OnItemRightClicked(string itemId, ContextClickEvent evt)
    {
        evt.StopPropagation();
        Debug.Log($"아이템 우클릭 (합성): {itemId}");
    }
}
