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

    public void Reset()
    {
        items.Clear();
        equipment.Clear();
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
    public int quantity;
}

/// <summary>
/// 장비 데이터
/// </summary>
[Serializable]
public struct EquipmentData
{
    public string id;
    public string name;
    public int grade;
    public int slot;
    public float attackBonus;
    public float defenseBonus;
    public float healthBonus;
}
