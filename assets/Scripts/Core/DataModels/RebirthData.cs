using System;
using System.Collections.Generic;

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
    
    /// <summary>
    /// 클리어한 보스 스테이지 목록 (보석 보상을 위해)
    /// Web 버전의 clearedBossStages: Set<number>와 동일
    /// </summary>
    public List<int> clearedBossStages = new List<int>();
    
    /// <summary>
    /// 보스 스테이지를 클리어했는지 확인
    /// </summary>
    public bool HasClearedBossStage(int stage)
    {
        return clearedBossStages.Contains(stage);
    }
    
    /// <summary>
    /// 보스 스테이지를 클리어한 것으로 표시
    /// </summary>
    public void AddClearedBossStage(int stage)
    {
        if (!clearedBossStages.Contains(stage))
        {
            clearedBossStages.Add(stage);
        }
    }
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

/// <summary>
/// 환생 업그레이드 통합 효과 (Web getCombinedEffects)
/// </summary>
[Serializable]
public struct RebirthEffects
{
    public float missionBonus;
    public float goldMultiplier;
    public float dropRateMultiplier;
    public float expMultiplier;
    public float offlineMultiplier;
    public float bossGoldMultiplier;
    public float synthesisBonusChance;
    public float stageSkipChance;
    public float upgradeDiscount;

    public static RebirthEffects Default => new RebirthEffects
    {
        goldMultiplier = 1f,
        dropRateMultiplier = 1f,
        expMultiplier = 1f,
        offlineMultiplier = 1f,
        bossGoldMultiplier = 1f
    };
}
