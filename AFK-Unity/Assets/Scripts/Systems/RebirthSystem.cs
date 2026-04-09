using UnityEngine;

/// <summary>
/// 환생 시스템을 관리하는 클래스
/// 플레이어의 환생과 보석 업그레이드를 처리합니다.
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

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 환생 시스템 ==========
    
    /// <summary>
    /// 환생 가능 여부 확인
    /// </summary>
    /// <returns>환생 가능하면 true</returns>
    public bool CanRebirth()
    {
        GameState state = GameState.Instance;
        
        // 최소 레벨 확인
        if (state.player.level < GameConfig.MinRebirthLevel)
        {
            GameLogger.Warn($"환생 불가 - 최소 레벨 {GameConfig.MinRebirthLevel} 필요 (현재: {state.player.level})");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 환생 수행
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool PerformRebirth()
    {
        if (!CanRebirth())
        {
            return false;
        }
        
        GameState state = GameState.Instance;
        
        GameLogger.Info($"환생 시작 - {state.rebirth.rebirthCount + 1}번째 환생");
        
        // 환생 보너스 계산
        float rebirthBonus = CalculateRebirthBonus();
        
        // 데이터 초기화
        state.ResetForRebirth();
        
        // 환생 카운트 증가
        state.rebirth.rebirthCount++;
        state.rebirth.totalBonus = rebirthBonus;
        
        // 통계 업데이트
        state.stats.totalRebirths++;
        
        // 골드 일부 유지
        long retainedGold = (long)(state.player.gold * GameConfig.RebirthGoldRetention);
        state.player.gold = retainedGold;
        
        GameLogger.Info($"환생 완료 - 보너스: {rebirthBonus:P1}, 유지 골드: {retainedGold}");
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.REBIRTH_PERFORMED);
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
        EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 환생 보너스 계산
    /// </summary>
    private float CalculateRebirthBonus()
    {
        GameState state = GameState.Instance;
        
        // 기본 보너스 (환생당 10%)
        float baseBonus = state.rebirth.rebirthCount * GameConfig.RebirthStatBonus;
        
        // 보석 업그레이드 보너스
        float gemBonus = state.gemUpgrades.rebirthBonusLevel * 0.05f; // 레벨당 5%
        
        return baseBonus + gemBonus;
    }

    /// <summary>
    /// 환생 후 유지되는 데이터 확인
    /// </summary>
    public string GetPreservedDataInfo()
    {
        GameState state = GameState.Instance;
        
        return $@"
유지되는 데이터:
- 발견한 아이템: {state.inventory.discoveredItems.Count}개
- 통계: 모든 통계 유지
- 보석 업그레이드: 모든 업그레이드 유지
- 환생 보너스: {state.rebirth.totalBonus:P1}

초기화되는 데이터:
- 레벨: {state.player.level} → 1
- 골드: {state.player.gold:N0} → {state.player.gold * GameConfig.RebirthGoldRetention:N0} (10% 유지)
- 스테이지: {state.stage.currentStage} → 1
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
        GameState state = GameState.Instance;
        int currentLevel = GetGemLevel(type);
        int maxLevel = GetMaxGemLevel(type);
        
        if (currentLevel >= maxLevel)
        {
            GameLogger.Warn($"최대 레벨 도달: {type}");
            return false;
        }
        
        long cost = GetUpgradeCost(type);
        if (state.player.gems < cost)
        {
            GameLogger.Warn($"보석 부족: {state.player.gems}/{cost}");
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
        
        GameState state = GameState.Instance;
        long cost = GetUpgradeCost(type);
        
        // 보석 차감
        state.player.gems -= (int)cost;
        
        // 레벨 증가
        IncreaseGemLevel(type);
        
        GameLogger.Info($"보석 업그레이드: {type} → 레벨 {GetGemLevel(type)}");
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.GEM_UPGRADED);
        EventBus.Instance.Emit(GameEvents.GEM_CHANGED);
        EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 현재 보석 레벨 가져오기
    /// </summary>
    public int GetGemLevel(GemUpgradeType type)
    {
        GameState state = GameState.Instance;
        
        switch (type)
        {
            case GemUpgradeType.OfflineReward: return state.gemUpgrades.offlineRewardLevel;
            case GemUpgradeType.CritDamage: return state.gemUpgrades.critDamageLevel;
            case GemUpgradeType.AutoBattle: return state.gemUpgrades.autoBattleLevel;
            case GemUpgradeType.RebirthBonus: return state.gemUpgrades.rebirthBonusLevel;
            case GemUpgradeType.DropRate: return state.gemUpgrades.dropRateLevel;
            case GemUpgradeType.StatBonus: return state.gemUpgrades.statBonusLevel;
            default: return 0;
        }
    }

    /// <summary>
    /// 보석 레벨 증가
    /// </summary>
    private void IncreaseGemLevel(GemUpgradeType type)
    {
        GameState state = GameState.Instance;
        
        switch (type)
        {
            case GemUpgradeType.OfflineReward: state.gemUpgrades.offlineRewardLevel++; break;
            case GemUpgradeType.CritDamage: state.gemUpgrades.critDamageLevel++; break;
            case GemUpgradeType.AutoBattle: state.gemUpgrades.autoBattleLevel++; break;
            case GemUpgradeType.RebirthBonus: state.gemUpgrades.rebirthBonusLevel++; break;
            case GemUpgradeType.DropRate: state.gemUpgrades.dropRateLevel++; break;
            case GemUpgradeType.StatBonus: state.gemUpgrades.statBonusLevel++; break;
        }
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
        GameState state = GameState.Instance;
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

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 총 보석 업그레이드 레벨
    /// </summary>
    public int GetTotalGemLevels()
    {
        GameState state = GameState.Instance;
        return state.gemUpgrades.offlineRewardLevel +
               state.gemUpgrades.critDamageLevel +
               state.gemUpgrades.autoBattleLevel +
               state.gemUpgrades.rebirthBonusLevel +
               state.gemUpgrades.dropRateLevel +
               state.gemUpgrades.statBonusLevel;
    }
}
