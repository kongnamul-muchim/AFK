using UnityEngine;

/// <summary>
/// 게임 통계를 추적하는 클래스
/// 플레이어의 모든 게임 활동을 기록합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class StatsTracker : MonoBehaviour
{
    private static StatsTracker _instance;
    
    /// <summary>
    /// StatsTracker의 싱글톤 인스턴스
    /// </summary>
    public static StatsTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("StatsTracker");
                _instance = go.AddComponent<StatsTracker>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    
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

    private void OnEnable()
    {
        // 의존성 주입 확인
        InjectDependencies();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.MONSTER_KILL, OnMonsterKill);
        _eventBus.On(GameEvents.BOSS_KILL, OnBossKill);
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        _eventBus.On(GameEvents.REBIRTH_PERFORMED, OnRebirth);
        _eventBus.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
        _eventBus.On(GameEvents.STAGE_CLEAR, OnStageClear);
    }

    private void OnDisable()
    {
        _eventBus.Off(GameEvents.MONSTER_KILL, OnMonsterKill);
        _eventBus.Off(GameEvents.BOSS_KILL, OnBossKill);
        _eventBus.Off(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        _eventBus.Off(GameEvents.REBIRTH_PERFORMED, OnRebirth);
        _eventBus.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
        _eventBus.Off(GameEvents.STAGE_CLEAR, OnStageClear);
    }

    // ========== 이벤트 핸들러 ==========
    
    private void OnMonsterKill()
    {
        // CombatSystem.ProcessVictory에서 이미 stats.totalKills를 증가시킴
        // 중복 방지를 위해 여기서는 STATS_CHANGED 이벤트만 발생
        _eventBus.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnBossKill()
    {
        // CombatSystem.ProcessVictory에서 이미 stats.totalBossKills를 증가시킴
        _eventBus.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnLevelUp()
    {
        var stats = _gameState.Stats;
        stats.totalLevelUps++;
        _gameState.Stats = stats;
        _eventBus.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnRebirth()
    {
        var stats = _gameState.Stats;
        stats.totalRebirths++;
        _gameState.Stats = stats;
        _eventBus.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnGoldChanged()
    {
        // 현재 골드 업데이트 (누적은 별도로 관리)
    }

    private void OnStageClear()
    {
        // 스테이지 클리어 수 업데이트
    }

    // ========== 플레이 시간 ==========
    
    private void Update()
    {
        var stats = _gameState.Stats;
        stats.totalPlayTime += Time.deltaTime;
        _gameState.Stats = stats;
    }

    // ========== 통계 포맷팅 ==========
    
    /// <summary>
    /// 플레이 시간을 시:분:초 형식으로 포맷팅
    /// </summary>
    /// <param name="seconds">초</param>
    /// <returns>포맷된 문자열</returns>
    public string FormatPlayTime(float seconds)
    {
        System.TimeSpan time = System.TimeSpan.FromSeconds(seconds);
        
        if (time.TotalDays >= 1)
        {
            return $"{(int)time.TotalDays}일 {(int)time.Hours}시간 {(int)time.Minutes}분";
        }
        else if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}시간 {(int)time.Minutes}분";
        }
        else
        {
            return $"{(int)time.Minutes}분 {(int)time.Seconds}초";
        }
    }

    /// <summary>
    /// 현재 플레이 시간 가져오기
    /// </summary>
    /// <returns>플레이 시간 (초)</returns>
    public float GetTotalPlayTime()
    {
        return _gameState.Stats.totalPlayTime;
    }

    /// <summary>
    /// 통계 요약 가져오기
    /// </summary>
    /// <returns>통계 요약 문자열</returns>
    public string GetStatsSummary()
    {
        var stats = _gameState.Stats;
        var player = _gameState.Player;
        var stage = _gameState.Stage;
        
        return $@"
=== 게임 통계 ===
플레이 시간: {FormatPlayTime(stats.totalPlayTime)}
레벨: {player.level}
최대 스테이지: {stage.maxStage}
총 처치 수: {stats.totalKills:N0}
보스 처치: {stats.totalBossKills}
레벨업 횟수: {stats.totalLevelUps}
환생 횟수: {stats.totalRebirths}
총 획득 골드: {stats.totalGoldEarned:N0}
발견한 아이템: {stats.totalItemsDiscovered}개
치명확률: {player.critChance:F1}%
치명피해: {_gameState.GetCritDamageMultiplier():F2}x
";
    }
}
