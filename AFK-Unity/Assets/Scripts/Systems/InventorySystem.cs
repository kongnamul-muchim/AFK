using UnityEngine;
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
    Accessory = 2
}

/// <summary>
/// 인벤토리 시스템을 관리하는 클래스
/// 아이템 관리, 장비 장착, 합성 등을 처리합니다.
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

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 아이템 관리 ==========
    
    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    /// <param name="item">추가할 아이템</param>
    /// <returns>성공 여부</returns>
    public bool AddItem(ItemData item)
    {
        GameState state = GameState.Instance;
        
        // 인벤토리 용량 확인
        int totalItems = state.inventory.items.Count + state.inventory.equipment.Count;
        if (totalItems >= GameConfig.MaxInventorySlots)
        {
            GameLogger.Warn("인벤토리가 가득 찼습니다.");
            return false;
        }
        
        // 기존에 동일한 아이템이 있는지 확인
        ItemData? existingItem = FindItem(item.id, item.grade);
        
        if (existingItem != null)
        {
            // 수량 증가
            int index = state.inventory.items.FindIndex(x => x.id == item.id && x.grade == item.grade);
            if (index >= 0)
            {
                ItemData existing = state.inventory.items[index];
                existing.quantity += item.quantity;
                state.inventory.items[index] = existing;
                GameLogger.DebugLog($"아이템 수량 증가: {item.name} x{item.quantity}");
            }
        }
        else
        {
            // 새 아이템 추가
            state.inventory.items.Add(item);
            GameLogger.DebugLog($"아이템 추가: {item.name}");
        }
        
        // 발견 아이템 등록
        if (!state.inventory.discoveredItems.Contains(item.id))
        {
            state.inventory.discoveredItems.Add(item.id);
            state.stats.totalItemsDiscovered++;
            EventBus.Instance.Emit(GameEvents.ITEM_DISCOVERED);
        }
        
        EventBus.Instance.Emit(GameEvents.ITEM_ACQUIRED);
        
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
        GameState state = GameState.Instance;
        
        int index = state.inventory.items.FindIndex(x => x.id == itemId && x.grade == grade);
        
        if (index < 0)
        {
            GameLogger.Warn($"아이템을 찾을 수 없습니다: {itemId}");
            return false;
        }
        
        ItemData item = state.inventory.items[index];
        
        if (item.quantity > quantity)
        {
            item.quantity -= quantity;
            ItemData updated = item;
            state.inventory.items[index] = updated;
        }
        else
        {
            state.inventory.items.RemoveAt(index);
        }
        
        GameLogger.DebugLog($"아이템 제거: {item.name} x{quantity}");
        
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
        GameState state = GameState.Instance;
        
        return state.inventory.items.Find(x => x.id == itemId && x.grade == grade);
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
        GameState state = GameState.Instance;
        
        ItemData? item = FindItem(itemId, grade);
        
        return item != null && item.Value.quantity >= requiredQuantity;
    }

    /// <summary>
    /// 인벤토리 정리 (수량 0인 아이템 제거)
    /// </summary>
    public void CompactInventory()
    {
        GameState state = GameState.Instance;
        
        state.inventory.items.RemoveAll(x => x.quantity <= 0);
    }

    // ========== 장비 관리 ==========
    
    /// <summary>
    /// 장비 장착
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <param name="grade">아이템 등급</param>
    /// <returns>성공 여부</returns>
    public bool EquipItem(string itemId, int grade)
    {
        GameState state = GameState.Instance;
        
        // 아이템 찾기
        ItemData? item = FindItem(itemId, grade);
        
        if (item == null)
        {
            GameLogger.Warn($"장비할 아이템을 찾을 수 없습니다: {itemId}");
            return false;
        }
        
        // 장비 슬롯 확인
        EquipmentSlot slot = GetEquipmentSlot(itemId);
        
        // 기존 장비 해제
        UnequipItem(slot);
        
        // 인벤토리에서 제거
        RemoveItem(itemId, grade);
        
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
        
        state.inventory.equipment.Add(equipment);
        
        GameLogger.Info($"장비 장착: {item.Value.name} ({slot})");
        
        // 스탯 업데이트
        UpdateStatsBonus();
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.ITEM_EQUIPPED);
        EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 장비 해제
    /// </summary>
    /// <param name="slot">장비 슬롯</param>
    /// <returns>성공 여부</returns>
    public bool UnequipItem(EquipmentSlot slot)
    {
        GameState state = GameState.Instance;
        
        int index = state.inventory.equipment.FindIndex(x => x.slot == (int)slot);
        
        if (index < 0)
        {
            return false; // 장착된 장비 없음
        }
        
        EquipmentData equipment = state.inventory.equipment[index];
        
        // 인벤토리로 반환
        ItemData item = new ItemData
        {
            id = equipment.id,
            name = equipment.name,
            grade = equipment.grade,
            quantity = 1
        };
        
        state.inventory.items.Add(item);
        state.inventory.equipment.RemoveAt(index);
        
        GameLogger.DebugLog($"장비 해제: {equipment.name} ({slot})");
        
        // 스탯 업데이트
        UpdateStatsBonus();
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.ITEM_UNEQUIPPED);
        EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 현재 장착된 장비 가져오기
    /// </summary>
    /// <param name="slot">장비 슬롯</param>
    /// <returns>장비 데이터, 없으면 null</returns>
    public EquipmentData? GetEquippedItem(EquipmentSlot slot)
    {
        GameState state = GameState.Instance;
        
        return state.inventory.equipment.Find(x => x.slot == (int)slot);
    }

    /// <summary>
    /// 장비 슬롯 판별
    /// </summary>
    private EquipmentSlot GetEquipmentSlot(string itemId)
    {
        if (itemId.Contains("sword") || itemId.Contains("weapon"))
            return EquipmentSlot.Weapon;
        if (itemId.Contains("armor"))
            return EquipmentSlot.Armor;
        return EquipmentSlot.Accessory;
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
        GameState state = GameState.Instance;
        
        float totalAttackBonus = 0f;
        float totalDefenseBonus = 0f;
        float totalHealthBonus = 0f;
        
        foreach (var equipment in state.inventory.equipment)
        {
            totalAttackBonus += equipment.attackBonus;
            totalDefenseBonus += equipment.defenseBonus;
            totalHealthBonus += equipment.healthBonus;
        }
        
        // 플레이어 기본 스탯에 보너스 추가 (GameState.GetTotalAttack 등에서 계산됨)
        // 여기서는 추가 보너스가 필요한 경우만 처리
        
        GameLogger.DebugLog($"장비 보너스 업데이트: 공격력 +{totalAttackBonus}, 방어력 +{totalDefenseBonus}, 체력 +{totalHealthBonus}");
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
        GameState state = GameState.Instance;
        
        // 필요한 수량 확인 (5개)
        if (!HasItem(itemId, grade, GameConfig.SynthesisRequiredCount))
        {
            GameLogger.Warn($"합성 불가 - 아이템 수량 부족: {itemId} (필요: {GameConfig.SynthesisRequiredCount})");
            return false;
        }
        
        // 최대 등급 확인
        int maxGrade = GetMaxGrade(itemId);
        if (grade >= maxGrade)
        {
            GameLogger.Warn($"최대 등급 도달: {itemId} (최대: {maxGrade})");
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
            quantity = 1
        };
        
        // 인벤토리에 추가
        state.inventory.items.Add(synthesizedItem);
        
        GameLogger.Info($"합성 성공: {synthesizedItem.name}");
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.ITEM_SYNTHESIZED);
        EventBus.Instance.Emit(GameEvents.ITEM_ACQUIRED);
        
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
            GameLogger.DebugLog($"연쇄 합성 감지: {itemId}");
            Synthesize(itemId, grade);
        }
    }

    /// <summary>
    /// 일괄 합성 (인벤토리 전체 스캔)
    /// </summary>
    /// <returns>합성된 아이템 수</returns>
    public int SynthesizeAll()
    {
        GameState state = GameState.Instance;
        int synthesizedCount = 0;
        
        // 등급별로 그룹화
        var groups = new Dictionary<string, List<ItemData>>();
        
        foreach (var item in state.inventory.items)
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
                totalQuantity += item.quantity;
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
            GameLogger.Info($"일괄 합성 완료: {synthesizedCount}회");
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
    /// 다음 등급 아이템 ID 생성
    /// </summary>
    private string GetNextGradeItemId(string itemId, int newGrade)
    {
        // ID에서 등급 부분만 변경
        return itemId.Substring(0, itemId.LastIndexOf('_') + 1) + newGrade + "_" + Random.Range(1000, 9999);
    }

    /// <summary>
    /// 다음 등급 아이템 이름 생성
    /// </summary>
    private string GetNextGradeItemName(string itemId, int newGrade)
    {
        string[] prefixes = new string[] { "일반 ", "고급 ", "희귀 ", "영웅 ", "전설 " };
        string baseName = itemId.Split('_')[0];
        
        string prefix = prefixes[Mathf.Min(newGrade, prefixes.Length - 1)];
        
        return prefix + baseName;
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 인벤토리 슬롯 수 가져오기
    /// </summary>
    public int GetInventorySlotCount()
    {
        GameState state = GameState.Instance;
        return state.inventory.items.Count;
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
        GameState state = GameState.Instance;
        return state.inventory.discoveredItems.Count;
    }
}
