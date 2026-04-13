using UnityEngine;

/// <summary>
/// 아이템 팩토리 - 아이템 생성을 담당
/// </summary>
public class ItemFactory : MonoBehaviour
{
    // TODO: Phase 2에서 구현

    /// <summary>
    /// 랜덤 아이템 생성
    /// </summary>
    public ItemData CreateRandomItem()
    {
        Debug.LogError("ItemFactory not implemented yet");
        return default;
    }

    /// <summary>
    /// 지정된 등급의 아이템 생성
    /// </summary>
    public ItemData CreateItemByGrade(int grade)
    {
        Debug.LogError("ItemFactory not implemented yet");
        return default;
    }

    /// <summary>
    /// 랜덤 장비 생성
    /// </summary>
    public EquipmentData CreateRandomEquipment()
    {
        Debug.LogError("ItemFactory not implemented yet");
        return default;
    }
}
