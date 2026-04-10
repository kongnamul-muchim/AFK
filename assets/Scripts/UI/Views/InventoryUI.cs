using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 UI 전담 클래스
/// UIManager에서 인벤토리 관련 로직을 분리 (SRP 준수)
/// </summary>
public class InventoryUIClass : MonoBehaviour
{
    [SerializeField] private VisualElement _inventoryContainer;
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private ILogger _logger;
    
    private List<ItemData> _inventoryItemList = new List<ItemData>();
    private string _currentTab = "weapon";
    
    private VisualElement _root;
    private ListView _listView;
    
    // 탭 버튼들
    private Button _tabWeapon;
    private Button _tabArmor;
    private Button _tabAccessory;
    private Button _tabBoots;
    
    private void Awake()
    {
        InjectDependencies();
    }
    
    /// <summary>
    /// 의존성 주입 (DI 적용)
    /// </summary>
    private void InjectDependencies()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
        _eventBus = serviceLocator.Get<IEventBus>();
        _logger = serviceLocator.Get<ILogger>();
    }
    
    /// <summary>
    /// 인벤토리 UI 초기화
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        // 탭 버튼 찾기
        _tabWeapon = _root.Q<Button>("TabWeapon");
        _tabArmor = _root.Q<Button>("TabArmor");
        _tabAccessory = _root.Q<Button>("TabAccessory");
        _tabBoots = _root.Q<Button>("TabBoots");
        
        // 인벤토리 아이템 컨테이너
        _inventoryContainer = _root.Q<VisualElement>("InventoryItems");
        
        SetupTabs();
    }
    
    /// <summary>
    /// 탭 버튼 이벤트 설정
    /// </summary>
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
    /// 인벤토리 리스트 새로고침
    /// </summary>
    public void RefreshInventoryGrid()
    {
        if (_inventoryContainer == null || _gameState == null) return;
        
        var listView = _inventoryContainer as ListView;
        if (listView == null) return;
        
        // 필터링된 아이템 리스트 생성
        _inventoryItemList.Clear();
        foreach (var item in _gameState.Inventory.items)
        {
            if (MatchesTab(item.id, _currentTab))
                _inventoryItemList.Add(item);
        }
        
        // ListView 초기화
        listView.itemsSource = null;
        listView.makeItem = null;
        listView.bindItem = null;
        
        // ListView 재설정
        listView.itemsSource = _inventoryItemList;
        listView.makeItem = MakeInventoryItem;
        listView.bindItem = BindInventoryItem;
        listView.fixedItemHeight = 66;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
        listView.showBoundCollectionSize = false;
        listView.reorderable = false;
        
        listView.Rebuild();
        
        _logger?.Debug($"인벤토리 리스트 업데이트: {_inventoryItemList.Count}개 아이템 ({_currentTab})");
    }
    
    /// <summary>
    /// 아이템 ID가 현재 탭에 해당하는지 확인
    /// </summary>
    private bool MatchesTab(string itemId, string tabType)
    {
        switch (tabType)
        {
            case "weapon":
                return itemId.Contains("sword") || itemId.Contains("weapon") || 
                       itemId.Contains("bow") || itemId.Contains("staff");
            case "armor":
                return itemId.Contains("armor") || itemId.Contains("helmet") || 
                       itemId.Contains("chest") || itemId.Contains("gloves");
            case "accessory":
                return itemId.Contains("ring") || itemId.Contains("necklace") || 
                       itemId.Contains("accessory");
            case "boots":
                return itemId.Contains("boots") || itemId.Contains("shoes");
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 인벤토리 아이템 VisualElement 생성
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
    /// 인벤토리 아이템 바인딩
    /// </summary>
    private void BindInventoryItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _inventoryItemList.Count) return;
        
        var item = _inventoryItemList[index];
        
        var iconLabel = element.Q<Label>("ItemIcon");
        var nameLabel = element.Q<Label>("ItemName");
        var qtyLabel = element.Q<Label>("ItemQuantity");
        
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
            nameLabel.style.color = TooltipManager.Instance?.GetGradeColor(item.grade) ?? Color.white;
        }
        if (qtyLabel != null) qtyLabel.text = item.quantity > 1 ? $"x{item.quantity}" : "";
        
        // 클릭 이벤트
        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) OnInventoryItemClicked(item, evt);
            if (evt.button == 1) OnInventoryItemRightClick(item);
        });
    }
    
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
    
    private string TruncateItemName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        int underscoreIndex = name.IndexOf('_');
        if (underscoreIndex > 0)
            return name.Substring(0, underscoreIndex);
        return name.Length > 6 ? name.Substring(0, 6) + "..." : name;
    }
    
    private void OnInventoryItemClicked(ItemData item, MouseDownEvent evt)
    {
        _logger?.Debug($"인벤토리 아이템 클릭: {item.name}");
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowItemTooltip(item.name, item.grade, evt.mousePosition);
            TooltipManager.Instance.HideItemTooltipDelayed(2f);
        }
    }
    
    private void OnInventoryItemRightClick(ItemData item)
    {
        _logger?.Warn($"인벤토리 아이템 우클릭 (합성): {item.name}");
        // 합성 로직은 InventorySystem에서 처리
        // TODO: InventorySystem DI 적용 후 호출
    }
}
