using System;
using UnityEngine;

/// <summary>
/// 저장/로드용 직렬화 가능 데이터 클래스
/// GameState의 스냅샷을 저장합니다.
/// </summary>
[Serializable]
public class SaveData
{
    public PlayerData player;
    public StageData stage;
    public CombatPhaseData combatPhase;
    public InventoryData inventory;
    public SettingsData settings;
    public TutorialData tutorial;
    public DailyMissionData dailyMissions;
    public RebirthData rebirth;
    public StatsData stats;
    public GemUpgradeData gemUpgrades;
    
    /// <summary>
    /// 현재 GameState에서 SaveData 생성
    /// </summary>
    public static SaveData CreateFromGameState(GameState state)
    {
        return new SaveData
        {
            player = state.player,
            stage = state.stage,
            combatPhase = state.combatPhase,
            inventory = state.inventory,
            settings = state.settings,
            tutorial = state.tutorial,
            dailyMissions = state.dailyMissions,
            rebirth = state.rebirth,
            stats = state.stats,
            gemUpgrades = state.gemUpgrades
        };
    }
    
    /// <summary>
    /// SaveData를 GameState에 적용
    /// </summary>
    public void ApplyToGameState(GameState state)
    {
        state.player = player;
        state.stage = stage;
        state.combatPhase = combatPhase;
        state.inventory = inventory;
        state.settings = settings;
        state.tutorial = tutorial;
        state.dailyMissions = dailyMissions;
        state.rebirth = rebirth;
        state.stats = stats;
        state.gemUpgrades = gemUpgrades;
    }
}
