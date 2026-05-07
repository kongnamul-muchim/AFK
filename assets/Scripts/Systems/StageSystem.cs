using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지 시스템을 관리하는 클래스
/// 스테이지 진행, 클리어, 몬스터 생성 등을 처리합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
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

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    /// <summary>
    /// ServiceLocator를 통한 의존성 주입
    /// </summary>
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;

        if (_gameState == null)
            _gameState = Bootstrap.Container.Resolve<IGameState>();
        if (_eventBus == null)
            _eventBus = Bootstrap.Container.Resolve<IEventBus>();
        if (_logger == null)
            _logger = Bootstrap.Container.Resolve<IGameLogger>();
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
        
        // 의존성 주입
        InjectDependencies();
    }

    // ========== 스테이지 관리 ==========
    
    /// <summary>
    /// 현재 스테이지로 이동
    /// </summary>
    /// <param name="stageNumber">이동할 스테이지 번호</param>
    public void EnterStage(int stageNumber)
    {
        if (stageNumber < 1)
        {
            _logger.Warn($"잘못된 스테이지 번호: {stageNumber}, 1로 보정");
            stageNumber = 1;
        }
        
        var stage = _gameState.Stage;
        stage.currentStage = stageNumber;
        
        // 클리어 배열 초기화 (필요시 확장)
        if (stage.clearedStages == null || stage.clearedStages.Length < stageNumber)
        {
            bool[] newCleared = new bool[stageNumber + 10];
            if (stage.clearedStages != null)
            {
                System.Array.Copy(stage.clearedStages, newCleared, stage.clearedStages.Length);
            }
            stage.clearedStages = newCleared;
        }
        _gameState.Stage = stage;
        
        _logger.Info($"스테이지 {stageNumber} 진입");
        
        // 스테이지 진입 이벤트
        _eventBus.Emit(GameEvents.STAGE_ENTERED);
        
        // 전투 시작
        CombatSystem.Instance.StartCombat();
    }

    /// <summary>
    /// 다음 스테이지로 이동
    /// </summary>
    public void NextStage()
    {
        int nextStage = _gameState.Stage.currentStage + 1;
        EnterStage(nextStage);
    }

    /// <summary>
    /// 스테이지 클리어 처리
    /// </summary>
    public void ClearStage()
    {
        int currentStage = _gameState.Stage.currentStage;
        
        // 클리어 플래그 설정
        if (_gameState.Stage.clearedStages != null && currentStage <= _gameState.Stage.clearedStages.Length)
        {
            var clearedStages = _gameState.Stage.clearedStages;
            clearedStages[currentStage - 1] = true;
            var stage = _gameState.Stage;
            stage.clearedStages = clearedStages;
            _gameState.Stage = stage;
        }
        
        // 최대 스테이지 업데이트
        if (currentStage >= _gameState.Stage.maxStage)
        {
            var stage2 = _gameState.Stage;
            stage2.maxStage = currentStage + 1;
            _gameState.Stage = stage2;
            _eventBus.Emit(GameEvents.STAGE_RECORD_UPDATED);
        }
        
        // 플레이어 HP 완전 회복
        var player = _gameState.Player;
        player.currentHP = _gameState.GetTotalHealth();
        _gameState.Player = player;
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.STAGE_CLEAR);
        _eventBus.Emit(GameEvents.STATS_CHANGED);
        
        _logger.Info($"스테이지 {currentStage} 클리어!");
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
        if (_gameState.Stage.clearedStages == null || stage > _gameState.Stage.clearedStages.Length)
        {
            return false;
        }
        
        return _gameState.Stage.clearedStages[stage - 1];
    }

    /// <summary>
    /// 최대 진입 가능 스테이지 가져오기
    /// </summary>
    public int GetMaxAvailableStage()
    {
        return _gameState.Stage.maxStage;
    }

    /// <summary>
    /// 자동 반복 모드 진입 - 10스테이지 하락 (Web enterAutoRepeat)
    /// </summary>
    public void EnterAutoRepeat()
    {
        int currentStage = _gameState.Stage.currentStage;
        int repeatStage = Mathf.Max(1, currentStage - 10);

        var stageData = _gameState.Stage;
        stageData.autoRepeat = true;
        stageData.currentStage = repeatStage;
        stageData.killsInStage = 0;
        _gameState.Stage = stageData;

        _logger.Info($"자동 반복 모드 진입 - {currentStage}층 → {repeatStage}층에서 다시 시작");
        EnterStage(repeatStage);
    }

    /// <summary>
    /// 자동 반복 모드 해제
    /// </summary>
    public void ExitAutoRepeat()
    {
        var stageData = _gameState.Stage;
        stageData.autoRepeat = false;
        _gameState.Stage = stageData;
        _logger.Debug("자동 반복 모드 해제");
    }

    /// <summary>
    /// 재도전 가능 여부 (Web canRetry)
    /// 플레이어 레벨 >= 보스 스테이지 / 2
    /// </summary>
    public bool CanRetry()
    {
        int bossStage = (_gameState.Stage.currentStage / 10) * 10;
        return _gameState.Player.level >= bossStage / 2;
    }
}
