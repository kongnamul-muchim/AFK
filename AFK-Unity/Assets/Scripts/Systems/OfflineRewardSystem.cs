using UnityEngine;

/// <summary>
/// 오프라인 보상 시스템
/// </summary>
public class OfflineRewardSystem : MonoBehaviour
{
    // TODO: Phase 2에서 구현

    /// <summary>
    /// 오프라인 경과 시간 계산
    /// </summary>
    public float CalculateOfflineTime()
    {
        Debug.LogError("OfflineRewardSystem not implemented yet");
        return 0f;
    }

    /// <summary>
    /// 오프라인 보상 계산
    /// </summary>
    public OfflineRewards CalculateRewards(float offlineTime)
    {
        Debug.LogError("OfflineRewardSystem not implemented yet");
        return default;
    }

    /// <summary>
    /// 오프라인 보상 청구
    /// </summary>
    public void ClaimRewards()
    {
        Debug.LogError("OfflineRewardSystem not implemented yet");
    }
}

[System.Serializable]
public struct OfflineRewards
{
    public long gold;
    public int gems;
    public ItemData[] items;
}
