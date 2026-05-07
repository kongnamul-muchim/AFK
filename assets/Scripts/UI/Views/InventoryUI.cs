using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
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
    
    private Label _bonusAtk;
    private Label _bonusDef;
    private Label _bonusHP;
    private Label _inventoryGold;
    
    // 장비 슬롯들
    private VisualElement _weaponSlot;
    private VisualElement _armorSlot;
    private VisualElement _accessorySlot;
    private VisualElement _bootsSlot;
    
    // 비교 툴팁 상태
    private string _pendingCompareItemId = null;
    private int _pendingCompareGrade = 0;

    // 합성 버튼
    private Button _synthesizeAllButton;
    private Label _synthesisResultLabel;

    private void Awake()
    {
        try
        {
            InjectDependencies();
            // Debug.Log("InventoryUIClass.Awake() - DI 성공");
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"InventoryUIClass.Awake() - DI 실패: {e.Message}");
        }
    }
    
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;
        _gameState = Bootstrap.Container.Resolve<IGameState>();
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
        
        // 장비 슬롯 찾기 (USS의 .equipment-slot 클래스로 찾아서 순서대로 저장)
        var allSlots = _root.Query<VisualElement>(className: "equipment-slot").ToList();
        if (allSlots.Count >= 4)
        {
            _weaponSlot = allSlots[0];
            _armorSlot = allSlots[1];
            _accessorySlot = allSlots[2];
            _bootsSlot = allSlots[3];
            // Debug.Log($"장비 슬롯 발견: weapon={_weaponSlot != null}, armor={_armorSlot != null}, accessory={_accessorySlot != null}, boots={_bootsSlot != null}");
        }
        else
        {
            // Debug.LogWarning($"장비 슬롯 부족: {allSlots.Count}개 (4개 기대)");
        }
        
        // ScrollView 설정
        if (_scrollView != null)
        {
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        
        // 장비 보너스 합계 라벨
        _bonusAtk = _root.Q<Label>("BonusAtk");
        _bonusDef = _root.Q<Label>("BonusDef");
        _bonusHP = _root.Q<Label>("BonusHP");
        _inventoryGold = _root.Q<Label>("InventoryGold");
        
        // 합성 버튼 찾기
        _synthesizeAllButton = _root.Q<Button>("BatchSynthesizeBtn");
        if (_synthesizeAllButton != null)
        {
            _synthesizeAllButton.clicked += OnSynthesizeAllClicked;
        }

        // 합성 결과 레이블
        _synthesisResultLabel = _root.Q<Label>("SynthesisResultLabel");
        if (_synthesisResultLabel != null)
        {
            _synthesisResultLabel.text = "";
            _synthesisResultLabel.style.display = DisplayStyle.None;
        }

        SetupTabs();
        
        if (_tabWeapon != null)
            OnTabClicked("weapon", _tabWeapon);
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
    /// 장비 슬롯 UI 업데이트 (장착된 아이템 표시)
    /// </summary>
    private void UpdateEquipmentSlots()
    {
        if (_gameState == null) return;
        
        // 장비 목록 가져오기
        var equipment = _gameState.Inventory.equipment;
        
        // 각 슬롯에 대해
        UpdateSingleEquipmentSlot(_weaponSlot, equipment.FirstOrDefault(e => e.slot == (int)EquipmentSlot.Weapon), "⚔️", "무기");
        UpdateSingleEquipmentSlot(_armorSlot, equipment.FirstOrDefault(e => e.slot == (int)EquipmentSlot.Armor), "🛡️", "방어구");
        UpdateSingleEquipmentSlot(_accessorySlot, equipment.FirstOrDefault(e => e.slot == (int)EquipmentSlot.Accessory), "💍", "액세서리");
        UpdateSingleEquipmentSlot(_bootsSlot, equipment.FirstOrDefault(e => e.slot == (int)EquipmentSlot.Boots), "👢", "부츠");
        
        UpdateBonusDisplay();
    }

    /// <summary>
    /// 장비 보너스 합계 + 골드 표시 갱신
    /// </summary>
    private void UpdateBonusDisplay()
    {
        if (_gameState == null) return;
        
        float totalAtk = 0, totalDef = 0, totalHp = 0;
        foreach (var eq in _gameState.Inventory.equipment)
        {
            totalAtk += eq.attackBonus;
            totalDef += eq.defenseBonus;
            totalHp += eq.healthBonus;
        }
        
        if (_bonusAtk != null) _bonusAtk.text = $"공격력: +{totalAtk}%";
        if (_bonusDef != null) _bonusDef.text = $"방어력: +{totalDef}%";
        if (_bonusHP != null) _bonusHP.text = $"체력: +{totalHp}%";
        if (_inventoryGold != null) _inventoryGold.text = _gameState.Player.gold.ToString("N0");
    }
    
    /// <summary>
    /// 개별 장비 슬롯 업데이트
    /// </summary>
    private void UpdateSingleEquipmentSlot(VisualElement slot, EquipmentData? equipment, string defaultEmoji, string slotName)
    {
        if (slot == null) return;
        
        // 슬롯 안의 모든 자식 제거 (아이템 아이콘, 텍스트 등)
        slot.Clear();
        
        if (equipment.HasValue)
        {
            var eq = equipment.Value;
            
            // 아이템 아이콘 (이모지)
            var iconLabel = new Label(defaultEmoji);
            iconLabel.style.fontSize = 60;
            iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            slot.Add(iconLabel);
            
            // 아이템 이름
            var nameLabel = new Label(eq.name);
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = Color.white;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.marginTop = 5;
            slot.Add(nameLabel);
            
            // 희귀도 표시 (색상) - rarity (0-4) 사용
            string[] rarityNames = GetGradeNames();  // 일반/고급/희귀/영웅/전설
            Color rarityColor = GetGradeColor(eq.rarity);  // rarity 기반 색상
            var gradeLabel = new Label(rarityNames[Mathf.Min(eq.rarity, rarityNames.Length - 1)]);
            gradeLabel.style.fontSize = 14;
            gradeLabel.style.color = rarityColor;
            gradeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            slot.Add(gradeLabel);
            
            // 슬롯 스타일 - 장착됨 상태
            slot.style.borderLeftColor = rarityColor;
            slot.style.borderRightColor = rarityColor;
            slot.style.borderTopColor = rarityColor;
            slot.style.borderBottomColor = rarityColor;
            slot.style.borderLeftWidth = 3;
            slot.style.borderRightWidth = 3;
            slot.style.borderTopWidth = 3;
            slot.style.borderBottomWidth = 3;
            
            // Debug.Log($"장비 슬롯 업데이트: {slotName} = {eq.name} (Grade {eq.grade})");
        }
        else
        {
            // 빈 슬롯 - 기본 이모지
            var iconLabel = new Label(defaultEmoji);
            iconLabel.style.fontSize = 50;
            iconLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            slot.Add(iconLabel);
            
            var nameLabel = new Label($"빈 {slotName}");
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.marginTop = 5;
            slot.Add(nameLabel);
            
            // 슬롯 스타일 - 빈 상태
            slot.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            slot.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            slot.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            slot.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            slot.style.borderLeftWidth = 2;
            slot.style.borderRightWidth = 2;
            slot.style.borderTopWidth = 2;
            slot.style.borderBottomWidth = 2;
        }
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
        
        // 장비 슬롯 업데이트 (장착된 아이템 표시)
        UpdateEquipmentSlots();
        
        // Debug.Log($"인벤토리 그리드 업데이트: {groupedItems.Count}개 그룹 ({_currentTab})");
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
        slot.AddToClassList("inventory-card");
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
            
            // 아이템 이름 (Web 버전과 동일)
            var nameLabel = new Label(itemName);
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            slot.Add(nameLabel);
            
            // 수량
            var countLabel = new Label($"{count}");
            countLabel.style.fontSize = 16;
            countLabel.style.color = Color.white;
            countLabel.style.position = Position.Absolute;
            countLabel.style.bottom = 4;
            countLabel.style.right = 4;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            slot.Add(countLabel);

            // 합성 가능 표시 (count >= 5 && 최대 등급 미만)
            int itemGrade = System.Convert.ToInt32(item["grade"]);
            string itemType = item["type"].ToString();
            int maxGrade = InventorySystem.Instance.CalculateMaxGradeByType(_currentTab);
            if (count >= GameConfig.SynthesisRequiredCount && itemGrade < maxGrade)
            {
                var synthBadge = new Label("합성");
                synthBadge.style.fontSize = 11;
                synthBadge.style.backgroundColor = new Color(0.1f, 0.8f, 0.2f, 0.8f);
                synthBadge.style.color = Color.white;
                synthBadge.style.position = Position.Absolute;
                synthBadge.style.top = 2;
                synthBadge.style.left = 2;
                synthBadge.style.paddingLeft = 4;
                synthBadge.style.paddingRight = 4;
                synthBadge.style.paddingTop = 2;
                synthBadge.style.paddingBottom = 2;
                synthBadge.style.borderTopLeftRadius = 4;
                synthBadge.style.borderBottomRightRadius = 4;
                slot.Add(synthBadge);
            }
            
            slot.RegisterCallback<ClickEvent>(evt => OnItemClicked(itemId));
            slot.RegisterCallback<ContextClickEvent>(evt => OnItemRightClicked(itemId, itemGrade, evt));
            
            // 툴팁 이벤트 (PointerEnter/Leave가 더 안정적)
            // 아이템 스탯 파싱 (CSV에서 가져온 stats JSON)
            int atkBonus = 0, defBonus = 0, hpBonus = 0;
            if (item.ContainsKey("attackBonus"))
            {
                atkBonus = System.Convert.ToInt32(item["attackBonus"]);
                defBonus = System.Convert.ToInt32(item["defenseBonus"]);
                hpBonus = item.ContainsKey("hpBonus") ? System.Convert.ToInt32(item["hpBonus"]) : System.Convert.ToInt32(item["healthBonus"]);
            }
            else if (item.ContainsKey("stats"))
            {
                var statsStr = item["stats"]?.ToString();
                if (!string.IsNullOrEmpty(statsStr))
                {
                    var parsed = JsonUtility.FromJson<ItemStatsJson>(statsStr);
                    atkBonus = parsed.attackBonus;
                    defBonus = parsed.defenseBonus;
                    hpBonus = parsed.hpBonus;
                }
            }
            
            slot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                int grade = GetRarityGrade(rarity);
                TooltipManager.Instance?.ShowItemTooltip(itemName, grade, evt.position, atkBonus, defBonus, hpBonus);
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
    /// 등급 이름 배열 반환 (0=일반, 1=고급, 2=희귀, 3=영웅, 4=전설)
    /// </summary>
    public string[] GetGradeNames()
    {
        return new[] { "일반", "고급", "희귀", "영웅", "전설" };
    }
    
    /// <summary>
    /// 등급별 색상 반환
    /// </summary>
    private Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return new Color(0.61f, 0.64f, 0.69f); // 일반
            case 1: return new Color(0.23f, 0.51f, 0.96f); // 고급
            case 2: return new Color(0.66f, 0.33f, 0.97f); // 희귀
            case 3: return new Color(0.96f, 0.62f, 0.04f); // 영웅
            case 4: return new Color(0.93f, 0.27f, 0.27f); // 전설
            default: return Color.white;
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
    /// 아이템 클릭 이벤트 (1click=비교, 2click=장착)
    /// </summary>
    private void OnItemClicked(string itemId)
    {
        // 아이템 정보 가져오기
        var item = _allItemsData.FirstOrDefault(i => i["id"].ToString() == itemId);
        if (item == null) return;
        
        int grade = System.Convert.ToInt32(item["grade"]);
        string itemType = item["type"].ToString();
        int rarity = GetRarityGrade(item["rarity"].ToString());
        
        Debug.Log($"[TOOLTIP-DBG] 아이템 클릭: id={itemId}, grade={grade}, pending={_pendingCompareItemId}");
        
        // 같은 아이템 두 번째 클릭이면 장착 실행
        if (_pendingCompareItemId == itemId && _pendingCompareGrade == grade)
        {
            Debug.Log($"[TOOLTIP-DBG] 같은 아이템 재클릭 → 장착 실행");
            // 같은 아이템 재클릭 → 장착 실행
            bool success = InventorySystem.Instance.EquipItem(itemId, grade);
            if (success)
            {
                ClearPendingComparison();
                RefreshInventoryGrid(); // UI 새로고침
            }
            return;
        }
        
        // 첫 번째 클릭이거나 다른 아이템 → 비교 툴팁 표시
        Debug.Log($"[TOOLTIP-DBG] 비교 툴팁 표시 시도");
        ShowItemComparison(itemId, grade, itemType, rarity);
    }
    
    /// <summary>
    /// 아이템 비교 툴팁 표시
    /// </summary>
    private void ShowItemComparison(string itemId, int grade, string itemType, int rarity)
    {
        Debug.Log($"[TOOLTIP-DBG] ShowItemComparison 시작: id={itemId}, grade={grade}, type={itemType}, rarity={rarity}");
        
        // 아이템 데이터 가져오기
        var item = _allItemsData.FirstOrDefault(i => i["id"].ToString() == itemId);
        if (item == null) return;
        
        // 새 아이템 스탯 계산
        float newAttack = InventorySystem.Instance.CalculateEquipmentBonus(grade, "attack");
        float newDefense = InventorySystem.Instance.CalculateEquipmentBonus(grade, "defense");
        float newHealth = InventorySystem.Instance.CalculateEquipmentBonus(grade, "health");
        
        Debug.Log($"[TOOLTIP-DBG] 새 스탯: atk={newAttack}, def={newDefense}, hp={newHealth}");
        
        // 현재 장비 스탯 가져오기
        float currentAttack = 0, currentDefense = 0, currentHealth = 0;
        var equipment = _gameState.Inventory.equipment;
        
        EquipmentSlot targetSlot = InventorySystem.Instance.GetSlotFromItemType(itemType);
        var currentEquip = equipment.FirstOrDefault(e => e.slot == (int)targetSlot);
        
        if (currentEquip.id != null)  // 슬롯에 장비가 있을 때
        {
            currentAttack = currentEquip.attackBonus;
            currentDefense = currentEquip.defenseBonus;
            currentHealth = currentEquip.healthBonus;
            Debug.Log($"[TOOLTIP-DBG] 현재 장비 있음: atk={currentAttack}, def={currentDefense}, hp={currentHealth}");
        }
        else
        {
            Debug.Log($"[TOOLTIP-DBG] 현재 장비 없음 (빈 슬롯)");
        }
        
        // 대기 중인 비교 상태 저장
        _pendingCompareItemId = itemId;
        _pendingCompareGrade = grade;
        
        Debug.Log($"[TOOLTIP-DBG] TooltipManager.Instance={(TooltipManager.Instance != null ? "있음" : "null")}");
        
        // 비교 툴팁 표시 (화면 중앙 근처)
        TooltipManager.Instance?.ShowComparisonTooltip(
            item["name"].ToString(),
            rarity,
            itemType,
            newAttack, newDefense, newHealth,
            currentAttack, currentDefense, currentHealth,
            new Vector2(300, 200)  // 중앙 위치
        );
        
        Debug.Log($"[TOOLTIP-DBG] ShowComparisonTooltip 호출 완료");
    }
    
    /// <summary>
    /// 대기 중인 비교 상태 초기화
    /// </summary>
    private void ClearPendingComparison()
    {
        _pendingCompareItemId = null;
        _pendingCompareGrade = 0;
        TooltipManager.Instance?.HideItemTooltip();
    }
    
    /// <summary>
    /// 아이템 우클릭 이벤트 (합성)
    /// </summary>
    private void OnItemRightClicked(string itemId, int itemGrade, ContextClickEvent evt)
    {
        evt.StopPropagation();

        // 보유 아이템 확인
        var ownedItem = _gameState.Inventory.items.FirstOrDefault(i => i.id == itemId && i.grade == itemGrade);
        if (ownedItem.id == null || ownedItem.count < GameConfig.SynthesisRequiredCount)
        {
            string itemName = ownedItem.id != null ? ownedItem.name : itemId;
            int currentCount = ownedItem.id != null ? ownedItem.count : 0;
            ShowSynthesisResult($"{itemName} - 수량 부족 ({currentCount}/{GameConfig.SynthesisRequiredCount})", false);
            return;
        }

        bool success = InventorySystem.Instance.Synthesize(itemId, itemGrade);
        if (success)
        {
            ShowSynthesisResult($"합성 성공! {ownedItem.name} → ", true);
            RefreshInventoryGrid();
        }
        else
        {
            ShowSynthesisResult("합성 실패 (최대 등급)", false);
        }
    }

    /// <summary>
    /// 일괄 합성 버튼 클릭
    /// </summary>
    private void OnSynthesizeAllClicked()
    {
        int count = InventorySystem.Instance.SynthesizeAllByType(_currentTab);
        if (count > 0)
        {
            ShowSynthesisResult($"일괄 합성 완료! ({count}회)", true);
            RefreshInventoryGrid();
        }
        else
        {
            ShowSynthesisResult("합성 가능한 아이템 없음", false);
        }
    }

    /// <summary>
    /// 합성 결과 메시지 표시
    /// </summary>
    private void ShowSynthesisResult(string message, bool success)
    {
        if (_synthesisResultLabel == null) return;

        _synthesisResultLabel.text = message;
        _synthesisResultLabel.style.color = success ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        _synthesisResultLabel.style.display = DisplayStyle.Flex;

        // 3초 후 자동 숨김
        StartCoroutine(HideSynthesisResultAfterDelay(3f));
    }

    private System.Collections.IEnumerator HideSynthesisResultAfterDelay(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (_synthesisResultLabel != null)
        {
            _synthesisResultLabel.style.display = DisplayStyle.None;
        }
    }
}
