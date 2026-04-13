using UnityEngine;

/// <summary>
/// 게임 설정 ScriptableObject
/// 런타임에 게임 밸런스 상수를 조정할 수 있음
/// Assets/ScriptableObjects/GameConfig.asset 에 에셋으로 생성
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfigSO : ScriptableObject
{
    // ========== 몬스터 기본 스탯 ==========
    [Header("몬스터 기본 스탯")]
    public float BaseMonsterHP = 100f;
    public float BaseMonsterAttack = 10f;
    public float BaseMonsterDefense = 5f;

    // ========== 플레이어 기본 스탯 ==========
    [Header("플레이어 기본 스탯")]
    public float BasePlayerHP = 200f;
    public float BasePlayerAttack = 15f;
    public float BasePlayerDefense = 8f;
    public float BasePlayerSpeed = 5f;
    [Range(0f, 100f)] public float BasePlayerCritChance = 5f;
    [Range(100f, 500f)] public float BasePlayerCritDamage = 50f;

    // ========== 레벨업 및 경험치 ==========
    [Header("레벨업 및 경험치")]
    public long ExpToLevelUp = 100;
    public float ExpMultiplier = 1.5f;
    public float StatPointPerLevel = 1f;

    // ========== 드롭률 ==========
    [Header("드롭률")]
    public float GoldDropRate = 0.8f;
    public float ItemDropRate = 0.3f;
    public float[] DropRates = new float[] { 0.70f, 0.20f, 0.07f, 0.025f, 0.005f };

    // ========== 오프라인 보상 ==========
    [Header("오프라인 보상")]
    public float OfflineRewardMultiplier = 0.1f;
    public float MaxOfflineTime = 8f;

    // ========== 자동 전투 ==========
    [Header("자동 전투")]
    public float AutoBattleDamageBonus = 0.5f;
    public float AutoBattleAttackInterval = 1f;

    // ========== 보석 업그레이드 ==========
    [Header("보석 업그레이드")]
    public float OfflineRewardBonusPerLevel = 0.02f;
    public float CritDamageBonusPerLevel = 0.02f;
    [Range(0f, 1f)] public float AutoBattleBonusPerLevel = 0.02f;
    public int AutoBattleMaxLevel = 50;
    public int RebirthBonusPerLevel = 1;
    public int RebirthBonusMaxLevel = 10;
    public float StatBonusPerLevel = 0.01f;
    public int DropRateMaxLevel = 20;

    // ========== 스테이지 ==========
    [Header("스테이지")]
    public float MonsterStatPerStage = 0.1f;
    public int BossStageInterval = 10;
    public float BossStatMultiplier = 3f;

    // ========== 환생 ==========
    [Header("환생")]
    public int MinRebirthLevel = 50;
    [Range(0f, 1f)] public float RebirthGoldRetention = 0.1f;
    [Range(0f, 1f)] public float RebirthStatBonus = 0.1f;

    // ========== 인벤토리 ==========
    [Header("인벤토리")]
    public int MaxInventorySlots = 100;
    public int EquipmentSlots = 3;
    public int SynthesisRequiredCount = 3;

    // ========== 미션 ==========
    [Header("미션")]
    public int DailyMissionMaxCount = 5;
    public int WeeklyMissionMaxCount = 3;

    // ========== UI ==========
    [Header("UI")]
    public float UIAnimationDuration = 0.3f;
    public float ToastDuration = 2f;

    // ========== 전투 ==========
    [Header("전투")]
    [Range(0f, 1f)] public float DamageVarianceMin = 0.9f;
    [Range(1f, 2f)] public float DamageVarianceMax = 1.1f;
    public float MonsterAttackSpeed = 1f;
    public float BaseExpReward = 50f;
    public float BaseGoldReward = 30f;
    public float DropRateRedistributionBonus = 0.01f;
    public float EquipmentBonusBase = 1.0f;
    public float MonsterCritDamage = 1.5f;

    // ========== 등급 ==========
    [Header("등급")]
    public string[] GradeNames = new[] { "일반", "고급", "희귀", "영웅", "전설" };
    public string[] GradePrefixes = new[] { "", "고급", "희귀", "영웅", "전설" };
    public float[] GradeStatMultipliers = new[] { 1f, 1.5f, 2f, 3f, 5f };

    // ========== 오프라인 ==========
    [Header("오프라인")]
    public int OfflineItemDropPerHour = 2;
    [Range(0f, 2f)] public float GoldDropVarianceMin = 0.8f;
    [Range(1f, 2f)] public float GoldDropVarianceMax = 1.2f;

    // ========== 싱글톤 인스턴스 ==========
    private static GameConfigSO _instance;
    
    /// <summary>
    /// GameConfigSO 인스턴스 (Resources 폴더에서 로드)
    /// </summary>
    public static GameConfigSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfigSO>("GameConfig");
                if (_instance == null)
                {
                    Debug.LogError("GameConfigSO를 Resources 폴더에서 찾을 수 없습니다!");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 등급 이름 가져오기
    /// </summary>
    public string GetGradeName(int grade)
    {
        if (grade >= 0 && grade < GradeNames.Length)
            return GradeNames[grade];
        return "알 수 없음";
    }
}
