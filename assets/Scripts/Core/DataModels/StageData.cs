using System;

/// <summary>
/// 스테이지 진행 데이터
/// </summary>
[Serializable]
public struct StageData
{
    public int currentStage;
    public int maxStage;
    public bool[] clearedStages;

    public void Reset()
    {
        currentStage = 1;
    }
}

/// <summary>
/// 전투 페이즈 데이터
/// </summary>
[Serializable]
public struct CombatPhaseData
{
    public int phase;
    public PlayerCombatState playerState;
    public MonsterData monsterState;
    public float timer;

    public void Reset()
    {
        phase = 0;
        timer = 0;
    }
}

/// <summary>
/// 전투 중 플레이어 상태
/// </summary>
[Serializable]
public struct PlayerCombatState
{
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
}

/// <summary>
/// 몬스터 데이터
/// </summary>
[Serializable]
public struct MonsterData
{
    public string name;
    public int stage;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public int grade;
}
