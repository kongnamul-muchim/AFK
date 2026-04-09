using UnityEngine;

/// <summary>
/// 게임 통계를 추적하는 클래스
/// 플레이어의 모든 게임 활동을 기록합니다.
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

    private void OnEnable()
    {
        // 이벤트 구독
        EventBus.Instance.On(GameEvents.MONSTER_KILL, OnMonsterKill);
        EventBus.Instance.On(GameEvents.BOSS_KILL, OnBossKill);
        EventBus.Instance.On(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        EventBus.Instance.On(GameEvents.REBIRTH_PERFORMED, OnRebirth);
        EventBus.Instance.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.On(GameEvents.STAGE_CLEAR, OnStageClear);
    }

    private void OnDisable()
    {
        EventBus.Instance.Off(GameEvents.MONSTER_KILL, OnMonsterKill);
        EventBus.Instance.Off(GameEvents.BOSS_KILL, OnBossKill);
        EventBus.Instance.Off(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        EventBus.Instance.Off(GameEvents.REBIRTH_PERFORMED, OnRebirth);
        EventBus.Instance.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.Off(GameEvents.STAGE_CLEAR, OnStageClear);
    }

    // ========== 이벤트 핸들러 ==========
    
    private void OnMonsterKill()
    {
        GameState state = GameState.Instance;
        state.stats.totalKills++;
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnBossKill()
    {
        GameState state = GameState.Instance;
        state.stats.totalBossKills++;
        state.stats.totalKills++;
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnLevelUp()
    {
        GameState state = GameState.Instance;
        state.stats.totalLevelUps++;
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnRebirth()
    {
        GameState state = GameState.Instance;
        state.stats.totalRebirths++;
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
    }

    private void OnGoldChanged()
    {
        GameState state = GameState.Instance;
        // 현재 골드 업데이트 (누적은 별도로 관리)
    }

    private void OnStageClear()
    {
        GameState state = GameState.Instance;
        // 스테이지 클리어 수 업데이트
    }

    // ========== 플레이 시간 ==========
    
    private void Update()
    {
        GameState state = GameState.Instance;
        state.stats.totalPlayTime += Time.deltaTime;
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
        GameState state = GameState.Instance;
        return state.stats.totalPlayTime;
    }

    /// <summary>
    /// 통계 요약 가져오기
    /// </summary>
    /// <returns>통계 요약 문자열</returns>
    public string GetStatsSummary()
    {
        GameState state = GameState.Instance;
        
        return $@"
=== 게임 통계 ===
플레이 시간: {FormatPlayTime(state.stats.totalPlayTime)}
레벨: {state.player.level}
최대 스테이지: {state.stage.maxStage}
총 처치 수: {state.stats.totalKills:N0}
보스 처치: {state.stats.totalBossKills}
레벨업 횟수: {state.stats.totalLevelUps}
환생 횟수: {state.stats.totalRebirths}
총 획득 골드: {state.stats.totalGoldEarned:N0}
발견한 아이템: {state.stats.totalItemsDiscovered}개
치명확률: {state.player.critChance:F1}%
치명피해: {state.GetCritDamageMultiplier():F2}x
";
    }
}
