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
    /// <summary>
    /// 이벤트 버스 참조
    /// </summary>
    private IEventBus _eventBusCache;
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
        stage.Reset();
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
    /// Web GameState.performRebirth() 기준:
    /// - 유지: discoveredItems, goldUpgrades, stats, gemUpgrades
    /// - 초기화: player, stage, combatPhase, inventory(발견제외), dailyMissions
    /// </summary>
    public void ResetForRebirth()
    {
        // 발견된 아이템 목록 백업 (유지)
        var discoveredBackup = inventory.discoveredItems;
        
        player.ResetForRebirth();
        stage.Reset();
        combatPhase.Reset();
        inventory.Reset();
        inventory.discoveredItems = discoveredBackup; // 발견 아이템 유지
        dailyMissions.Reset();
        
        // stats와 gemUpgrades는 유지
        
        GameLogger.Info("환생 초기화 완료");
    }

    // ========== 유틸리티 메서드 ==========

    /// <summary>
    /// 현재 플레이어의 총 공격력 계산 (StatCalculator로 위임)
    /// </summary>
    public float GetTotalAttack()
    {
        return StatCalculator.CalculateTotalAttack(player, inventory, gemUpgrades, rebirth);
    }

    /// <summary>
    /// 현재 플레이어의 총 방어력 계산 (StatCalculator로 위임)
    /// </summary>
    public float GetTotalDefense()
    {
        return StatCalculator.CalculateTotalDefense(player, inventory, gemUpgrades, rebirth);
    }
    
    /// <summary>
    /// 경험치 추가 및 레벨업 처리 (Web 버전과 동일)
    /// </summary>
    /// <param name="amount">추가할 경험치량</param>
    /// <returns>레벨업 여부</returns>
    public bool AddExperience(long amount)
    {
        if (player == null) return false;
        
        bool leveledUp = player.AddExperience(amount);
        
        // 경험치 변경 이벤트 (UI 업데이트용)
        if (Bootstrap.Container != null)
            Bootstrap.Container.Resolve<IEventBus>().Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        if (leveledUp)
        {
            // 레벨업 시 파생 스탯 재계산
            player.maxHP = GetTotalHealth();
            player.currentHP = player.maxHP; // HP 완전 회복
            
            // 레벨업 이벤트
            if (Bootstrap.Container != null)
                Bootstrap.Container.Resolve<IEventBus>().Emit(GameEvents.PLAYER_LEVEL_UP);
            Debug.Log($"[GameState] 레벨업! Lv.{player.level}");
        }
        
        return leveledUp;
    }
    
    /// <summary>
    /// 현재 플레이어의 총 체력 계산 (StatCalculator로 위임, hpDouble 버프 적용)
    /// </summary>
    public float GetTotalHealth()
    {
        float baseHealth = StatCalculator.CalculateTotalHealth(player, inventory, gemUpgrades, rebirth);
        
        // hpDouble 버프 적용 (Web 버전과 동일)
        if (DailyMissionSystem.Instance.HasActiveBuff("hpDouble"))
        {
            baseHealth *= 2.0f;
        }
        
        return baseHealth;
    }

    /// <summary>
    /// 레벨업에 필요한 경험치 계산 (StatCalculator로 위임)
    /// </summary>
    public long GetExpToNextLevel()
    {
        return StatCalculator.CalculateExpToNextLevel(player.level);
    }

    /// <summary>
    /// 오프라인 보상 배율 계산 (StatCalculator로 위임)
    /// </summary>
    public float GetOfflineRewardMultiplier()
    {
        return StatCalculator.CalculateOfflineRewardMultiplier(gemUpgrades);
    }

    /// <summary>
    /// 자동 전투 데미지 배율 계산 (StatCalculator로 위임)
    /// </summary>
    public float GetAutoBattleDamageMultiplier()
    {
        return StatCalculator.CalculateAutoBattleDamageMultiplier(gemUpgrades);
    }

    /// <summary>
    /// 치명타 피해 배율 계산 (StatCalculator로 위임)
    /// </summary>
    public float GetCritDamageMultiplier()
    {
        return StatCalculator.CalculateCritDamageMultiplier(gemUpgrades);
    }

    /// <summary>
    /// 드롭 확률 테이블 가져오기 (StatCalculator로 위임)
    /// </summary>
    public float[] GetDropRates()
    {
        return StatCalculator.CalculateDropRates(gemUpgrades);
    }
    
    /// <summary>
    /// 보스 스테이지 첫 클리어 시 보석 보상 계산 (Web 버전과 동일)
    /// 10층: 5개, 20층: 10개, 30층: 15개, ... 100층: 50개, 이후 50개 고정
    /// </summary>
    /// <param name="bossLevel">보스 레벨 (1=10층, 2=20층, ...)</param>
    /// <returns>보석 보상량</returns>
    public int CalculateBossGemReward(int bossLevel)
    {
        // 10층 단위 보스 첫 클리어 시 보석 보상 (높은 보상)
        // 10층: 5개, 20층: 10개, 30층: 15개, ... 100층: 50개
        // 100층 이후: 50개 고정
        int baseReward = Mathf.Min(bossLevel * 5, 50);
        return baseReward;
    }
}
