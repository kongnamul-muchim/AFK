using System;
using System.Collections.Generic;

/// <summary>
/// 게임 상태 접근을 위한 인터페이스
/// DIP 준수: 구체적 구현 대신 인터페이스에 의존
/// </summary>
public interface IGameState
{
    // 플레이어 데이터
    PlayerData Player { get; set; }
    
    // 스테이지 데이터
    StageData Stage { get; set; }
    
    // 전투 데이터
    CombatPhaseData CombatPhase { get; set; }
    
    // 인벤토리 데이터
    InventoryData Inventory { get; set; }
    
    // 설정 데이터
    SettingsData Settings { get; set; }
    
    // 튜토리얼 데이터
    TutorialData Tutorial { get; set; }
    
    // 미션 데이터
    DailyMissionData DailyMissions { get; set; }
    
    // 환생 데이터
    RebirthData Rebirth { get; set; }
    
    // 통계 데이터
    StatsData Stats { get; set; }
    
    // 보석 업그레이드 데이터
    GemUpgradeData GemUpgrades { get; set; }
    
    // 초기화 메서드
    void Initialize();
    void ResetForRebirth();
    
    // 계산 메서드
    float GetTotalAttack();
    float GetTotalDefense();
    float GetTotalHealth();
    long GetExpToNextLevel();
    float GetOfflineRewardMultiplier();
    float GetAutoBattleDamageMultiplier();
    float GetCritDamageMultiplier();
    float[] GetDropRates();
}

/// <summary>
/// 이벤트 버스 인터페이스
/// 문자열 기반 이벤트 시스템을 위한 인터페이스
/// </summary>
public interface IEventBus
{
    void On(string eventName, Action callback);
    void Off(string eventName, Action callback);
    void Emit(string eventName);
    void Clear();
    void Once(string eventName, Action callback);
}

/// <summary>
/// 로거 인터페이스
/// </summary>
public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Debug(string message);
}

/// <summary>
/// 저장 관리자 인터페이스
/// </summary>
public interface ISaveManager
{
    void Save(GameState state);
    GameState Load();
    bool SaveExists();
    void DeleteSave();
    void CreateBackup();
    void RestoreFromBackup();
    void StartAutoSave(float interval = 5f);
    void StopAutoSave();
}
