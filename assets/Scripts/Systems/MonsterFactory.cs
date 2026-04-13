using UnityEngine;

/// <summary>
/// 몬스터 팩토리 - 몬스터 생성 및 스펙 계산을 담당 (SRP 준수)
/// CombatSystem에서 몬스터 생성 책임을 분리
/// </summary>
public class MonsterFactory
{
    // 몬스터 종류
    private static readonly string[] MonsterTypes = new string[] { "슬라임", "고블린", "오크", "트롤", "드래곤" };
    private static readonly string[] MonsterPrefixes = new string[] { "작은 ", "일반 ", "강한 ", "엘리트 ", "보스 " };

    /// <summary>
    /// 스테이지에 맞는 몬스터 생성
    /// </summary>
    public MonsterData CreateMonster(int stage, bool isBoss = false)
    {
        // 보스 여부 판정 (10스테이지마다)
        bool bossStage = (stage % 10 == 0);
        bool actualIsBoss = isBoss || bossStage;

        // 몬스터 스펙 계산
        MonsterData monster = CalculateMonsterStats(stage, actualIsBoss);
        
        // 몬스터 이름 생성
        monster.name = GenerateMonsterName(stage, actualIsBoss);
        
        return monster;
    }

    /// <summary>
    /// 보스 몬스터 생성
    /// </summary>
    public MonsterData CreateBoss(int stage)
    {
        return CreateMonster(stage, isBoss: true);
    }

    /// <summary>
    /// 몬스터 스탯 계산 (Web 버전과 동일: 1.1^(stage-1) 스케일링)
    /// </summary>
    private MonsterData CalculateMonsterStats(int stage, bool isBoss)
    {
        // Web 버전과 동일한 스케일링 공식: base * 1.1^(stage-1)
        float stageMultiplier = Mathf.Pow(GameConfig.MonsterStatPerStage, stage - 1);
        
        // 기본 스탯 (Web 버전 monsters.csv 기준)
        float baseHP = 50f;  // slime hp_base
        float baseAttack = 5f; // slime atk_base
        float baseDefense = 5f;
        
        float hp = baseHP * stageMultiplier;
        float attack = baseAttack * stageMultiplier;
        float defense = baseDefense * stageMultiplier;
        
        // 보스 배율 (Web 버전: 보스 스테이지만)
        if (isBoss)
        {
            hp *= GameConfig.BossStatMultiplier; // 3x
            attack *= GameConfig.BossStatMultiplier;
            defense *= GameConfig.BossStatMultiplier;
        }
        
        // 몬스터 등급 결정 (Web 버전: stage 기반)
        int grade = isBoss ? 3 : GetMonsterGrade(stage);
        
        // 등급별 스탯 보정
        float gradeMult = GameConfig.GradeStatMultipliers[Mathf.Min(grade, 4)];
        hp *= gradeMult;
        attack *= gradeMult;
        defense *= gradeMult;
        
        return new MonsterData
        {
            stage = stage,
            currentHP = hp,
            maxHP = hp,
            attack = attack,
            defense = defense,
            grade = grade
        };
    }

    /// <summary>
    /// 몬스터 등급 결정 (스테이지 기반 확률)
    /// </summary>
    private int GetMonsterGrade(int stage)
    {
        // 스테이지가 높을수록 고등급 몬스터 등장 확률 증가
        // 단순화: 스테이지 / 5로 등급 결정 (최대 4)
        int baseGrade = Mathf.Min(stage / 5, 4);
        
        // 약간의 랜덤성 추가 (±1)
        int variance = Random.Range(-1, 2);
        return Mathf.Clamp(baseGrade + variance, 0, 4);
    }

    /// <summary>
    /// 몬스터 이름 생성
    /// </summary>
    private string GenerateMonsterName(int stage, bool isBoss)
    {
        int typeIndex = Mathf.Min(stage / 10, MonsterTypes.Length - 1);
        string prefix = isBoss ? "보스 " : MonsterPrefixes[Mathf.Min(stage / 5, MonsterPrefixes.Length - 1)];
        
        return prefix + MonsterTypes[typeIndex];
    }

    /// <summary>
    /// 몬스터 공격 속도 계산 (몬스터 종류별)
    /// </summary>
    public float GetMonsterAttackSpeed(MonsterData monster)
    {
        // 기본 공격 속도 (초당 공격 횟수)
        // 등급에 따라 약간 변동
        float baseSpeed = 1f;
        float gradeBonus = monster.grade * 0.1f;
        
        return baseSpeed + gradeBonus;
    }
}
