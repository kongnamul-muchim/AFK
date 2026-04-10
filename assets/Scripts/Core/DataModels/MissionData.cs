using System;
using System.Collections.Generic;

/// <summary>
/// 게임 설정 데이터
/// </summary>
[Serializable]
public class SettingsData
{
    public float soundVolume = 1f;
    public float musicVolume = 1f;
    public bool autoBattleEnabled = true;
}

/// <summary>
/// 튜토리얼 진행 데이터
/// </summary>
[Serializable]
public class TutorialData
{
    public int currentStep;
    public List<string> completedSteps = new List<string>();
}

/// <summary>
/// 일일/주간 미션 데이터
/// </summary>
[Serializable]
public class DailyMissionData
{
    public List<MissionData> missions = new List<MissionData>();
    public long lastReset;
    public List<MissionData> weeklyMissions = new List<MissionData>();
    public long weeklyLastReset;
    
    public MissionBuffsData buffs = new MissionBuffsData();
    
    public void Reset()
    {
        missions.Clear();
        weeklyMissions.Clear();
        buffs = new MissionBuffsData();
    }
}

/// <summary>
/// 미션 버프 데이터
/// </summary>
[Serializable]
public class MissionBuffsData
{
    public long attackDouble = 0;
    public long hpDouble = 0;
    public long goldDouble = 0;
    public long expDouble = 0;
}

/// <summary>
/// 미션 데이터
/// </summary>
[Serializable]
public struct MissionData
{
    public string id;
    public string type;
    public int target;
    public int progress;
    public bool completed;
    public bool claimed;
    public string reward;
}
