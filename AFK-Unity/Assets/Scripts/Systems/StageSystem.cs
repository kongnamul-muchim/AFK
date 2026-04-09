using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지 시스템을 관리하는 클래스
/// 스테이지 진행, 클리어, 몬스터 생성 등을 처리합니다.
/// </summary>
public class StageSystem : MonoBehaviour
{
    private static StageSystem _instance;
    
    /// <summary>
    /// StageSystem의 싱글톤 인스턴스
    /// </summary>
    public static StageSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("StageSystem");
                _instance = go.AddComponent<StageSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 스테이지 관리 ==========
    
    /// <summary>
    /// 현재 스테이지로 이동
    /// </summary>
    /// <param name="stageNumber">이동할 스테이지 번호</param>
    public void EnterStage(int stageNumber)
    {
        GameState state = GameState.Instance;
        
        if (stageNumber < 1)
        {
            GameLogger.Error($"잘못된 스테이지 번호: {stageNumber}");
            return;
        }
        
        state.stage.currentStage = stageNumber;
        
        // 클리어 배열 초기화 (필요시 확장)
        if (state.stage.clearedStages == null || state.stage.clearedStages.Length < stageNumber)
        {
            bool[] newCleared = new bool[stageNumber + 10];
            if (state.stage.clearedStages != null)
            {
                System.Array.Copy(state.stage.clearedStages, newCleared, state.stage.clearedStages.Length);
            }
            state.stage.clearedStages = newCleared;
        }
        
        GameLogger.Info($"스테이지 {stageNumber} 진입");
        
        // 스테이지 진입 이벤트
        EventBus.Instance.Emit(GameEvents.STAGE_ENTERED);
        
        // 전투 시작
        CombatSystem.Instance.StartCombat();
    }

    /// <summary>
    /// 다음 스테이지로 이동
    /// </summary>
    public void NextStage()
    {
        GameState state = GameState.Instance;
        int nextStage = state.stage.currentStage + 1;
        
        EnterStage(nextStage);
    }

    /// <summary>
    /// 스테이지 클리어 처리
    /// </summary>
    public void ClearStage()
    {
        GameState state = GameState.Instance;
        int currentStage = state.stage.currentStage;
        
        // 클리어 플래그 설정
        if (state.stage.clearedStages != null && currentStage <= state.stage.clearedStages.Length)
        {
            state.stage.clearedStages[currentStage - 1] = true;
        }
        
        // 최대 스테이지 업데이트
        if (currentStage >= state.stage.maxStage)
        {
            state.stage.maxStage = currentStage + 1;
            EventBus.Instance.Emit(GameEvents.STAGE_RECORD_UPDATED);
        }
        
        // 플레이어 HP 완전 회복
        state.player.currentHP = state.GetTotalHealth();
        
        // 통계 업데이트
        state.stats.totalKills++;
        
        // 이벤트 발생
        EventBus.Instance.Emit(GameEvents.STAGE_CLEAR);
        EventBus.Instance.Emit(GameEvents.STATS_CHANGED);
        
        GameLogger.Info($"스테이지 {currentStage} 클리어!");
    }

    /// <summary>
    /// 보스 스테이지 여부 확인
    /// </summary>
    /// <param name="stage">스테이지 번호</param>
    /// <returns>보스 스테이지이면 true</returns>
    public bool IsBossStage(int stage)
    {
        return stage % 10 == 0;
    }

    /// <summary>
    /// 스테이지 클리어 여부 확인
    /// </summary>
    /// <param name="stage">스테이지 번호</param>
    /// <returns>클리어되었으면 true</returns>
    public bool IsStageCleared(int stage)
    {
        GameState state = GameState.Instance;
        
        if (state.stage.clearedStages == null || stage > state.stage.clearedStages.Length)
        {
            return false;
        }
        
        return state.stage.clearedStages[stage - 1];
    }

    /// <summary>
    /// 최대 진입 가능 스테이지 가져오기
    /// </summary>
    public int GetMaxAvailableStage()
    {
        GameState state = GameState.Instance;
        return state.stage.maxStage;
    }
}
