using UnityEngine;

/// <summary>
/// 드롭 테이블 - 아이템/골드 드롭 로직을 담당 (SRP 준수)
/// CombatSystem에서 드롭 관련 책임을 분리
/// </summary>
public class DropTable
{
    private static readonly string[] ItemTypes = new string[] { "sword", "armor", "boots", "accessory" };
    private static readonly string[] ItemNames = new string[] { "검", "방어구", "신발", "장신구" };
    private static readonly string[] GradePrefixes = new string[] { "일반 ", "고급 ", "희귀 ", "영웅 ", "전설 " };

    /// <summary>
    /// 드롭 아이템 결정
    /// </summary>
    public ItemData? GetDrop(int monsterGrade, int stage)
    {
        // 드롭 확률 확인
        if (Random.value > GameConfig.ItemDropRate)
        {
            return null; // 드롭 없음
        }

        // 등급 결정 (몬스터 등급 기반)
        int dropGrade = CalculateDropGrade(monsterGrade);

        // 아이템 생성
        string itemId = GenerateItemId(dropGrade);
        string itemName = GenerateItemName(dropGrade);

        return new ItemData
        {
            id = itemId,
            name = itemName,
            grade = dropGrade,
            count = 1
        };
    }

    /// <summary>
    /// 골드 드롭량 계산
    /// </summary>
    public int GetGoldDrop(int monsterGrade, int stage, bool isBoss)
    {
        int baseGold = 5 * stage;

        // 등급 보정
        float gradeMult = GameConfig.GradeStatMultipliers[monsterGrade];

        // 보스 보정
        if (isBoss)
        {
            gradeMult *= 3;
        }

        // 변동폭
        float variance = Random.Range(GameConfig.GoldDropVarianceMin, GameConfig.GoldDropVarianceMax);

        return Mathf.RoundToInt(baseGold * gradeMult * variance);
    }

    /// <summary>
    /// 경험치 보상 계산
    /// </summary>
    public long GetExpReward(int stage, bool isBoss)
    {
        long baseExp = 10 * stage;

        if (isBoss)
        {
            baseExp *= 5;
        }

        return baseExp;
    }

    /// <summary>
    /// 드롭 등급 결정 (몬스터 등급 기반)
    /// </summary>
    private int CalculateDropGrade(int monsterGrade)
    {
        // 몬스터 등급에 비례하되, 확률적으로 결정
        float[] rates = GetDropRates();
        
        float roll = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < rates.Length; i++)
        {
            cumulative += rates[i];
            if (roll < cumulative)
            {
                // 몬스터 등급 보정 (고등급 몬스터는 더 좋은 아이템 드롭)
                return Mathf.Min(i + (monsterGrade > 2 ? 1 : 0), rates.Length - 1);
            }
        }

        return 0;
    }

    /// <summary>
    /// 드롭 확률 테이블 가져오기
    /// </summary>
    private float[] GetDropRates()
    {
        // 일반(40%), 고급(30%), 희귀(20%), 영웅(8%), 전설(2%)
        return new float[] { 0.4f, 0.3f, 0.2f, 0.08f, 0.02f };
    }

    /// <summary>
    /// 아이템 ID 생성
    /// </summary>
    private string GenerateItemId(int grade)
    {
        string type = ItemTypes[Random.Range(0, ItemTypes.Length)];
        return $"{type}_grade{grade}_{Random.Range(1000, 9999)}";
    }

    /// <summary>
    /// 아이템 이름 생성
    /// </summary>
    private string GenerateItemName(int grade)
    {
        string prefix = GradePrefixes[grade];
        string type = ItemNames[Random.Range(0, ItemNames.Length)];
        return prefix + type;
    }

    /// <summary>
    /// 등급 이름 가져오기
    /// </summary>
    public string GetGradeName(int grade)
    {
        string[] names = new string[] { "일반", "고급", "희귀", "영웅", "전설" };
        return names[Mathf.Min(grade, names.Length - 1)];
    }
}
