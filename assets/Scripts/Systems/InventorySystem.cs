using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 장비 슬롯 열거형
/// </summary>
public enum EquipmentSlot
{
    /// <summary>무기</summary>
    Weapon = 0,
    /// <summary>방어구</summary>
    Armor = 1,
    /// <summary>액세서리</summary>
    Accessory = 2,
    /// <summary>부츠</summary>
    Boots = 3
}

/// <summary>
/// 인벤토리 시스템을 관리하는 클래스
/// 아이템 관리, 장비 장착, 합성 등을 처리합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class InventorySystem : MonoBehaviour
{
    private static InventorySystem _instance;
    
    /// <summary>
    /// InventorySystem의 싱글톤 인스턴스
    /// </summary>
    public static InventorySystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("InventorySystem");
                _instance = go.AddComponent<InventorySystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    /// <summary>
    /// ServiceLocator를 통한 의존성 주입
    /// </summary>
    private void InjectDependencies()
    {
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 의존성 주입
        InjectDependencies();
    }

    // ========== 아이템 관리 ==========
    
    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    /// <param name="item">추가할 아이템</param>
    /// <returns>성공 여부</returns>
    public bool AddItem(ItemData item)
    {
        // 인벤토리 용량 확인
        int totalItems = _gameState.Inventory.items.Count + _gameState.Inventory.equipment.Count;
        if (totalItems >= GameConfig.MaxInventorySlots)
        {
            _logger.Warn("인벤토리가 가득 찼습니다.");
            return false;
        }
        
        // 기존에 동일한 아이템이 있는지 확인
        ItemData? existingItem = FindItem(item.id, item.grade);
        
        if (existingItem != null)
        {
            // 수량 증가
            var inventory = _gameState.Inventory;
            int index = inventory.items.FindIndex(x => x.id == item.id && x.grade == item.grade);
            if (index >= 0)
            {
                ItemData existing = inventory.items[index];
                existing.count += item.count;
                inventory.items[index] = existing;
                _logger.Debug($"아이템 수량 증가: {item.name} x{item.count}");
            }
            _gameState.Inventory = inventory;
        }
        else
        {
            // 새 아이템 추가
            var inv = _gameState.Inventory;
            inv.items.Add(item);
            _gameState.Inventory = inv;
            _logger.Debug($"아이템 추가: {item.name}");
        }
        
        // 발견 아이템 등록
        var inventory2 = _gameState.Inventory;
        if (!inventory2.discoveredItems.Contains(item.id))
        {
            inventory2.discoveredItems.Add(item.id);
            var stats = _gameState.Stats;
            stats.totalItemsDiscovered++;
            _gameState.Stats = stats;
            _gameState.Inventory = inventory2;
            _eventBus.Emit(GameEvents.ITEM_DISCOVERED);
        }
        
        _eventBus.Emit(GameEvents.ITEM_ACQUIRED);
        
        return true;
    }

    /// <summary>
    /// 인벤토리에서 아이템 제거
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이템 등급</param>
    /// <param name="quantity">제거할 수량 (기본 1)</param>
    /// <returns>성공 여부</returns>
    public bool RemoveItem(string itemId, int grade, int quantity = 1)
    {
        var inventory = _gameState.Inventory;
        int index = inventory.items.FindIndex(x => x.id == itemId && x.grade == grade);
        
        if (index < 0)
        {
            _logger.Warn($"아이템을 찾을 수 없습니다: {itemId}");
            return false;
        }
        
        ItemData item = inventory.items[index];
        
        // 카운트가 0 이하면 제거만 하고 리턴 (음수 방지)
        if (item.count <= 0)
        {
            inventory.items.RemoveAt(index);
            _gameState.Inventory = inventory;
            _logger.Warn($"아이템 카운트 0 이하: {itemId}, 제거만 수행");
            return false;
        }
        
        if (item.count > quantity)
        {
            item.count -= quantity;
            ItemData updated = item;
            inventory.items[index] = updated;
        }
        else
        {
            inventory.items.RemoveAt(index);
        }
        
        _gameState.Inventory = inventory;
        _logger.Debug($"아이템 제거: {item.name} x{quantity}");
        
        return true;
    }

    /// <summary>
    /// 인벤토리에서 아이템 찾기
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이템 등급</param>
    /// <returns>찾은 아이템, 없으면 null</returns>
    public ItemData? FindItem(string itemId, int grade)
    {
        return _gameState.Inventory.items.Find(x => x.id == itemId && x.grade == grade);
    }

    /// <summary>
    /// 인벤토리에 특정 아이템이 있는지 확인
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이템 등급</param>
    /// <param name="requiredQuantity">필요한 수량</param>
    /// <returns>충분한 수량이 있으면 true</returns>
    public bool HasItem(string itemId, int grade, int requiredQuantity = 1)
    {
        ItemData? item = FindItem(itemId, grade);
        
        return item != null && item.Value.count >= requiredQuantity;
    }

    /// <summary>
    /// 인벤토리 정리 (수량 0인 아이템 제거)
    /// </summary>
    public void CompactInventory()
    {
        var inventory = _gameState.Inventory;
        inventory.items.RemoveAll(x => x.count <= 0);
        _gameState.Inventory = inventory;
    }

    // ========== 장비 관리 ==========
    
    /// <summary>
    /// 장비 장착
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이탬 등급</param>
    /// <returns>성공 여부</returns>
    public bool EquipItem(string itemId, int grade)
    {
        // 아이템 찾기
        ItemData? item = FindItem(itemId, grade);
        
        if (item == null)
        {
            _logger.Warn($"장비할 아이템을 찾을 수 없습니다: {itemId}");
            return false;
        }
        
        // 장비 슬롯 결정: item.type을 직접 사용 (Web 버전과 동일)
        // type: "weapon", "armor", "accessory", "boots" 등
        EquipmentSlot slot = GetSlotFromItemType(item.Value.type);
        
        // 기존 장비 해제
        UnequipItem(slot);
        
        // 인벤토리에서 제거 (수량 1 감소)
        RemoveItem(itemId, grade, 1);
        
        // 장비 슬롯에 추가
        EquipmentData equipment = new EquipmentData
        {
            id = itemId,
            name = item.Value.name,
            grade = grade,
            slot = (int)slot,
            attackBonus = CalculateEquipmentBonus(grade, "attack"),
            defenseBonus = CalculateEquipmentBonus(grade, "defense"),
            healthBonus = CalculateEquipmentBonus(grade, "health")
        };
        
        var inventory = _gameState.Inventory;
        inventory.equipment.Add(equipment);
        _gameState.Inventory = inventory;
        
        _logger.Info($"장비 장착: {item.Value.name} ({slot})");
        
        // 스탯 업데이트
        UpdateStatsBonus();
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.ITEM_EQUIPPED);
        _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 장비 해제
    /// </summary>
    /// <param name="slot">장비 슬롯</param>
    /// <returns>성공 여부</returns>
    public bool UnequipItem(EquipmentSlot slot)
    {
        var inventory = _gameState.Inventory;
        int index = inventory.equipment.FindIndex(x => x.slot == (int)slot);
        
        if (index < 0)
        {
            return false; // 장착된 장비 없음
        }
        
        EquipmentData equipment = inventory.equipment[index];
        
        // 인벤토리로 반환
        ItemData item = new ItemData
        {
            id = equipment.id,
            name = equipment.name,
            grade = equipment.grade,
            count = 1
        };
        
        inventory.items.Add(item);
        inventory.equipment.RemoveAt(index);
        _gameState.Inventory = inventory;
        
        _logger.Debug($"장비 해제: {equipment.name} ({slot})");
        
        // 스탯 업데이트
        UpdateStatsBonus();
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.ITEM_UNEQUIPPED);
        _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 현재 장착된 장비 가져오기
    /// </summary>
    /// <param name="slot">장비 슬롯</param>
    /// <returns>장비 데이터, 없으면 null</returns>
    public EquipmentData? GetEquippedItem(EquipmentSlot slot)
    {
        return _gameState.Inventory.equipment.Find(x => x.slot == (int)slot);
    }

    /// <summary>
    /// 아이템 타입으로 장비 슬롯 결정 (Web 버전과 동일)
    /// </summary>
    private EquipmentSlot GetSlotFromItemType(string itemType)
    {
        // item.type을 그대로 사용: "weapon", "armor", "accessory", "boots"
        switch (itemType?.ToLower())
        {
            case "weapon":
                return EquipmentSlot.Weapon;
            case "armor":
                return EquipmentSlot.Armor;
            case "boots":
                return EquipmentSlot.Boots;
            case "accessory":
            default:
                return EquipmentSlot.Accessory;
        }
    }

    /// <summary>
    /// 장비 보너스 계산
    /// </summary>
    private float CalculateEquipmentBonus(int grade, string statType)
    {
        float baseValue = 10f;
        float gradeMultiplier = 1f + grade * 0.5f;
        
        switch (statType)
        {
            case "attack":
                return baseValue * gradeMultiplier * 2f;
            case "defense":
                return baseValue * gradeMultiplier * 1.5f;
            case "health":
                return baseValue * gradeMultiplier * 10f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 장비 보너스로 스탯 업데이트
    /// </summary>
    private void UpdateStatsBonus()
    {
        float totalAttackBonus = 0f;
        float totalDefenseBonus = 0f;
        float totalHealthBonus = 0f;
        
        foreach (var equipment in _gameState.Inventory.equipment)
        {
            totalAttackBonus += equipment.attackBonus;
            totalDefenseBonus += equipment.defenseBonus;
            totalHealthBonus += equipment.healthBonus;
        }
        
        // 플레이어 기본 스탯에 보너스 추가 (GameState.GetTotalAttack 등에서 계산됨)
        // 여기서는 추가 보너스가 필요한 경우만 처리
        
        _logger.Debug($"장비 보너스 업데이트: 공격력 +{totalAttackBonus}, 방어력 +{totalDefenseBonus}, 체력 +{totalHealthBonus}");
    }

    // ========== 합성 시스템 ==========
    
    /// <summary>
    /// 아이템 합성
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이템 등급</param>
    /// <returns>성공 여부</returns>
    public bool Synthesize(string itemId, int grade)
    {
        // 필요한 수량 확인 (5개)
        if (!HasItem(itemId, grade, GameConfig.SynthesisRequiredCount))
        {
            _logger.Warn($"합성 불가 - 아이템 수량 부족: {itemId} (필요: {GameConfig.SynthesisRequiredCount})");
            return false;
        }
        
        // 최대 등급 확인
        int maxGrade = GetMaxGrade(itemId);
        if (grade >= maxGrade)
        {
            _logger.Warn($"최대 등급 도달: {itemId} (최대: {maxGrade})");
            return false;
        }
        
        // 아이템 제거 (5개)
        for (int i = 0; i < GameConfig.SynthesisRequiredCount; i++)
        {
            RemoveItem(itemId, grade);
        }
        
        // 다음 등급 아이템 생성
        string nextItemId = GetNextGradeItemId(itemId, grade + 1);
        string nextItemName = GetNextGradeItemName(itemId, grade + 1);
        
        ItemData synthesizedItem = new ItemData
        {
            id = nextItemId,
            name = nextItemName,
            grade = grade + 1,
            count = 1
        };
        
        // 인벤토리에 추가
        var inventory = _gameState.Inventory;
        inventory.items.Add(synthesizedItem);
        _gameState.Inventory = inventory;
        
        _logger.Info($"합성 성공: {synthesizedItem.name}");
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.ITEM_SYNTHESIZED);
        _eventBus.Emit(GameEvents.ITEM_ACQUIRED);
        
        // 연쇄 합성 확인
        CheckChainSynthesis(nextItemId, grade + 1);
        
        return true;
    }

    /// <summary>
    /// 연쇄 합성 확인 (합성 결과가 또 다른 합성 조건을 만족하는지)
    /// </summary>
    private void CheckChainSynthesis(string itemId, int grade)
    {
        if (HasItem(itemId, grade, GameConfig.SynthesisRequiredCount))
        {
            _logger.Debug($"연쇄 합성 감지: {itemId}");
            Synthesize(itemId, grade);
        }
    }

    /// <summary>
    /// 일괄 합성 (인벤토리 전체 스캔)
    /// </summary>
    /// <returns>합성된 아이템 수</returns>
    public int SynthesizeAll()
    {
        int synthesizedCount = 0;
        
        // 등급별로 그룹화
        var groups = new Dictionary<string, List<ItemData>>();
        
        foreach (var item in _gameState.Inventory.items)
        {
            string key = $"{item.id}_g{item.grade}";
            if (!groups.ContainsKey(key))
            {
                groups[key] = new List<ItemData>();
            }
            groups[key].Add(item);
        }
        
        // 각 그룹별로 합성 시도
        foreach (var kvp in groups)
        {
            int totalQuantity = 0;
            foreach (var item in kvp.Value)
            {
                totalQuantity += item.count;
            }
            
            if (totalQuantity >= GameConfig.SynthesisRequiredCount)
            {
                ItemData firstItem = kvp.Value[0];
                int maxGrade = GetMaxGrade(firstItem.id);
                
                if (firstItem.grade < maxGrade)
                {
                    while (HasItem(firstItem.id, firstItem.grade, GameConfig.SynthesisRequiredCount))
                    {
                        Synthesize(firstItem.id, firstItem.grade);
                        synthesizedCount++;
                    }
                }
            }
        }
        
        if (synthesizedCount > 0)
        {
            _logger.Info($"일괄 합성 완료: {synthesizedCount}회");
        }
        
        return synthesizedCount;
    }

    /// <summary>
    /// 아이템 최대 등급 가져오기
    /// </summary>
    private int GetMaxGrade(string itemId)
    {
        // 무기: 15, 그 외: 10
        if (itemId.Contains("sword") || itemId.Contains("weapon"))
            return 15;
        return 10;
    }

    /// <summary>
    /// 다음 등급 아이템 ID 생성 (결정론적 해시 기반)
    /// 동일한 입력에 대해 항상 동일한 ID를 반환하여 합성 시스템의 일관성 보장
    /// </summary>
    private string GetNextGradeItemId(string itemId, int newGrade)
    {
        // 해시 기반 결정론적 ID 생성
        return $"{itemId}_grade{newGrade}_{GetStableHash(itemId, newGrade):D4}";
    }

    /// <summary>
    /// 안정적인 해시값 생성 (동일 입력 → 동일 출력 보장)
    /// </summary>
    private int GetStableHash(string itemId, int grade)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + itemId.GetHashCode();
            hash = hash * 31 + grade;
            return Math.Abs(hash) % 10000;
        }
    }

    /// <summary>
    /// 다음 등급 아이템 이름 생성
    /// </summary>
    private string GetNextGradeItemName(string itemId, int newGrade)
    {
        string baseName = itemId.Split('_')[0];
        string prefix = GameConfig.GradePrefixes[Mathf.Min(newGrade, GameConfig.GradePrefixes.Length - 1)];
        return prefix + baseName;
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 인벤토리 슬롯 수 가져오기
    /// </summary>
    public int GetInventorySlotCount()
    {
        return _gameState.Inventory.items.Count;
    }

    /// <summary>
    /// 인벤토리 최대 슬롯 수
    /// </summary>
    public int GetMaxInventorySlots()
    {
        return GameConfig.MaxInventorySlots;
    }

    /// <summary>
    /// 발견한 아이템 수
    /// </summary>
    public int GetDiscoveredItemCount()
    {
        return _gameState.Inventory.discoveredItems.Count;
    }
}
