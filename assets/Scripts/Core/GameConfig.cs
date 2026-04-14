using UnityEngine;

/// <summary>
/// 게임 설정 상수 클래스
/// 모든 게임 밸런스 상수는 여기서 관리하며, ScriptableObject로 대체 가능
/// </summary>
public static class GameConfig
{
    // ========== 몬스터 기본 스탯 ==========
    
    /// <summary>몬스터 기본 체력 (스테이지 1 기준)</summary>
    public static readonly float BaseMonsterHP = 500f;
    
    /// <summary>몬스터 기본 공격력</summary>
    public static readonly float BaseMonsterAttack = 10f;
    
    /// <summary>몬스터 기본 방어력</summary>
    public static readonly float BaseMonsterDefense = 5f;

    // ========== 플레이어 기본 스탯 ==========
    
    /// <summary>플레이어 기본 체력 (레벨 1 기준)</summary>
    public static readonly float BasePlayerHP = 100f;
    
    /// <summary>플레이어 기본 공격력</summary>
    public static readonly float BasePlayerAttack = 10f;
    
    /// <summary>플레이어 기본 방어력</summary>
    public static readonly float BasePlayerDefense = 5f;
    
    /// <summary>플레이어 기본 이동속도</summary>
    public static readonly float BasePlayerSpeed = 100f;
    
    /// <summary>플레이어 기본 치명확률 (0-1)</summary>
    public static readonly float BasePlayerCritChance = 0.05f;
    
    /// <summary>플레이어 기본 치명피해 배율</summary>
    public static readonly float BasePlayerCritDamage = 1.5f;

    // ========== 레벨업 및 경험치 ==========
    
    /// <summary>레벨 1에서 레벨업에 필요한 경험치</summary>
    public static readonly long ExpToLevelUp = 100;
    
    /// <summary>레벨업마다 증가하는 경험치 배율</summary>
    public static readonly float ExpMultiplier = 1.2f;
    
    /// <summary>레벨당 스탯 포인트 증가량</summary>
    public static readonly float StatPointPerLevel = 1f;

    // ========== 드롭률 ==========
    
    /// <summary>골드 드롭률</summary>
    public static readonly float GoldDropRate = 0.8f;
    
    /// <summary>아이템 드롭률</summary>
    public static readonly float ItemDropRate = 0.3f;
    
    /// <summary>아이템 등급별 기본 드롭 확률 [일반, 고급, 희귀, 영웅, 전설]</summary>
    public static readonly float[] DropRates = new float[] { 0.70f, 0.20f, 0.07f, 0.025f, 0.005f };

    // ========== 오프라인 보상 ==========
    
    /// <summary>오프라인 보상 배율 (온라인 대비)</summary>
    public static readonly float OfflineRewardMultiplier = 0.1f;
    
    /// <summary>최대 오프라인 보상 시간 (시간)</summary>
    public static readonly float MaxOfflineTime = 24f;

    // ========== 자동 전투 ==========
    
    /// <summary>자동 전투 기본 데미지 배율</summary>
    public static readonly float AutoBattleDamageBonus = 0.5f;
    
    /// <summary>자동 전투 공격 간격 (초)</summary>
    public static readonly float AutoBattleAttackInterval = 1f;

    // ========== 보석 업그레이드 ==========
    
    /// <summary>오프라인 보상 증가량 (레벨당)</summary>
    public static readonly float OfflineRewardBonusPerLevel = 0.02f;
    
    /// <summary>치명타 피해 증가량 (레벨당)</summary>
    public static readonly float CritDamageBonusPerLevel = 0.02f;
    
    /// <summary>자동 전투 강화량 (레벨당, 최대 100%)</summary>
    public static readonly float AutoBattleBonusPerLevel = 0.02f;
    
    /// <summary>자동 전투 최대 레벨</summary>
    public static readonly int AutoBattleMaxLevel = 50;
    
    /// <summary>환생 보너스 증가량 (레벨당)</summary>
    public static readonly int RebirthBonusPerLevel = 1;
    
    /// <summary>환생 보너스 최대 레벨</summary>
    public static readonly int RebirthBonusMaxLevel = 10;
    
    /// <summary>기본 스탯 % 증가량 (레벨당)</summary>
    public static readonly float StatBonusPerLevel = 0.01f;
    
    /// <summary>드롭 확률 업 최대 레벨</summary>
    public static readonly int DropRateMaxLevel = 20;

    // ========== 스테이지 ==========
    
    /// <summary>스테이지당 몬스터 스탯 증가 배율 (1.1 = 10% 증가)</summary>
    public static readonly float MonsterStatPerStage = 1.1f;
    
