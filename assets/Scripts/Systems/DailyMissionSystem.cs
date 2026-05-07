using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 미션 타입 열거형
/// </summary>
public enum MissionType
{
    /// <summary>몬스터 처치</summary>
    Kill,
    /// <summary>스테이지 클리어</summary>
    ClearStage,
    /// <summary>골드 획득</summary>
    CollectGold,
    /// <summary>아이템 합성</summary>
    Synthesize,
    /// <summary>환생</summary>
    Rebirth
}

/// <summary>
/// 일일/주간 미션 시스템을 관리하는 클래스
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class DailyMissionSystem : MonoBehaviour
{
    private static DailyMissionSystem _instance;
    
    /// <summary>
    /// DailyMissionSystem의 싱글톤 인스턴스
    /// </summary>
    public static DailyMissionSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DailyMissionSystem");
                _instance = go.AddComponent<DailyMissionSystem>();
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

    private void OnEnable()
    {
        // 의존성 주입 확인
        InjectDependencies();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.MONSTER_KILL, OnMonsterKill);
        _eventBus.On(GameEvents.STAGE_CLEAR, OnStageClear);
        _eventBus.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
        _eventBus.On(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
        _eventBus.On(GameEvents.REBIRTH_PERFORMED, OnRebirth);
    }

    private void OnDisable()
    {
        // 이벤트 해제
        _eventBus.Off(GameEvents.MONSTER_KILL, OnMonsterKill);
        _eventBus.Off(GameEvents.STAGE_CLEAR, OnStageClear);
        _eventBus.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
        _eventBus.Off(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
        _eventBus.Off(GameEvents.REBIRTH_PERFORMED, OnRebirth);
    }

    // ========== 미션 생성 ==========
    
    /// <summary>
    /// 일일 미션 생성
    /// </summary>
    public void GenerateDailyMissions()
    {
        var dailyMissions = _gameState.DailyMissions;
        dailyMissions.missions.Clear();
        
        // 5개 미션 생성
        AddMission(dailyMissions.missions, MissionType.Kill, GetDailyTarget(MissionType.Kill), GetDailyReward(MissionType.Kill));
        AddMission(dailyMissions.missions, MissionType.ClearStage, GetDailyTarget(MissionType.ClearStage), GetDailyReward(MissionType.ClearStage));
        AddMission(dailyMissions.missions, MissionType.CollectGold, GetDailyTarget(MissionType.CollectGold), GetDailyReward(MissionType.CollectGold));
        AddMission(dailyMissions.missions, MissionType.Synthesize, GetDailyTarget(MissionType.Synthesize), GetDailyReward(MissionType.Synthesize));
        AddMission(dailyMissions.missions, MissionType.Kill, GetDailyTarget(MissionType.Kill) * 2, GetDailyReward(MissionType.Kill) * 2);
        
        dailyMissions.lastReset = DateTime.UtcNow.Ticks;
        _gameState.DailyMissions = dailyMissions;
        
        _logger.Info($"일일 미션 생성 ({dailyMissions.missions.Count}개)");
    }

    /// <summary>
    /// 주간 미션 생성
    /// </summary>
    public void GenerateWeeklyMissions()
    {
        var dailyMissions = _gameState.DailyMissions;
        dailyMissions.weeklyMissions.Clear();
        
        // 3개 미션 생성 (일일의 5배 난이도)
        AddMission(dailyMissions.weeklyMissions, MissionType.Kill, GetDailyTarget(MissionType.Kill) * 5, GetWeeklyReward(MissionType.Kill));
        AddMission(dailyMissions.weeklyMissions, MissionType.ClearStage, GetDailyTarget(MissionType.ClearStage) * 5, GetWeeklyReward(MissionType.ClearStage));
        AddMission(dailyMissions.weeklyMissions, MissionType.CollectGold, GetDailyTarget(MissionType.CollectGold) * 5, GetWeeklyReward(MissionType.CollectGold));
        
        dailyMissions.weeklyLastReset = DateTime.UtcNow.Ticks;
        _gameState.DailyMissions = dailyMissions;
        
        _logger.Info($"주간 미션 생성 ({dailyMissions.weeklyMissions.Count}개)");
    }

    private void AddMission(List<MissionData> missionList, MissionType type, int target, long reward)
    {
        missionList.Add(new MissionData
        {
            id = $"{type}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            type = type.ToString(),
            target = target,
            progress = 0,
            completed = false,
            claimed = false,
            reward = reward.ToString()
        });
    }

    private string GetMissionDescription(MissionType type, int target)
    {
        switch (type)
        {
            case MissionType.Kill: return $"몬스터 {target}마리 처치";
            case MissionType.ClearStage: return $"스테이지 {target}클리어";
            case MissionType.CollectGold: return $"골드 {target:N0}획득";
            case MissionType.Synthesize: return $"아이템 {target}회 합성";
            case MissionType.Rebirth: return $"환생 {target}회";
            default: return "미션";
        }
    }

    private int GetDailyTarget(MissionType type)
    {
        int stage = Mathf.Max(1, _gameState.Stage.maxStage);
        
        switch (type)
        {
            case MissionType.Kill: return 20 + stage * 2;
            case MissionType.ClearStage: return 5 + stage / 2;
            case MissionType.CollectGold: return 1000 + stage * 100;
            case MissionType.Synthesize: return 3;
            case MissionType.Rebirth: return 1;
            default: return 10;
        }
    }

    private long GetDailyReward(MissionType type)
    {
        int stage = Mathf.Max(1, _gameState.Stage.maxStage);
        
        switch (type)
        {
            case MissionType.Kill: return 100 + stage * 10;
            case MissionType.ClearStage: return 200 + stage * 20;
            case MissionType.CollectGold: return 50 + stage * 5;
            case MissionType.Synthesize: return 150;
            case MissionType.Rebirth: return 500;
            default: return 100;
        }
    }

    private long GetWeeklyReward(MissionType type)
    {
        // 주간 보상은 일일의 7~8배
        long daily = GetDailyReward(type);
        return daily * 8;
    }

    // ========== 미션 진행도 업데이트 ==========
    
    private void OnMonsterKill()
    {
        UpdateMissionProgress(MissionType.Kill, 1);
    }

    private void OnStageClear()
    {
        UpdateMissionProgress(MissionType.ClearStage, 1);
    }

    private void OnGoldChanged()
    {
        // 골드 획득량은 별도로 추적 필요
        // 간단하게 현재 골드를 기준으로 진행도 업데이트
        UpdateMissionProgress(MissionType.CollectGold, (int)(_gameState.Player.gold / 10));
    }

    private void OnSynthesize()
    {
        UpdateMissionProgress(MissionType.Synthesize, 1);
    }

    private void OnRebirth()
    {
        UpdateMissionProgress(MissionType.Rebirth, 1);
    }

    /// <summary>
    /// 미션 진행도 업데이트
    /// </summary>
    public void UpdateMissionProgress(MissionType type, int amount)
    {
        var dailyMissions = _gameState.DailyMissions;
        string typeStr = type.ToString();
        
        // 일일 미션 업데이트
        for (int i = 0; i < dailyMissions.missions.Count; i++)
        {
            MissionData mission = dailyMissions.missions[i];
            if (mission.type == typeStr && !mission.completed)
            {
                mission.progress += amount;
                
                if (mission.progress >= mission.target)
                {
                    mission.completed = true;
                    _logger.Info($"미션 완료: {GetMissionDescription(type, mission.target)}");
                    _eventBus.Emit(GameEvents.DAILY_MISSION_COMPLETED);
                }
                
                dailyMissions.missions[i] = mission;
            }
        }
        
        // 주간 미션 업데이트
        for (int i = 0; i < dailyMissions.weeklyMissions.Count; i++)
        {
            MissionData mission = dailyMissions.weeklyMissions[i];
            if (mission.type == typeStr && !mission.completed)
            {
                mission.progress += amount;
                
                if (mission.progress >= mission.target)
                {
                    mission.completed = true;
                    _logger.Info($"주간 미션 완료: {GetMissionDescription(type, mission.target)}");
                    _eventBus.Emit(GameEvents.WEEKLY_MISSION_COMPLETED);
                }
                
                dailyMissions.weeklyMissions[i] = mission;
            }
        }
        
        _gameState.DailyMissions = dailyMissions;
    }

    // ========== 미션 보상 청구 ==========
    
    /// <summary>
    /// 미션 보상 청구
    /// </summary>
    /// <param name="missionId">미션 ID</param>
    /// <returns>성공 여부</returns>
    public bool ClaimReward(string missionId)
    {
        var dailyMissions = _gameState.DailyMissions;
        
        // 일일 미션에서 찾기
        int index = dailyMissions.missions.FindIndex(m => m.id == missionId);
        if (index >= 0)
        {
            MissionData mission = dailyMissions.missions[index];
            if (!mission.completed || mission.claimed)
            {
                _logger.Warn("보상을 청구할 수 없는 미션입니다.");
                return false;
            }
            
            // 보상 지급
            long reward = GetDailyReward((MissionType)Enum.Parse(typeof(MissionType), mission.type));
            var player = _gameState.Player;
            player.gold += reward;
            _gameState.Player = player;
            
            mission.claimed = true;
            dailyMissions.missions[index] = mission;
            _gameState.DailyMissions = dailyMissions;
            
            _logger.Info($"일일 미션 보상 청구: 골드 +{reward}");
            
            _eventBus.Emit(GameEvents.DAILY_MISSION_CLAIMED);
            _eventBus.Emit(GameEvents.GOLD_CHANGED);
            
            return true;
        }
        
        // 주간 미션에서 찾기
        index = dailyMissions.weeklyMissions.FindIndex(m => m.id == missionId);
        if (index >= 0)
        {
            MissionData mission = dailyMissions.weeklyMissions[index];
            if (!mission.completed || mission.claimed)
            {
                _logger.Warn("보상을 청구할 수 없는 미션입니다.");
                return false;
            }
            
            // 주간 보상 지급 (보석 + 골드)
            long goldReward = GetWeeklyReward((MissionType)Enum.Parse(typeof(MissionType), mission.type));
            int gemReward = 16; // 기본 보석 16개
            
            var player = _gameState.Player;
            player.gold += goldReward;
            player.gems += gemReward;
            _gameState.Player = player;
            
            mission.claimed = true;
            dailyMissions.weeklyMissions[index] = mission;
            _gameState.DailyMissions = dailyMissions;
            
            _logger.Info($"주간 미션 보상 청구: 골드 +{goldReward}, 보석 +{gemReward}");
            
            _eventBus.Emit(GameEvents.WEEKLY_MISSION_CLAIMED);
            _eventBus.Emit(GameEvents.GOLD_CHANGED);
            _eventBus.Emit(GameEvents.GEM_CHANGED);
            
            return true;
        }
        
        return false;
    }

    // ========== 일일/주간 초기화 ==========
    
    /// <summary>
    /// 일일 초기화 확인 (매일 0시 기준)
    /// </summary>
    public void CheckDailyReset()
    {
        var dailyMissions = _gameState.DailyMissions;
        
        DateTime now = DateTime.UtcNow;
        DateTime lastReset = new DateTime(dailyMissions.lastReset, DateTimeKind.Utc);
        
        // 하루 지났는지 확인
        if ((now.Date - lastReset.Date).TotalDays >= 1)
        {
            GenerateDailyMissions();
            _logger.Info("일일 미션 초기화");
        }
    }

    /// <summary>
    /// 주간 초기화 확인 (매주 월요일 0시 기준)
    /// </summary>
    public void CheckWeeklyReset()
    {
        var dailyMissions = _gameState.DailyMissions;
        
        DateTime now = DateTime.UtcNow;
        DateTime lastReset = new DateTime(dailyMissions.weeklyLastReset, DateTimeKind.Utc);
        
        // 월요일이고 일주일이 지났는지 확인
        if (now.DayOfWeek == DayOfWeek.Monday && (now - lastReset).TotalDays >= 7)
        {
            GenerateWeeklyMissions();
            _eventBus.Emit(GameEvents.WEEKLY_MISSIONS_RESET);
            _logger.Info("주간 미션 초기화");
        }
    }

    // ========== 버프 시스템 ==========
    
    /// <summary>
    /// 현재 활성화된 버프 확인
    /// </summary>
    /// <param name="buffType">버프 타입 (attackDouble, hpDouble, goldDouble, expDouble)</param>
    /// <returns>활성화 여부</returns>
    public bool HasActiveBuff(string buffType)
    {
        var dailyMissions = _gameState.DailyMissions;
        long buffTime = buffType switch
        {
            "attackDouble" => dailyMissions.buffs.attackDouble,
            "hpDouble" => dailyMissions.buffs.hpDouble,
            "goldDouble" => dailyMissions.buffs.goldDouble,
            "expDouble" => dailyMissions.buffs.expDouble,
            _ => 0
        };
        
        return buffTime > DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// 버프 활성화
    /// </summary>
    /// <param name="buffType">버프 타입</param>
    /// <param name="durationMinutes">지속 시간 (분)</param>
    public void ActivateBuff(string buffType, int durationMinutes)
    {
        var dailyMissions = _gameState.DailyMissions;
        long durationTicks = TimeSpan.FromMinutes(durationMinutes).Ticks;
        long expireTime = DateTime.UtcNow.Ticks + durationTicks;
        
        switch (buffType)
        {
            case "attackDouble":
                dailyMissions.buffs.attackDouble = expireTime;
                break;
            case "hpDouble":
                dailyMissions.buffs.hpDouble = expireTime;
                break;
            case "goldDouble":
                dailyMissions.buffs.goldDouble = expireTime;
                break;
            case "expDouble":
                dailyMissions.buffs.expDouble = expireTime;
                break;
        }
        
        _gameState.DailyMissions = dailyMissions;
        
        _logger.Info($"버프 활성화: {buffType} ({durationMinutes}분)");
        _eventBus.Emit(GameEvents.BUFF_ACTIVATED);
    }

    /// <summary>
    /// 버프 배율 가져오기 (전투 시스템에서 사용)
    /// </summary>
    /// <param name="buffType">버프 타입</param>
    /// <returns>배율 (활성화 시 2.0, 아니면 1.0)</returns>
    public float GetBuffMultiplier(string buffType)
    {
        if (HasActiveBuff(buffType))
        {
            return 2.0f; // 2배
        }
        return 1.0f;
    }

    /// <summary>
    /// 남은 버프 시간 가져오기 (초)
    /// </summary>
    /// <param name="buffType">버프 타입</param>
    /// <returns>남은 시간 (초), 활성화되지 않았으면 0</returns>
    public long GetRemainingBuffTime(string buffType)
    {
        var dailyMissions = _gameState.DailyMissions;
        long buffTime = buffType switch
        {
            "attackDouble" => dailyMissions.buffs.attackDouble,
            "hpDouble" => dailyMissions.buffs.hpDouble,
            "goldDouble" => dailyMissions.buffs.goldDouble,
            "expDouble" => dailyMissions.buffs.expDouble,
            _ => 0
        };
        
        if (buffTime <= DateTime.UtcNow.Ticks)
            return 0;
        
        return (buffTime - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerSecond;
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 완료 가능한 미션 수
    /// </summary>
    public int GetClaimableMissionCount()
    {
        var dailyMissions = _gameState.DailyMissions;
        int count = 0;
        
        foreach (var mission in dailyMissions.missions)
        {
            if (mission.completed && !mission.claimed) count++;
        }
        foreach (var mission in dailyMissions.weeklyMissions)
        {
            if (mission.completed && !mission.claimed) count++;
        }
        
        return count;
    }
}
