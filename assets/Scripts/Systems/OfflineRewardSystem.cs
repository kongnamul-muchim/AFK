using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 오프라인 보상 시스템을 관리하는 클래스
/// 플레이어가 오프라인 상태였을 때의 보상을 계산하고 지급합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
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
        
        InjectDependencies();
    }

    private DateTime GetSaveFileLastWriteTime()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(savePath))
        {
            return File.GetLastWriteTimeUtc(savePath);
        }
        return DateTime.UtcNow;
    }

    // ========== 오프라인 시간 계산 ==========
    
    /// <summary>
    /// 오프라인 경과 시간 계산 (초)
    /// </summary>
    /// <returns>경과 시간 (초)</returns>
    public float CalculateOfflineTime()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        Debug.Log($"[OfflineTime] savePath={savePath}, exists={File.Exists(savePath)}");
        
        DateTime now = DateTime.UtcNow;
        DateTime lastWrite = GetSaveFileLastWriteTime();
        Debug.Log($"[OfflineTime] now={now:O}, lastWrite={lastWrite:O}");
        
        TimeSpan elapsed = now - lastWrite;
        Debug.Log($"[OfflineTime] elapsed={elapsed.TotalHours} hours");
        
        float hours = (float)elapsed.TotalHours;
        if (hours > GameConfig.MaxOfflineTime)
            hours = GameConfig.MaxOfflineTime;
        if (hours < 0f)
            hours = 0f;
        
        return hours * 3600f;
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
        
        // 오프라인 보상 배율 (보석 업그레이드 적용)
        float multiplier = _gameState.GetOfflineRewardMultiplier();
        
        // 시간당 보상 계산
        float hours = offlineSeconds / 3600f;
        
        // 골드 보상 (스테이지 * 100 * 배율 * 시간)
        long goldReward = (long)(_gameState.Stage.maxStage * 100 * multiplier * hours);
        
        // 경험치 보상 (스테이지 * 50 * 배율 * 시간)
        long expReward = (long)(_gameState.Stage.maxStage * 50 * multiplier * hours);
        
        // 아이템 드롭 시뮬레이션
        List<ItemData> items = new List<ItemData>();
        int dropCount = Mathf.FloorToInt(hours * GameConfig.OfflineItemDropPerHour * multiplier);
        
        for (int i = 0; i < dropCount; i++)
        {
            // 드롭 확률에 따라 아이템 생성
            float[] dropRates = _gameState.GetDropRates();
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
                count = 1
            });
        }
        
        _logger.Info($"오프라인 보상 계산 완료: 골드 {goldReward:N0}, 경험치 {expReward:N0}, 아이템 {items.Count}개");
        
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
            _logger.Warn("오프라인 시간이 0입니다.");
            return false;
        }
        
        OfflineRewardData rewards = CalculateRewards(offlineSeconds);
        
        // 골드 지급
        var player = _gameState.Player;
        player.gold += rewards.gold;
        _gameState.Player = player;
        
        // 경험치 지급
        player = _gameState.Player;
        player.experience += rewards.experience;
        _gameState.Player = player;
        
        // 레벨업 확인
        CheckLevelUp();
        
        // 아이템 지급
        var inventory = _gameState.Inventory;
        foreach (var item in rewards.items)
        {
            inventory.items.Add(item);
            
            if (!inventory.discoveredItems.Contains(item.id))
            {
                inventory.discoveredItems.Add(item.id);
                var stats = _gameState.Stats;
                stats.totalItemsDiscovered++;
                _gameState.Stats = stats;
            }
        }
        _gameState.Inventory = inventory;
        
        // 통계 업데이트
        var stats2 = _gameState.Stats;
        stats2.totalGoldEarned += rewards.gold;
        _gameState.Stats = stats2;
        
        _logger.Info($"오프라인 보상 청구 완료: {FormatOfflineTime(offlineSeconds)} 경과");
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.OFFLINE_REWARDS_CLAIMED);
        _eventBus.Emit(GameEvents.GOLD_CHANGED);
        _eventBus.Emit(GameEvents.STATS_CHANGED);
        
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
        var player = _gameState.Player;
        
        while (player.experience >= _gameState.GetExpToNextLevel())
        {
            player.experience -= _gameState.GetExpToNextLevel();
            player.level++;
            
            var stats = _gameState.Stats;
            stats.totalLevelUps++;
            _gameState.Stats = stats;
            
            // 스탯 증가
            player.attack += GameConfig.StatPointPerLevel;
            player.defense += GameConfig.StatPointPerLevel;
            player.health += GameConfig.StatPointPerLevel * 10;
            
            _logger.Info($"레벨업! 레벨 {player.level}");
            
            _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
            _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
            _eventBus.Emit(GameEvents.STATS_CHANGED);
        }
        
        _gameState.Player = player;
    }

    private string GetGradeName(int grade)
    {
        return GameConfig.GetGradeName(grade);
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
