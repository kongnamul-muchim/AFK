using System;

/// <summary>
/// 플레이어 기본 데이터
/// SRP 준수: 플레이어 관련 데이터만 담당
/// 초기화 로직 통합: 필드 초기값과 Initialize() 일관성 유지
/// </summary>
[Serializable]
public class PlayerData
{
    // ========== 기본 스탯 ==========
    public int level = 1;
    public long experience = 0;
    
    // HP
    public float currentHP = 100;
    public float maxHP = 100;
    
    // 전투 스탯
    public float attack = 15;
    public float defense = 8;
    public float health = 200;
    
    // 추가 스탯
    public float speed = 200;
    public float critChance = 0.05f;
    public float critDamage = 1.5f;
    
    // 재화
    public long gold = 0;
    public int gems = 0;
    
    // 환생
    public int rebirthCount = 0;
    public int statPoints = 0;
    
    // ========== 업그레이드 데이터 ==========
    
    /// <summary>골드 업그레이드 레벨</summary>
    public SerializableDictionary<string, int> goldUpgrades = new SerializableDictionary<string, int>();
    
    /// <summary>스탯 업그레이드 레벨</summary>
    public SerializableDictionary<string, int> statUpgrades = new SerializableDictionary<string, int>();

    /// <summary>
    /// 플레이어 데이터 초기화 (새 게임 시작 시)
    /// 모든 필드를 기본값으로 설정
    /// </summary>
    public void Initialize()
    {
        // 기본 스탯 초기화
        level = 1;
        experience = 0;
        currentHP = 100;
        maxHP = 100;
        attack = 15;
        defense = 8;
        health = 200;
        speed = 200;
        critChance = 0.05f;
        critDamage = 1.5f;
        gold = 0;
        gems = 0;
        rebirthCount = 0;
        statPoints = 0;
        
        // 업그레이드 초기화
        InitializeUpgrades();
    }

    /// <summary>
    /// 업그레이드 데이터 초기화
    /// </summary>
    private void InitializeUpgrades()
    {
        // 골드 업그레이드 초기화
        goldUpgrades = new SerializableDictionary<string, int>();
        goldUpgrades.Add("attack", 0);
        goldUpgrades.Add("defense", 0);
        goldUpgrades.Add("hp", 0);
        goldUpgrades.Add("hpRegen", 0);
        goldUpgrades.Add("attackSpeed", 0);
        goldUpgrades.Add("critChance", 0);
        goldUpgrades.Add("critDamage", 0);
        goldUpgrades.Add("decisiveChance", 0);
        goldUpgrades.Add("decisiveDamage", 0);
        goldUpgrades.Add("goldBonus", 0);
        goldUpgrades.Add("expBonus", 0);
        
        // 스탯 업그레이드 초기화
        statUpgrades = new SerializableDictionary<string, int>();
        statUpgrades.Add("attack", 0);
        statUpgrades.Add("defense", 0);
        statUpgrades.Add("hp", 0);
        statUpgrades.Add("hpRegen", 0);
        statUpgrades.Add("attackSpeed", 0);
        statUpgrades.Add("critChance", 0);
        statUpgrades.Add("critDamage", 0);
    }

    /// <summary>
    /// 환생 시 초기화 (일부 데이터 유지)
    /// rebirthCount 증가, 기본 스탯 초기화, 골드 일부 손실
    /// </summary>
    public void ResetForRebirth()
    {
        level = 1;
        experience = 0;
        currentHP = maxHP;
        gold = 0; // 환생 시 골드 초기화
        attack = 15;
        defense = 8;
        health = 200;
        rebirthCount++;
        statPoints = 0;
        
        // 업그레이드는 유지 (환생 보너스로 활용)
    }

    /// <summary>
    /// 스테이지 클리어 후 HP 회복
    /// </summary>
    public void RecoverHP()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// 데미지 입기
    /// </summary>
    /// <param name="damage">입을 데미지</param>
    /// <returns>남은 HP</returns>
    public float TakeDamage(float damage)
    {
        currentHP = Math.Max(0, currentHP - damage);
        return currentHP;
    }

    /// <summary>
    /// 경험치 추가 및 레벨업 확인 (Web 버전과 동일)
    /// </summary>
    /// <param name="amount">획득한 경험치</param>
    /// <returns>레벨업 여부</returns>
    public bool AddExperience(long amount)
    {
        experience += amount;
        
        // Web 버전과 동일: 1.2배 스케일
        long expNeeded = 100 * (long)Math.Pow(1.2, level - 1);
        bool leveledUp = false;
        
        while (experience >= expNeeded)
        {
            experience -= expNeeded;
            level++;
            statPoints += 1;
            expNeeded = 100 * (long)Math.Pow(1.2, level - 1); // 다음 레벨업에 필요한 EXP 재계산
            leveledUp = true;
            
            // HP 완전 회복 (Web 버전과 동일)
            currentHP = maxHP;
        }
        
        return leveledUp;
    }
}