    /// <summary>보스 스테이지 간격</summary>
    public static readonly int BossStageInterval = 10;
    
    /// <summary>보스 스탯 배율</summary>
    public static readonly float BossStatMultiplier = 3f;

    // ========== 환생 ==========
    
    /// <summary>환생 최소 레벨</summary>
    public static readonly int MinRebirthLevel = 50;
    
    /// <summary>환생 시 골드 유지 비율 (%)</summary>
    public static readonly float RebirthGoldRetention = 0.1f;
    
    /// <summary>환생당 스탯 보너스 (%)</summary>
    public static readonly float RebirthStatBonus = 0.1f;

    // ========== 인벤토리 ==========
    
    /// <summary>인벤토리 최대 슬롯 수</summary>
    public static readonly int MaxInventorySlots = 100;
    
    /// <summary>장비 슬롯 수 (무기, 방어구, 액세서리)</summary>
    public static readonly int EquipmentSlots = 3;
    
    /// <summary>아이템 합성에 필요한 개수</summary>
    public static readonly int SynthesisRequiredCount = 5;

    // ========== 미션 ==========
    
    /// <summary>일일 미션 최대 개수</summary>
    public static readonly int DailyMissionMaxCount = 5;
    
    /// <summary>주간 미션 최대 개수</summary>
    public static readonly int WeeklyMissionMaxCount = 3;
    
    /// <summary>일일 미션 초기화 시간 (한국 시간 기준)</summary>
    public static readonly System.TimeSpan DailyResetTime = new System.TimeSpan(0, 0, 0, 0); // 자정
    
    /// <summary>주간 미션 초기화 요일 (월요일)</summary>
    public static readonly System.DayOfWeek WeeklyResetDay = System.DayOfWeek.Monday;

    // ========== UI ==========
    
    /// <summary>UI 애니메이션 기본 지속 시간</summary>
    public static readonly float UIAnimationDuration = 0.3f;
    
    /// <summary>토스트 메시지 표시 시간</summary>
    public static readonly float ToastDuration = 2f;

    // ========== 전투 ==========
    
    /// <summary>최소 데미지</summary>
    public static readonly float MinDamage = 1f;
    
    /// <summary>데미지 변동폭 최소값</summary>
    public static readonly float DamageVarianceMin = 0.9f;
    
    /// <summary>데미지 변동폭 최대값</summary>
    public static readonly float DamageVarianceMax = 1.1f;
    
    /// <summary>몬스터 공격 속도 (초)</summary>
    public static readonly float MonsterAttackSpeed = 1f;
    
    /// <summary>기본 경험치 보상</summary>
    public static readonly float BaseExpReward = 50f;
    
    /// <summary>기본 골드 보상</summary>
    public static readonly float BaseGoldReward = 30f;
    
    /// <summary>드롭 확률 재분배 보너스</summary>
    public static readonly float DropRateRedistributionBonus = 0.01f;
    
    /// <summary>장비 보너스 기본값</summary>
    public static readonly float EquipmentBonusBase = 1.0f;
    
    /// <summary>몬스터 치명피해 배율</summary>
    public static readonly float MonsterCritDamage = 1.5f;

    // ========== 등급 ==========
    
    /// <summary>등급 이름 배열 [일반, 고급, 희귀, 영웅, 전설]</summary>
    public static readonly string[] GradeNames = new[] { "일반", "고급", "희귀", "영웅", "전설" };
    
    /// <summary>등급 접두사 배열 [일반, 고급, 희귀, 영웅, 전설]</summary>
    public static readonly string[] GradePrefixes = new[] { "", "고급", "희귀", "영웅", "전설" };
    
    /// <summary>등급별 스탯 배율 [일반, 고급, 희귀, 영웅, 전설]</summary>
    public static readonly float[] GradeStatMultipliers = new[] { 1f, 1.5f, 2f, 3f, 5f };

    // ========== 오프라인 ==========
    
    /// <summary>오프라인 아이템 드롭 시간당 개수</summary>
    public static readonly int OfflineItemDropPerHour = 2;
    
    /// <summary>골드 변동폭 최소값</summary>
    public static readonly float GoldDropVarianceMin = 0.8f;
    
    /// <summary>골드 변동폭 최대값</summary>
    public static readonly float GoldDropVarianceMax = 1.2f;
    
    /// <summary>
    /// 등급 이름 가져오기
    /// </summary>
    public static string GetGradeName(int grade)
    {
        if (grade >= 0 && grade < GradeNames.Length)
            return GradeNames[grade];
        return "알 수 없음";
    }
}
