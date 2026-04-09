using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 오프라인 보상 시스템을 관리하는 클래스
/// 플레이어가 오프라인 상태였을 때의 보상을 계산하고 지급합니다.
/// </summary>
public class OfflineRewardSystem : MonoBehaviour
{
    private static OfflineRewardSystem _instance;
    
    /// <summary>
    /// OfflineRewardSystem의 싱글톤 인스턴스
    /// </summary>
    public static OfflineRewardSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("OfflineRewardSystem");
                _instance = go.AddComponent<OfflineRewardSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>마지막 저장 시간</summary>
    private DateTime _lastSaveTime;

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
        // 게임 로드 시 마지막 저장 시간 업데이트
        EventBus.Instance.On(GameEvents.GAME_LOADED, OnGameLoaded);
    }

    private void OnDisable()
    {
        EventBus.Instance.Off(GameEvents.GAME_LOADED, OnGameLoaded);
    }

    private void OnGameLoaded()
    {
        // 마지막 저장 시간을 현재 시간으로 업데이트
        _lastSaveTime = DateTime.UtcNow;
    }

    // ========== 오프라인 시간 계산 ==========
    
    /// <summary>
    /// 오프라인 경과 시간 계산 (초)
    /// </summary>
    /// <returns>경과 시간 (초)</returns>
    public float CalculateOfflineTime()
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan elapsed = now - _lastSaveTime;
        
        // 최대 24시간 제한
        float hours = (float)elapsed.TotalHours;
        if (hours > GameConfig.MaxOfflineTime)
        {
            hours = GameConfig.MaxOfflineTime;
        }
        
        return hours * 3600f; // 시간 → 초 변환
    }

    /// <summary>
    /// 오프라인 시간 포맷팅
    /// </summary>
    /// <param name="seconds">초</param>
    /// <returns>포맷된 문자열</returns>
    public string FormatOfflineTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        
        if (time.TotalDays >= 1)
        {
            return $"{(int)time.TotalDays}일 {time.Hours}시간";
        }
        else if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}시간 {time.Minutes}분";
        }
        else
        {
            return $"{(int)time.TotalMinutes}분 {time.Seconds}초";
        }
    }

    // ========== 오프라인 보상 계산 ==========
    
    /// <summary>
    /// 오프라인 보상 계산
    /// </summary>
    /// <param name="offlineSeconds">오프라인 시간 (초)</param>
    /// <returns>오프라인 보상 데이터</returns>
    public OfflineRewardData CalculateRewards(float offlineSeconds)
    {
        if (offlineSeconds <= 0)
        {
            return new OfflineRewardData();
        }
        
        GameState state = GameState.Instance;
        
        // 오프라인 보상 배율 (보석 업그레이드 적용)
        float multiplier = state.GetOfflineRewardMultiplier();
        
        // 시간당 보상 계산
        float hours = offlineSeconds / 3600f;
        
        // 골드 보상 (스테이지 * 100 * 배율 * 시간)
        long goldReward = (long)(state.stage.maxStage * 100 * multiplier * hours);
        
        // 경험치 보상 (스테이지 * 50 * 배율 * 시간)
        long expReward = (long)(state.stage.maxStage * 50 * multiplier * hours);
        
        // 아이템 드롭 시뮬레이션
        List<ItemData> items = new List<ItemData>();
        int dropCount = Mathf.FloorToInt(hours * 2 * multiplier); // 시간당 2개 * 배율
        
        for (int i = 0; i < dropCount; i++)
        {
            // 드롭 확률에 따라 아이템 생성
            float[] dropRates = state.GetDropRates();
            int grade = 0;
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;
            
            for (int j = 0; j < dropRates.Length; j++)
            {
                cumulative += dropRates[j];
                if (roll < cumulative)
                {
                    grade = j;
                    break;
                }
            }
            
            items.Add(new ItemData
            {
                id = $"offline_item_{i}",
                name = $"오프라인 아이템 ({GetGradeName(grade)})",
                grade = grade,
                quantity = 1
            });
        }
        
        GameLogger.Info($"오프라인 보상 계산 완료: 골드 {goldReward:N0}, 경험치 {expReward:N0}, 아이템 {items.Count}개");
        
        return new OfflineRewardData
        {
            gold = goldReward,
            experience = expReward,
            items = items.ToArray(),
            offlineTime = offlineSeconds
        };
    }

    /// <summary>
    /// 오프라인 보상 청구
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool ClaimRewards()
    {
        float offlineSeconds = CalculateOfflineTime();
        
        if (offlineSeconds <= 0)
        {
            GameLogger.Warn("오프라인 시간이 0입니다.");
            return false;
        }
        
        OfflineRewardData rewards = CalculateRewards(offlineSeconds);
        
        GameState state = GameState.Instance;
        
        // 골드 지급
        state.player.gold += rewards.gold;
        
        // 경험치 지급
        state.player.experience += rewards.experience;
        
        // 레벨업 확인
        CheckLevelUp();
        
        // 아이템 지급
        foreach (var item in rewards.items)
        {
            state.inventory.items.Add(item);
            
            if (!state.inventory.discoveredItems.Contains(item.id))
            {
                state.inventory.discoveredItems.Add(item.id);
                state.stats.totalItemsDiscovered++;
            }
        }
        
        // 통계 업데이트
        state.stats.totalGoldEarned += rewards.gold;
        
        GameLogger.Info($"오프라인 보상 청구 완료: {FormatOfflineTime(offlineSeconds)} 경과");
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.OFFLINE_REWARDS_CLAIMED);
        EventBus.Instance.Emit(GameEvents.GOLD_CHANGED);
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
        
        return true;
    }

    /// <summary>
    /// 오프라인 보상 청구 가능 여부
    /// </summary>
    /// <returns>청구 가능하면 true</returns>
    public bool CanClaimRewards()
    {
        return CalculateOfflineTime() > 0;
    }

    /// <summary>
    /// 레벨업 확인
    /// </summary>
    private void CheckLevelUp()
    {
        GameState state = GameState.Instance;
        
        while (state.player.experience >= state.GetExpToNextLevel())
        {
            state.player.experience -= state.GetExpToNextLevel();
            state.player.level++;
            state.stats.totalLevelUps++;
            
            // 스탯 증가
            state.player.attack += GameConfig.StatPointPerLevel;
            state.player.defense += GameConfig.StatPointPerLevel;
            state.player.health += GameConfig.StatPointPerLevel * 10;
            
            GameLogger.Info($"레벨업! 레벨 {state.player.level}");
            
            EventBus.Instance.Emit(GameEvents.PLAYER_LEVEL_UP);
            EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
            EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
        }
    }

    private string GetGradeName(int grade)
    {
        string[] names = new string[] { "일반", "고급", "희귀", "영웅", "전설" };
        return names[Mathf.Min(grade, names.Length - 1)];
    }
}

/// <summary>
/// 오프라인 보상 데이터
/// </summary>
[System.Serializable]
public struct OfflineRewardData
{
    public long gold;
    public long experience;
    public ItemData[] items;
    public float offlineTime;
}
