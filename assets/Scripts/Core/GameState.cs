using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 상태를 관리하는 싱글톤 클래스
/// SRP 준수: GameState는 상태 관리만 담당, 데이터 모델은 별도 파일에 분리
/// DIP 준수: IGameState 인터페이스 구현
/// </summary>
public class GameState : MonoBehaviour, IGameState
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

    // ========== IGameState 인터페이스 구현 ==========
    
    PlayerData IGameState.Player { get => player; set => player = value; }
    StageData IGameState.Stage { get => stage; set => stage = value; }
    CombatPhaseData IGameState.CombatPhase { get => combatPhase; set => combatPhase = value; }
    InventoryData IGameState.Inventory { get => inventory; set => inventory = value; }
    SettingsData IGameState.Settings { get => settings; set => settings = value; }
    TutorialData IGameState.Tutorial { get => tutorial; set => tutorial = value; }
    DailyMissionData IGameState.DailyMissions { get => dailyMissions; set => dailyMissions = value; }
    RebirthData IGameState.Rebirth { get => rebirth; set => rebirth = value; }
    StatsData IGameState.Stats { get => stats; set => stats = value; }
    GemUpgradeData IGameState.GemUpgrades { get => gemUpgrades; set => gemUpgrades = value; }

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
