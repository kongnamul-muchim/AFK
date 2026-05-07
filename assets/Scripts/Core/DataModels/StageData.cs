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
    public bool autoRepeat;

    public void Reset()
    {
        currentStage = 1;
        maxStage = 1;
        autoRepeat = false;
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
        // 몬스터 상태 초기화 (새 전투 시작 시 죽은 상태로 등장하는 버그 방지)
        monsterState = new MonsterData
        {
            name = "",
            stage = 0,
            currentHP = 0,
            maxHP = 0,
            attack = 0,
            defense = 0,
            grade = 0
        };
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
