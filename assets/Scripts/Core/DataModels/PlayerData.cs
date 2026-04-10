using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 기본 데이터
/// SRP 준수: 플레이어 관련 데이터만 담당
/// </summary>
[Serializable]
public class PlayerData
{
    public int level = 1;
    public long experience = 0;
    public float currentHP = 100;
    public float maxHP = 100;
    public float attack = 15;
    public float defense = 8;
    public float health = 200;
    public float speed = 200;
    public float critChance = 0.05f;
    public float critDamage = 1.5f;
    public long gold = 0;
    public int gems = 0;
    public int rebirthCount = 0;
    public int statPoints = 0;
    
    // 골드 업그레이드
    public SerializableDictionary<string, int> goldUpgrades = new SerializableDictionary<string, int>();
    
    // 스탯 업그레이드
    public SerializableDictionary<string, int> statUpgrades = new SerializableDictionary<string, int>();

    public void Initialize()
    {
        level = 1;
        experience = 0;
        currentHP = 100;
        maxHP = 100;
        attack = 15;
        defense = 8;
        health = 200;
        speed = 200;
        critChance = 0.05f;
        critDamage = 1.5f;
        gold = 0;
        gems = 0;
        rebirthCount = 0;
        statPoints = 0;
        
        // 골드 업그레이드 초기화
        goldUpgrades = new SerializableDictionary<string, int>();
        goldUpgrades.Add("attack", 0);
        goldUpgrades.Add("defense", 0);
        goldUpgrades.Add("hp", 0);
        goldUpgrades.Add("hpRegen", 0);
        goldUpgrades.Add("attackSpeed", 0);
        goldUpgrades.Add("critChance", 0);
        goldUpgrades.Add("critDamage", 0);
        goldUpgrades.Add("decisiveChance", 0);
        goldUpgrades.Add("decisiveDamage", 0);
        goldUpgrades.Add("goldBonus", 0);
        goldUpgrades.Add("expBonus", 0);
        
        // 스탯 업그레이드 초기화
        statUpgrades = new SerializableDictionary<string, int>();
        statUpgrades.Add("attack", 0);
        statUpgrades.Add("defense", 0);
        statUpgrades.Add("hp", 0);
        statUpgrades.Add("hpRegen", 0);
        statUpgrades.Add("attackSpeed", 0);
        statUpgrades.Add("critChance", 0);
        statUpgrades.Add("critDamage", 0);
    }

    public void ResetForRebirth()
    {
        level = 1;
        experience = 0;
        currentHP = maxHP;
        gold = 0;
        attack = 15;
        defense = 8;
        health = 200;
        rebirthCount++;
    }
}
