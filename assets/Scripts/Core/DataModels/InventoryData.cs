using System;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 데이터
/// </summary>
[Serializable]
public class InventoryData
{
    public List<ItemData> items = new List<ItemData>();
    public List<EquipmentData> equipment = new List<EquipmentData>();
    public List<string> discoveredItems = new List<string>();
    public int gold = 0;
    public int gems = 0;

    public void Reset()
    {
        items.Clear();
        equipment.Clear();
        discoveredItems.Clear();
        gold = 0;
        gems = 0;
    }
}

/// <summary>
/// 아이템 데이터
/// </summary>
[Serializable]
public struct ItemData
{
    public string id;
    public string name;
    public int grade;
    public int count;      // 보유 수량 (0 이면 잠금)
    public int rarity;     // 0:common, 1:rare, 2:epic, 3:legendary, 4:mythic
    public string type;    // weapon, armor, boots, accessory
    public int attackBonus;
    public int defenseBonus;
    public int healthBonus;
}

/// <summary>
/// 장비 데이터
/// </summary>
[Serializable]
public struct EquipmentData
{
    public string id;
    public string name;
    public int grade;      // 티어 등급 (1-5: Bronze/Iron/Steel/Gold/Mythril)
    public int rarity;    // 희귀도 (0-4: common/rare/epic/legendary/mythic)
    public int slot;
    public float attackBonus;
    public float defenseBonus;
    public float healthBonus;
}
