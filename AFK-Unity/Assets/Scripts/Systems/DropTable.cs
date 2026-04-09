using UnityEngine;

/// <summary>
/// 드롭 테이블 - 아이템 드롭 확률 관리
/// </summary>
public class DropTable : MonoBehaviour
{
    // TODO: Phase 2에서 구현

    /// <summary>
    /// 드롭 아이템 결정
    /// </summary>
    public ItemData GetDrop(int monsterGrade)
    {
        Debug.LogError("DropTable not implemented yet");
        return default;
    }

    /// <summary>
    /// 골드 드롭량 계산
    /// </summary>
    public int GetGoldDrop(int monsterGrade)
    {
        Debug.LogError("DropTable not implemented yet");
        return 0;
    }
}
