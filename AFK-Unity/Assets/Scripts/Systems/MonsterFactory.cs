using UnityEngine;

/// <summary>
/// 몬스터 팩토리 - 몬스터 생성을 담당
/// </summary>
public class MonsterFactory : MonoBehaviour
{
    // TODO: Phase 2에서 구현

    /// <summary>
    /// 스테이지에 맞는 몬스터 생성
    /// </summary>
    public MonsterData CreateMonster(int stage)
    {
        Debug.LogError("MonsterFactory not implemented yet");
        return default;
    }

    /// <summary>
    /// 보스 몬스터 생성
    /// </summary>
    public MonsterData CreateBoss(int stage)
    {
        Debug.LogError("MonsterFactory not implemented yet");
        return default;
    }
}
