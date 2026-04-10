using System;

/// <summary>
/// 환생 데이터
/// </summary>
[Serializable]
public class RebirthData
{
    public int rebirthCount = 0;
    public int bonusPoints = 0;
    public int minLevel = 50;
    
    public SerializableDictionary<string, int> upgrades = new SerializableDictionary<string, int>();
    
    public void Initialize()
    {
        rebirthCount = 0;
        bonusPoints = 0;
        minLevel = 50;
        
        upgrades = new SerializableDictionary<string, int>();
        upgrades.Add("dailyMissionBonus", 0);
        upgrades.Add("goldDouble", 0);
        upgrades.Add("dropRateIncrease", 0);
        upgrades.Add("expDouble", 0);
        upgrades.Add("offlineBonus", 0);
        upgrades.Add("bossGoldBonus", 0);
        upgrades.Add("synthesisMaster", 0);
        upgrades.Add("stageSkip", 0);
        upgrades.Add("upgradeDiscount", 0);
        upgrades.Add("expTriple", 0);
        upgrades.Add("goldTriple", 0);
    }
}

/// <summary>
/// 게임 통계 데이터
/// </summary>
[Serializable]
public class StatsData
{
    public float totalPlayTime;
    public int totalLevelUps;
    public int totalRebirths;
    public int totalKills;
    public int totalBossKills;
    public long totalGoldEarned;
    public int totalItemsDiscovered;
}

/// <summary>
/// 보석 업그레이드 데이터
/// </summary>
[Serializable]
public class GemUpgradeData
{
    public int offlineRewardLevel;
    public int critDamageLevel;
    public int autoBattleLevel;
    public int rebirthBonusLevel;
    public int dropRateLevel;
    public int statBonusLevel;
}
