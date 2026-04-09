using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 상태를 관리하는 싱글톤 클래스
/// 모든 게임 데이터는 GameState를 통해 접근하고 관리됩니다.
/// </summary>
public class GameState : MonoBehaviour
{
    private static GameState _instance;
    
    /// <summary>
    /// GameState의 싱글톤 인스턴스
    /// </summary>
    public static GameState Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameState");
                _instance = go.AddComponent<GameState>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 데이터 필드 ==========
    
    /// <summary>
    /// 플레이어 정보 (레벨, 스탯, 재화 등)
    /// </summary>
    public PlayerData player;
    
    /// <summary>
    /// 스테이지 정보 (현재 스테이지, 최대 스테이지, 클리어 여부)
    /// </summary>
    public StageData stage;
    
    /// <summary>
    /// 전투 페이즈 정보 (페이즈 상태, 플레이어/몬스터 상태, 타이머)
    /// </summary>
    public CombatPhaseData combatPhase;
    
    /// <summary>
    /// 인벤토리 정보 (아이템, 장비, 발견한 아이템)
    /// </summary>
    public InventoryData inventory;
    
    /// <summary>
    /// 게임 설정 (사운드 볼륨, 자동 전투 여부 등)
    /// </summary>
    public SettingsData settings;
    
    /// <summary>
    /// 튜토리얼 진행 상태
    /// </summary>
    public TutorialData tutorial;
    
    /// <summary>
    /// 일일/주간 미션 데이터
    /// </summary>
    public DailyMissionData dailyMissions;
    
    /// <summary>
    /// 환생 데이터 (환생 횟수, 보너스)
    /// </summary>
    public RebirthData rebirth;
    
    /// <summary>
    /// 게임 통계 (플레이 시간, 처치 수, 레벨업 횟수 등)
    /// </summary>
    public StatsData stats;
    
    /// <summary>
    /// 보석 업그레이드 데이터
    /// </summary>
    public GemUpgradeData gemUpgrades;

    // ========== MonoBehaviour 라이프사이클 ==========

