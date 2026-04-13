using UnityEngine;

/// <summary>
/// 플레이어 스탯 계산기 - 모든 스탯 계산을 담당 (SRP 준수)
/// GameState에서 계산 로직을 분리
/// </summary>
public static class StatCalculator
{
    /// <summary>
    /// 총 공격력 계산 (기본 + 장비 + 버프)
    /// </summary>
    public static float CalculateTotalAttack(PlayerData player, InventoryData inventory, GemUpgradeData gemUpgrades, RebirthData rebirth)
    {
        float total = player.attack;
        
        // 장비 공격력 추가
        foreach (var equip in inventory.equipment)
        {
            total += equip.attackBonus;
        }
        
        // 보석 업그레이드 보너스
        total *= (1 + gemUpgrades.statBonusLevel * GameConfig.StatBonusPerLevel);
        
        // 환생 보너스
        total *= (1 + rebirth.rebirthCount * 0.1f);
        
        return total;
    }

    /// <summary>
    /// 총 방어력 계산
    /// </summary>
    public static float CalculateTotalDefense(PlayerData player, InventoryData inventory, GemUpgradeData gemUpgrades, RebirthData rebirth)
    {
        float total = player.defense;
        
        foreach (var equip in inventory.equipment)
        {
            total += equip.defenseBonus;
        }
        
        total *= (1 + gemUpgrades.statBonusLevel * GameConfig.StatBonusPerLevel);
        total *= (1 + rebirth.rebirthCount * 0.1f);
        
        return total;
    }

    /// <summary>
    /// 총 체력 계산
    /// </summary>
    public static float CalculateTotalHealth(PlayerData player, InventoryData inventory, GemUpgradeData gemUpgrades, RebirthData rebirth)
    {
        float total = player.health;
        
        foreach (var equip in inventory.equipment)
        {
            total += equip.healthBonus;
        }
        
        total *= (1 + gemUpgrades.statBonusLevel * GameConfig.StatBonusPerLevel);
        total *= (1 + rebirth.rebirthCount * 0.1f);
        
        return total;
    }

    /// <summary>
    /// 레벨업에 필요한 경험치 계산
    /// </summary>
    public static long CalculateExpToNextLevel(int playerLevel)
    {
        return (long)(GameConfig.ExpToLevelUp * Mathf.Pow(GameConfig.ExpMultiplier, playerLevel - 1));
    }

    /// <summary>
    /// 오프라인 보상 배율 계산 (보석 업그레이드 적용)
    /// </summary>
    public static float CalculateOfflineRewardMultiplier(GemUpgradeData gemUpgrades)
    {
        return GameConfig.OfflineRewardMultiplier * (1 + gemUpgrades.offlineRewardLevel * GameConfig.OfflineRewardBonusPerLevel);
    }

    /// <summary>
    /// 자동 전투 데미지 배율 계산
    /// </summary>
    public static float CalculateAutoBattleDamageMultiplier(GemUpgradeData gemUpgrades)
    {
        float bonus = Mathf.Min(gemUpgrades.autoBattleLevel * GameConfig.AutoBattleBonusPerLevel, 1f);
        return 1 + bonus;
    }

    /// <summary>
    /// 치명타 피해 배율 계산
    /// </summary>
    public static float CalculateCritDamageMultiplier(GemUpgradeData gemUpgrades)
    {
        return 1.5f + (gemUpgrades.critDamageLevel * GameConfig.CritDamageBonusPerLevel);
    }

    /// <summary>
    /// 드롭 확률 테이블 계산 (보석 업그레이드 적용)
    /// </summary>
    public static float[] CalculateDropRates(GemUpgradeData gemUpgrades)
    {
        float[] baseRates = new float[GameConfig.DropRates.Length];
        System.Array.Copy(GameConfig.DropRates, baseRates, GameConfig.DropRates.Length);
        
        if (gemUpgrades.dropRateLevel > 0)
        {
            // 고레어 아이템 확률 증가, 저레어 아이템 확률 감소
            float bonusPerLevel = 0.01f; // 레벨당 1% 재분배
            
            // 일반 아이템 확률 감소
            baseRates[0] = Mathf.Max(0.3f, baseRates[0] - (gemUpgrades.dropRateLevel * bonusPerLevel * 4));
            
            // 고급 아이템 확률 증가
            baseRates[1] = baseRates[1] + (gemUpgrades.dropRateLevel * bonusPerLevel);
            
            // 희귀 아이템 확률 증가
            baseRates[2] = baseRates[2] + (gemUpgrades.dropRateLevel * bonusPerLevel);
            
            // 영웅 아이템 확률 증가
            baseRates[3] = baseRates[3] + (gemUpgrades.dropRateLevel * bonusPerLevel);
            
            // 전설 아이템 확률 증가
            baseRates[4] = baseRates[4] + (gemUpgrades.dropRateLevel * bonusPerLevel);
        }
        
        return baseRates;
    }
}
