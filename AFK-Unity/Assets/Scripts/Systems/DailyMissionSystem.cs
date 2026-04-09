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
        EventBus.Instance.On(GameEvents.STAGE_CLEAR, OnStageClear);
        EventBus.Instance.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.On(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
        EventBus.Instance.On(GameEvents.REBIRTH_PERFORMED, OnRebirth);
    }

    private void OnDisable()
    {
        // 이벤트 해제
        EventBus.Instance.Off(GameEvents.MONSTER_KILL, OnMonsterKill);
        EventBus.Instance.Off(GameEvents.STAGE_CLEAR, OnStageClear);
        EventBus.Instance.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
        EventBus.Instance.Off(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
        EventBus.Instance.Off(GameEvents.REBIRTH_PERFORMED, OnRebirth);
    }

    // ========== 미션 생성 ==========
    
    /// <summary>
    /// 일일 미션 생성
    /// </summary>
    public void GenerateDailyMissions()
    {
        GameState state = GameState.Instance;
        state.dailyMissions.dailyMissions.Clear();
        
        // 5개 미션 생성
        AddMission(state.dailyMissions.dailyMissions, MissionType.Kill, GetDailyTarget(MissionType.Kill), GetDailyReward(MissionType.Kill));
        AddMission(state.dailyMissions.dailyMissions, MissionType.ClearStage, GetDailyTarget(MissionType.ClearStage), GetDailyReward(MissionType.ClearStage));
        AddMission(state.dailyMissions.dailyMissions, MissionType.CollectGold, GetDailyTarget(MissionType.CollectGold), GetDailyReward(MissionType.CollectGold));
        AddMission(state.dailyMissions.dailyMissions, MissionType.Synthesize, GetDailyTarget(MissionType.Synthesize), GetDailyReward(MissionType.Synthesize));
        AddMission(state.dailyMissions.dailyMissions, MissionType.Kill, GetDailyTarget(MissionType.Kill) * 2, GetDailyReward(MissionType.Kill) * 2);
        
        state.dailyMissions.lastDailyReset = DateTime.UtcNow;
        
        GameLogger.Info($"일일 미션 생성 ({state.dailyMissions.dailyMissions.Count}개)");
    }

    /// <summary>
    /// 주간 미션 생성
    /// </summary>
    public void GenerateWeeklyMissions()
    {
        GameState state = GameState.Instance;
        state.dailyMissions.weeklyMissions.Clear();
        
        // 3개 미션 생성 (일일의 5배 난이도)
        AddMission(state.dailyMissions.weeklyMissions, MissionType.Kill, GetDailyTarget(MissionType.Kill) * 5, GetWeeklyReward(MissionType.Kill));
        AddMission(state.dailyMissions.weeklyMissions, MissionType.ClearStage, GetDailyTarget(MissionType.ClearStage) * 5, GetWeeklyReward(MissionType.ClearStage));
        AddMission(state.dailyMissions.weeklyMissions, MissionType.CollectGold, GetDailyTarget(MissionType.CollectGold) * 5, GetWeeklyReward(MissionType.CollectGold));
        
        state.dailyMissions.lastWeeklyReset = DateTime.UtcNow;
        
        GameLogger.Info($"주간 미션 생성 ({state.dailyMissions.weeklyMissions.Count}개)");
    }

    private void AddMission(List<MissionData> missionList, MissionType type, int target, long reward)
    {
        missionList.Add(new MissionData
        {
            id = $"{type}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            description = GetMissionDescription(type, target),
            targetCount = target,
            currentCount = 0,
            isCompleted = false,
            isClaimed = false,
            type = (int)type
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
        GameState state = GameState.Instance;
        int stage = Mathf.Max(1, state.stage.maxStage);
        
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
        GameState state = GameState.Instance;
        int stage = Mathf.Max(1, state.stage.maxStage);
        
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
        GameState state = GameState.Instance;
        UpdateMissionProgress(MissionType.CollectGold, (int)(state.player.gold / 10));
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
        GameState state = GameState.Instance;
        
        // 일일 미션 업데이트
        for (int i = 0; i < state.dailyMissions.dailyMissions.Count; i++)
        {
            MissionData mission = state.dailyMissions.dailyMissions[i];
            if ((MissionType)mission.type == type && !mission.isCompleted)
            {
                mission.currentCount += amount;
                
                if (mission.currentCount >= mission.targetCount)
                {
                    mission.isCompleted = true;
                    GameLogger.Info($"미션 완료: {mission.description}");
                    EventBus.Instance.Emit(GameEvents.DAILY_MISSION_COMPLETED);
                }
                
                state.dailyMissions.dailyMissions[i] = mission;
            }
        }
        
        // 주간 미션 업데이트
        for (int i = 0; i < state.dailyMissions.weeklyMissions.Count; i++)
        {
            MissionData mission = state.dailyMissions.weeklyMissions[i];
            if ((MissionType)mission.type == type && !mission.isCompleted)
            {
                mission.currentCount += amount;
                
                if (mission.currentCount >= mission.targetCount)
                {
                    mission.isCompleted = true;
                    GameLogger.Info($"주간 미션 완료: {mission.description}");
                    EventBus.Instance.Emit(GameEvents.WEEKLY_MISSION_COMPLETED);
                }
                
                state.dailyMissions.weeklyMissions[i] = mission;
            }
        }
    }

    // ========== 미션 보상 청구 ==========
    
    /// <summary>
    /// 미션 보상 청구
    /// </summary>
    /// <param name="missionId">미션 ID</param>
    /// <returns>성공 여부</returns>
    public bool ClaimReward(string missionId)
    {
        GameState state = GameState.Instance;
        
        // 일일 미션에서 찾기
        int index = state.dailyMissions.dailyMissions.FindIndex(m => m.id == missionId);
        if (index >= 0)
        {
            MissionData mission = state.dailyMissions.dailyMissions[index];
            if (!mission.isCompleted || mission.isClaimed)
            {
                GameLogger.Warn("보상을 청구할 수 없는 미션입니다.");
                return false;
            }
            
            // 보상 지급
            long reward = GetDailyReward((MissionType)mission.type);
            state.player.gold += reward;
            
            mission.isClaimed = true;
            state.dailyMissions.dailyMissions[index] = mission;
            
            GameLogger.Info($"일일 미션 보상 청구: 골드 +{reward}");
            
            EventBus.Instance.Emit(GameEvents.DAILY_MISSION_CLAIMED);
            EventBus.Instance.Emit(GameEvents.GOLD_CHANGED);
            
            return true;
        }
        
        // 주간 미션에서 찾기
        index = state.dailyMissions.weeklyMissions.FindIndex(m => m.id == missionId);
        if (index >= 0)
        {
            MissionData mission = state.dailyMissions.weeklyMissions[index];
            if (!mission.isCompleted || mission.isClaimed)
            {
                GameLogger.Warn("보상을 청구할 수 없는 미션입니다.");
                return false;
            }
            
            // 주간 보상 지급 (보석 + 골드)
            long goldReward = GetWeeklyReward((MissionType)mission.type);
            int gemReward = 16; // 기본 보석 16개
            
            state.player.gold += goldReward;
            state.player.gems += gemReward;
            
            mission.isClaimed = true;
            state.dailyMissions.weeklyMissions[index] = mission;
            
            GameLogger.Info($"주간 미션 보상 청구: 골드 +{goldReward}, 보석 +{gemReward}");
            
            EventBus.Instance.Emit(GameEvents.WEEKLY_MISSION_CLAIMED);
            EventBus.Instance.Emit(GameEvents.GOLD_CHANGED);
            EventBus.Instance.Emit(GameEvents.GEM_CHANGED);
            
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
        GameState state = GameState.Instance;
        
        DateTime now = DateTime.UtcNow;
        DateTime lastReset = state.dailyMissions.lastDailyReset;
        
        // 하루 지났는지 확인
        if ((now.Date - lastReset.Date).TotalDays >= 1)
        {
            GenerateDailyMissions();
            GameLogger.Info("일일 미션 초기화");
        }
    }

    /// <summary>
    /// 주간 초기화 확인 (매주 월요일 0시 기준)
    /// </summary>
    public void CheckWeeklyReset()
    {
        GameState state = GameState.Instance;
        
        DateTime now = DateTime.UtcNow;
        DateTime lastReset = state.dailyMissions.lastWeeklyReset;
        
        // 월요일이고 일주일이 지났는지 확인
        if (now.DayOfWeek == DayOfWeek.Monday && (now - lastReset).TotalDays >= 7)
        {
            GenerateWeeklyMissions();
            EventBus.Instance.Emit(GameEvents.WEEKLY_MISSIONS_RESET);
            GameLogger.Info("주간 미션 초기화");
        }
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 완료 가능한 미션 수
    /// </summary>
    public int GetClaimableMissionCount()
    {
        GameState state = GameState.Instance;
        int count = 0;
        
        foreach (var mission in state.dailyMissions.dailyMissions)
        {
            if (mission.isCompleted && !mission.isClaimed) count++;
        }
        foreach (var mission in state.dailyMissions.weeklyMissions)
        {
            if (mission.isCompleted && !mission.isClaimed) count++;
        }
        
        return count;
    }
}
