using UnityEngine;

/// <summary>
/// 환생 시스템을 관리하는 클래스
/// 플레이어의 환생과 보석 업그레이드를 처리합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class RebirthSystem : MonoBehaviour
{
    private static RebirthSystem _instance;
    
    /// <summary>
    /// RebirthSystem의 싱글톤 인스턴스
    /// </summary>
    public static RebirthSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("RebirthSystem");
                _instance = go.AddComponent<RebirthSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    /// <summary>
    /// ServiceLocator를 통한 의존성 주입
    /// </summary>
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;

        if (_gameState == null)
            _gameState = Bootstrap.Container.Resolve<IGameState>();
        if (_eventBus == null)
            _eventBus = Bootstrap.Container.Resolve<IEventBus>();
        if (_logger == null)
            _logger = Bootstrap.Container.Resolve<IGameLogger>();
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
        
        // 의존성 주입
        InjectDependencies();
    }

    // ========== 환생 시스템 ==========
    
    /// <summary>
    /// 환생 가능 여부 확인
    /// </summary>
    /// <returns>환생 가능하면 true</returns>
    public bool CanRebirth()
    {
        // 최소 레벨 확인
        if (_gameState.Player.level < GameConfig.MinRebirthLevel)
        {
            _logger.Warn($"환생 불가 - 최소 레벨 {GameConfig.MinRebirthLevel} 필요 (현재: {_gameState.Player.level})");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 환생 수행 (Web GameState.performRebirth() 기준)
    /// </summary>
    public bool PerformRebirth()
    {
        if (!CanRebirth())
        {
            return false;
        }
        
        _logger.Info($"환생 시작 - {_gameState.Rebirth.rebirthCount + 1}번째 환생");
        
        int bonusPoints = GetBonusPointsPreview();
        
        // 데이터 초기화 (GameState.ResetForRebirth에서 처리)
        // 발견된 아이템, 골드 업그레이드, stats, gemUpgrades는 유지됨
        _gameState.ResetForRebirth();
        
        // 환생 카운트 증가 및 보너스 포인트 추가
        var rebirth = _gameState.Rebirth;
        rebirth.rebirthCount++;
        rebirth.bonusPoints += bonusPoints;
        _gameState.Rebirth = rebirth;
        
        // 통계 업데이트
        var stats = _gameState.Stats;
        stats.totalRebirths++;
        _gameState.Stats = stats;
        
        _logger.Info($"환생 완료 - 보너스 포인트: {bonusPoints}");
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.REBIRTH_PERFORMED);
        _eventBus.Emit(GameEvents.STATS_CHANGED);
        _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 환생 시 얻을 보너스 포인트 미리보기 (Web 버전과 동일한 계산식)
    /// </summary>
    public int GetBonusPointsPreview()
    {
        int playerLevel = _gameState.Player.level;
        int minLevel = GameConfig.MinRebirthLevel;
        int bonusLevel = Mathf.Max(0, playerLevel - minLevel + 1);
        int basePoints = Mathf.FloorToInt(bonusLevel * (1 + bonusLevel * 0.1f));
        
        int gemBonusLevel = _gameState.GemUpgrades.rebirthBonusLevel;
        basePoints += Mathf.Min(gemBonusLevel, 10);
        
        return basePoints;
    }

    /// <summary>
    /// 환생 후 유지되는 데이터 확인
    /// </summary>
    public string GetPreservedDataInfo()
    {
        return $@"
유지되는 데이터:
- 발견한 아이템: {_gameState.Inventory.discoveredItems.Count}개
- 통계: 모든 통계 유지
- 보석 업그레이드: 모든 업그레이드 유지
- 환생 보너스: {_gameState.Rebirth.bonusPoints}포인트

초기화되는 데이터:
- 레벨: {_gameState.Player.level} → 1
- 골드: {_gameState.Player.gold:N0} → {_gameState.Player.gold * GameConfig.RebirthGoldRetention:N0} (10% 유지)
- 스테이지: {_gameState.Stage.currentStage} → 1
- 인벤토리: 초기화
- 장비: 초기화
";
    }

    // ========== 보석 업그레이드 시스템 ==========
    
    /// <summary>
    /// 보석 업그레이드 타입 열거형
    /// </summary>
    public enum GemUpgradeType
    {
        OfflineReward,
        CritDamage,
        AutoBattle,
        RebirthBonus,
        DropRate,
        StatBonus
    }

    /// <summary>
    /// 보석 업그레이드 가능 여부 확인
    /// </summary>
    /// <param name="type">업그레이드 타입</param>
    /// <returns>업그레이드 가능하면 true</returns>
    public bool CanUpgradeGem(GemUpgradeType type)
    {
        int currentLevel = GetGemLevel(type);
        int maxLevel = GetMaxGemLevel(type);
        
        if (currentLevel >= maxLevel)
        {
            _logger.Warn($"최대 레벨 도달: {type}");
            return false;
        }
        
        long cost = GetUpgradeCost(type);
        if (_gameState.Player.gems < cost)
        {
            _logger.Warn($"보석 부족: {_gameState.Player.gems}/{cost}");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 보석 업그레이드 수행
    /// </summary>
    /// <param name="type">업그레이드 타입</param>
    /// <returns>성공 여부</returns>
    public bool UpgradeGem(GemUpgradeType type)
    {
        if (!CanUpgradeGem(type))
        {
            return false;
        }
        
        long cost = GetUpgradeCost(type);
        
        // 보석 차감
        var player = _gameState.Player;
        player.gems -= (int)cost;
        _gameState.Player = player;
        
        // 레벨 증가
        IncreaseGemLevel(type);
        
        _logger.Info($"보석 업그레이드: {type} → 레벨 {GetGemLevel(type)}");
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.GEM_UPGRADED);
        _eventBus.Emit(GameEvents.GEM_CHANGED);
        _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 현재 보석 레벨 가져오기
    /// </summary>
    public int GetGemLevel(GemUpgradeType type)
    {
        switch (type)
        {
            case GemUpgradeType.OfflineReward: return _gameState.GemUpgrades.offlineRewardLevel;
            case GemUpgradeType.CritDamage: return _gameState.GemUpgrades.critDamageLevel;
            case GemUpgradeType.AutoBattle: return _gameState.GemUpgrades.autoBattleLevel;
            case GemUpgradeType.RebirthBonus: return _gameState.GemUpgrades.rebirthBonusLevel;
            case GemUpgradeType.DropRate: return _gameState.GemUpgrades.dropRateLevel;
            case GemUpgradeType.StatBonus: return _gameState.GemUpgrades.statBonusLevel;
            default: return 0;
        }
    }

    /// <summary>
    /// 보석 레벨 증가
    /// </summary>
    private void IncreaseGemLevel(GemUpgradeType type)
    {
        var gemUpgrades = _gameState.GemUpgrades;
        
        switch (type)
        {
            case GemUpgradeType.OfflineReward: gemUpgrades.offlineRewardLevel++; break;
            case GemUpgradeType.CritDamage: gemUpgrades.critDamageLevel++; break;
            case GemUpgradeType.AutoBattle: gemUpgrades.autoBattleLevel++; break;
            case GemUpgradeType.RebirthBonus: gemUpgrades.rebirthBonusLevel++; break;
            case GemUpgradeType.DropRate: gemUpgrades.dropRateLevel++; break;
            case GemUpgradeType.StatBonus: gemUpgrades.statBonusLevel++; break;
        }
        
        _gameState.GemUpgrades = gemUpgrades;
    }

    /// <summary>
    /// 보석 최대 레벨 가져오기
    /// </summary>
    private int GetMaxGemLevel(GemUpgradeType type)
    {
        switch (type)
        {
            case GemUpgradeType.OfflineReward: return int.MaxValue; // 무한
            case GemUpgradeType.CritDamage: return int.MaxValue; // 무한
            case GemUpgradeType.AutoBattle: return GameConfig.AutoBattleMaxLevel; // 50
            case GemUpgradeType.RebirthBonus: return GameConfig.RebirthBonusMaxLevel; // 10
            case GemUpgradeType.DropRate: return GameConfig.DropRateMaxLevel; // 20
            case GemUpgradeType.StatBonus: return int.MaxValue; // 무한
            default: return int.MaxValue;
        }
    }

    /// <summary>
    /// 보석 업그레이드 비용 계산
    /// </summary>
    /// <param name="type">업그레이드 타입</param>
    /// <returns>필요한 보석 수</returns>
    public long GetUpgradeCost(GemUpgradeType type)
    {
        int currentLevel = GetGemLevel(type);
        
        // 기본 비용: 10 * (레벨 + 1)
        long baseCost = 10 * (currentLevel + 1);
        
        // 타입별 보정
        switch (type)
        {
            case GemUpgradeType.OfflineReward: return baseCost;
            case GemUpgradeType.CritDamage: return baseCost;
            case GemUpgradeType.AutoBattle: return baseCost * 2;
            case GemUpgradeType.RebirthBonus: return baseCost * 3;
            case GemUpgradeType.DropRate: return baseCost * 2;
            case GemUpgradeType.StatBonus: return baseCost;
            default: return baseCost;
        }
    }

    /// <summary>
    /// 보석 업그레이드 효과 가져오기
    /// </summary>
    /// <param name="type">업그레이드 타입</param>
    /// <returns>효과 설명</returns>
    public string GetGemEffectDescription(GemUpgradeType type)
    {
        int level = GetGemLevel(type);
        
        switch (type)
        {
            case GemUpgradeType.OfflineReward:
                return $"오프라인 보상 {(level * GameConfig.OfflineRewardBonusPerLevel * 100):F0}% 증가";
            case GemUpgradeType.CritDamage:
                return $"치명타 피해 {(level * GameConfig.CritDamageBonusPerLevel * 100):F0}% 증가";
            case GemUpgradeType.AutoBattle:
                return $"자동 전투 강화 {(level * GameConfig.AutoBattleBonusPerLevel * 100):F0}% 증가 (최대 100%)";
            case GemUpgradeType.RebirthBonus:
                return $"환생 보너스 {level}개 추가 획득";
            case GemUpgradeType.DropRate:
                return $"드롭 확률 업 (고등급 아이템 등장률 증가)";
            case GemUpgradeType.StatBonus:
                return $"기본 스탯 {(level * GameConfig.StatBonusPerLevel * 100):F0}% 증가";
            default:
                return "알 수 없는 효과";
        }
    }

    /// <summary>
    /// 총 보석 업그레이드 레벨
    /// </summary>
    public int GetTotalGemLevels()
    {
        var gemUpgrades = _gameState.GemUpgrades;
        return gemUpgrades.offlineRewardLevel +
               gemUpgrades.critDamageLevel +
               gemUpgrades.autoBattleLevel +
               gemUpgrades.rebirthBonusLevel +
               gemUpgrades.dropRateLevel +
               gemUpgrades.statBonusLevel;
    }
}