    private void Awake()
    {
        // 싱글톤 인스턴스 관리
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 초기화 메서드 ==========

    /// <summary>
    /// 새 게임 시작 시 모든 데이터를 초기값으로 설정
    /// </summary>
    public void Initialize()
    {
        player = new PlayerData();
        stage = new StageData();
        combatPhase = new CombatPhaseData();
        inventory = new InventoryData();
        settings = new SettingsData();
        tutorial = new TutorialData();
        dailyMissions = new DailyMissionData();
        rebirth = new RebirthData();
        stats = new StatsData();
        gemUpgrades = new GemUpgradeData();
        
        GameLogger.Info("GameState 초기화 완료");
    }

    /// <summary>
    /// 환생 시 초기화 (일부 데이터는 유지)
    /// </summary>
    public void ResetForRebirth()
    {
        player.ResetForRebirth();
        stage.Reset();
        combatPhase.Reset();
        inventory.Reset();
        dailyMissions.Reset();
        
        // stats와 gemUpgrades는 유지
        
        GameLogger.Info("환생 초기화 완료");
    }

    // ========== 유틸리티 메서드 ==========

    /// <summary>
    /// 현재 플레이어의 총 공격력 계산 (기본 + 장비 + 버프)
    /// </summary>
    public float GetTotalAttack()
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
    /// 현재 플레이어의 총 방어력 계산
    /// </summary>
    public float GetTotalDefense()
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
    /// 현재 플레이어의 총 체력 계산
    /// </summary>
    public float GetTotalHealth()
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
    public long GetExpToNextLevel()
    {
        return (long)(GameConfig.ExpToLevelUp * Mathf.Pow(GameConfig.ExpMultiplier, player.level - 1));
    }

    /// <summary>
    /// 오프라인 보상 배율 계산 (보석 업그레이드 적용)
    /// </summary>
    public float GetOfflineRewardMultiplier()
    {
        return GameConfig.OfflineRewardMultiplier * (1 + gemUpgrades.offlineRewardLevel * GameConfig.OfflineRewardBonusPerLevel);
    }

    /// <summary>
    /// 자동 전투 데미지 배율 계산
    /// </summary>
    public float GetAutoBattleDamageMultiplier()
    {
        float bonus = Mathf.Min(gemUpgrades.autoBattleLevel * GameConfig.AutoBattleBonusPerLevel, 1f);
        return 1 + bonus;
    }

    /// <summary>
    /// 치명타 피해 배율 계산
    /// </summary>
    public float GetCritDamageMultiplier()
    {
        return 1.5f + (gemUpgrades.critDamageLevel * GameConfig.CritDamageBonusPerLevel);
    }

    /// <summary>
    /// 드롭 확률 테이블 가져오기 (보석 업그레이드 적용)
    /// </summary>
    public float[] GetDropRates()
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

// ========== 데이터 구조체들 ==========

/// <summary>
/// 플레이어 기본 데이터
/// </summary>
[System.Serializable]
public struct PlayerData
{
    public int level;
    public long experience;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public float health;
    public float speed;
    public float critChance;
    public float critDamage;
    public long gold;
    public int gems;
    public int rebirthCount;

    public void ResetForRebirth()
    {
        level = 1;
        experience = 0;
        currentHP = maxHP;
        gold = 0;
        attack = 15;
        defense = 8;
        health = 200;
        // rebirthCount는 증가시킴
        rebirthCount++;
    }
}

/// <summary>
/// 스테이지 진행 데이터
/// </summary>
[System.Serializable]
public struct StageData
{
    public int currentStage;
    public int maxStage;
    public bool[] clearedStages;

    public void Reset()
    {
        currentStage = 1;
        // maxStage는 유지 (가장 높은 도달 스테이지)
    }
}

/// <summary>
/// 전투 페이즈 데이터
/// </summary>
[System.Serializable]
public struct CombatPhaseData
{
    public int phase; // 0: 대기, 1: 전투, 2: 보상
    public PlayerCombatState playerState;
    public MonsterData monsterState;
    public float timer;

    public void Reset()
    {
        phase = 0;
        timer = 0;
    }
}

/// <summary>
/// 전투 중 플레이어 상태
/// </summary>
[System.Serializable]
public struct PlayerCombatState
{
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
}

/// <summary>
/// 몬스터 데이터
/// </summary>
[System.Serializable]
public struct MonsterData
{
    public string name;
    public int stage;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public int grade; // 0:일반, 1:고급, 2:희귀, 3:영웅, 4:전설
}

/// <summary>
/// 인벤토리 데이터 (클래스로 변경 - List 직렬화 문제 해결)
/// </summary>
[System.Serializable]
public class InventoryData
{
    public List<ItemData> items = new List<ItemData>();
    public List<EquipmentData> equipment = new List<EquipmentData>();
    public List<string> discoveredItems = new List<string>(); // HashSet 대신 List 사용

    public void Reset()
    {
        items.Clear();
        equipment.Clear();
        // discoveredItems는 유지 (도감용)
    }
}

/// <summary>
/// 아이템 데이터
/// </summary>
[System.Serializable]
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
[System.Serializable]
public struct EquipmentData
{
    public string id;
    public string name;
    public int grade;
    public int slot; // 0:무기, 1:방어구, 2:액세서리
    public float attackBonus;
    public float defenseBonus;
    public float healthBonus;
}

/// <summary>
/// 게임 설정 데이터
/// </summary>
[System.Serializable]
public class SettingsData
{
    public float soundVolume = 1f;
    public float musicVolume = 1f;
    public bool autoBattleEnabled = true;
}

/// <summary>
/// 튜토리얼 진행 데이터
/// </summary>
[System.Serializable]
public class TutorialData
{
    public int currentStep;
    public List<string> completedSteps = new List<string>(); // HashSet 대신 List 사용
}

/// <summary>
/// 일일/주간 미션 데이터
/// </summary>
[System.Serializable]
public class DailyMissionData
{
    public List<MissionData> dailyMissions = new List<MissionData>();
    public List<MissionData> weeklyMissions = new List<MissionData>();
    public System.DateTime lastDailyReset;
    public System.DateTime lastWeeklyReset;

    public void Reset()
    {
        dailyMissions.Clear();
        weeklyMissions.Clear();
    }
}

/// <summary>
/// 미션 데이터
/// </summary>
[System.Serializable]
public struct MissionData
{
    public string id;
    public string description;
    public int targetCount;
    public int currentCount;
    public bool isCompleted;
    public bool isClaimed;
    public int type; // 0:Kill, 1:ClearStage, 2:CollectGold, 3:UpgradeItem, 4:Rebirth
}

/// <summary>
/// 환생 데이터
/// </summary>
[System.Serializable]
public class RebirthData
{
    public int rebirthCount;
    public float totalBonus;
}

/// <summary>
/// 게임 통계 데이터
/// </summary>
[System.Serializable]
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
[System.Serializable]
public class GemUpgradeData
{
    public int offlineRewardLevel;
    public int critDamageLevel;
    public int autoBattleLevel;
    public int rebirthBonusLevel;
    public int dropRateLevel;
    public int statBonusLevel;
}
