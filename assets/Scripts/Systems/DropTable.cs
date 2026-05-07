using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드롭 테이블 - 아이템/골드 드롭 로직을 담당 (SRP 준수)
/// CombatSystem에서 드롭 관련 책임을 분리
/// Web 버전과 동일한 드롭 로직 구현
/// </summary>
public class DropTable
{
    // Web 버전과 동일한 타입 (items.csv의 type과 일치)
    private static readonly string[] ItemTypes = new string[] { "weapon", "armor", "boots", "accessory" };
    
    // Web 버전의 gradeProbabilities (선형 감소)
    // common 70%, rare 20%, epic 7%, legendary 2.5%, mythic 0.5%
    private static readonly float[] GradeProbabilities = new float[] { 0.70f, 0.20f, 0.07f, 0.025f, 0.005f };
    
    // rarity 인덱스 ->rarity 이름 (items.csv의 rarity와 일치)
    private static readonly string[] RarityNames = new string[] { "common", "rare", "epic", "legendary", "mythic" };
    
    // 등급 이름 (일반 이름)
    private static readonly string[] GradeNames = new string[] { "일반", "고급", "희귀", "영웅", "전설" };

    /// <summary>
    /// 드롭 아이템 결정 - Web 버전 rollItemDrop() 로직
    /// </summary>
    public ItemData? GetDrop(int monsterGrade, int stage)
    {
        // 드롭 확률 확인 (기본 30%)
        if (Random.value > GameConfig.ItemDropRate)
        {
            return null; // 드롭 없음
        }

        // Web 버전: stage >= 91이면 baseGrade = 21, 아니면 Math.ceil(stage / 10)
        int baseGrade;
        if (stage >= 91)
        {
            baseGrade = 21; // Mythril tier
        }
        else
        {
            baseGrade = Mathf.CeilToInt(stage / 10f);
        }
        
        // grade 범위: [baseGrade, baseGrade+1, baseGrade+2, baseGrade+3, baseGrade+4]
        int[] dropGrades = new int[] {
            baseGrade,
            baseGrade + 1,
            baseGrade + 2,
            baseGrade + 3,
            baseGrade + 4
        };
        
        // Web 버전: 선형 감소 확률 (70%, 20%, 7%, 2.5%, 0.5%)
        // Grade 랜덤 선택 (가중치 적용)
        int selectedGradeIndex = WeightedRandomIndex(GradeProbabilities);
        int selectedGrade = dropGrades[selectedGradeIndex];
        string selectedRarity = RarityNames[selectedGradeIndex];
        
        // Web 버전: 타입 랜덤 선택 (균등 25%)
        string selectedType = ItemTypes[Random.Range(0, ItemTypes.Length)];
        
        // items.csv에서 grade + type 매칭
        var itemsData = DataLoader.Load("items");
        var matchingItems = new List<Dictionary<string, object>>();
        
        foreach (var item in itemsData)
        {
            if (item.TryGetValue("grade", out var gradeObj) &&
                item.TryGetValue("type", out var typeObj))
            {
                int itemGrade = System.Convert.ToInt32(gradeObj);
                string itemType = typeObj.ToString();
                
                // grade와 type이 일치하면 추가
                if (itemGrade == selectedGrade && itemType == selectedType)
                {
                    matchingItems.Add(item);
                }
            }
        }
        
        // 일치하는 아이템이 없으면 null 반환
        if (matchingItems.Count == 0)
        {
            return null;
        }
        
        // 일치하는 아이템 중 무작위 선택
        var selectedItem = matchingItems[Random.Range(0, matchingItems.Count)];
        
        // ItemData 생성
        var itemData = new ItemData
        {
            id = selectedItem["id"].ToString(),
            name = selectedItem["name"].ToString(),
            grade = selectedGrade,
            rarity = selectedGradeIndex,
            type = selectedItem["type"].ToString(),  // ← CSV의 실제 type 사용
            count = 1
        };
        
        // stats JSON 파싱
        if (selectedItem.TryGetValue("stats", out var statsObj))
        {
            ParseStats(statsObj.ToString(), ref itemData);
        }
        
        return itemData;
    }

    private void ParseStats(string statsStr, ref ItemData item)
    {
        var parsed = JsonUtility.FromJson<ItemStatsJson>(statsStr);
        if (parsed != null)
        {
            item.attackBonus = parsed.attackBonus;
            item.defenseBonus = parsed.defenseBonus;
            item.healthBonus = parsed.healthBonus;
        }
    }

    /// <summary>
    /// 가중치 기반 랜덤 인덱스 선택 - Web 버전 weightedRandomIndex()
    /// </summary>
    private int WeightedRandomIndex(float[] probabilities)
    {
        float roll = Random.value;
        float cumulative = 0f;
        
        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (roll < cumulative)
            {
                return i;
            }
        }
        
        return probabilities.Length - 1; // 마지막 인덱스
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
    /// 등급 이름 가져오기 (Web 버전 호환)
    /// </summary>
    public string GetGradeName(int grade)
    {
        // grade를 rarity 인덱스로 변환 (grade 1-5: common/rare/epic/legendary/mythic)
        // grade 6-10: common/rare/epic/legendary/mythic
        // etc.
        int rarityIndex = (grade - 1) % 5;
        return GradeNames[rarityIndex];
    }
}
