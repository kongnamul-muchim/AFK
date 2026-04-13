using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 시스템을 관리하는 클래스
/// 신규 플레이어에게 게임 방법을 안내합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    private static TutorialSystem _instance;
    
    /// <summary>
    /// TutorialSystem의 싱글톤 인스턴스
    /// </summary>
    public static TutorialSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TutorialSystem");
                _instance = go.AddComponent<TutorialSystem>();
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
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
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

    private void OnEnable()
    {
        // 의존성 주입 확인
        InjectDependencies();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        _eventBus.On(GameEvents.ITEM_EQUIPPED, OnEquipItem);
        _eventBus.On(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
    }

    private void OnDisable()
    {
        _eventBus.Off(GameEvents.PLAYER_LEVEL_UP, OnLevelUp);
        _eventBus.Off(GameEvents.ITEM_EQUIPPED, OnEquipItem);
        _eventBus.Off(GameEvents.ITEM_SYNTHESIZED, OnSynthesize);
    }

    // ========== 튜토리얼 단계 ==========
    
    /// <summary>
    /// 튜토리얼 단계
    /// </summary>
    public enum TutorialStep
    {
        None = -1,
        Welcome = 0,           // 환영 메시지
        FirstCombat = 1,       // 첫 전투
        LevelUp = 2,           // 레벨업
        EquipItem = 3,         // 장비 장착
        Synthesize = 4,        // 아이템 합성
        Rebirth = 5,           // 환생
        Completed = 6          // 완료
    }

    /// <summary>
    /// 현재 튜토리얼 단계
    /// </summary>
    public TutorialStep CurrentStep
    {
        get
        {
            return (TutorialStep)_gameState.Tutorial.currentStep;
        }
    }

    /// <summary>
    /// 튜토리얼 초기화
    /// </summary>
    public void InitializeTutorial()
    {
        var tutorial = _gameState.Tutorial;
        tutorial.currentStep = 0;
        tutorial.completedSteps.Clear();
        _gameState.Tutorial = tutorial;
        
        _logger.Info("튜토리얼 초기화");
    }

    /// <summary>
    /// 튜토리얼 다음 단계로 진행
    /// </summary>
    public void AdvanceTutorial()
    {
        var tutorial = _gameState.Tutorial;
        
        // 현재 단계 완료 처리
        string currentStepId = GetStepId((TutorialStep)tutorial.currentStep);
        if (!tutorial.completedSteps.Contains(currentStepId))
        {
            tutorial.completedSteps.Add(currentStepId);
        }
        
        // 다음 단계로
        tutorial.currentStep++;
        _gameState.Tutorial = tutorial;
        
        _logger.Info($"튜토리얼 진행: 단계 {tutorial.currentStep}");
        
        // 단계별 처리
        OnStepChanged((TutorialStep)tutorial.currentStep);
        
        // 완료 확인
        if (tutorial.currentStep >= (int)TutorialStep.Completed)
        {
            CompleteTutorial();
        }
        
        _eventBus.Emit(GameEvents.TUTORIAL_STEP_COMPLETED);
    }

    /// <summary>
    /// 튜토리얼 완료
    /// </summary>
    private void CompleteTutorial()
    {
        var tutorial = _gameState.Tutorial;
        tutorial.currentStep = (int)TutorialStep.Completed;
        _gameState.Tutorial = tutorial;
        
        // 완료 보상
        var player = _gameState.Player;
        player.gold += 1000;
        player.gems += 10;
        _gameState.Player = player;
        
        _logger.Info("튜토리얼 완료! 보상: 골드 1000, 보석 10");
        
        _eventBus.Emit(GameEvents.GOLD_CHANGED);
        _eventBus.Emit(GameEvents.GEM_CHANGED);
    }

    /// <summary>
    /// 단계 ID 가져오기
    /// </summary>
    private string GetStepId(TutorialStep step)
    {
        return $"tutorial_{step.ToString().ToLower()}";
    }

    /// <summary>
    /// 단계 변경 시 처리
    /// </summary>
    private void OnStepChanged(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Welcome:
                ShowTutorialMessage("환영합니다! AFK RPG에 오신 것을 환영합니다.");
                break;
            case TutorialStep.FirstCombat:
                ShowTutorialMessage("전투는 자동으로 진행됩니다. 몬스터를 처치하여 경험치와 골드를 얻으세요.");
                break;
            case TutorialStep.LevelUp:
                ShowTutorialMessage("레벨업! 스탯이 자동으로 증가합니다.");
                break;
            case TutorialStep.EquipItem:
                ShowTutorialMessage("아이템을 얻었습니다! 장비 슬롯에 장착하여 스탯을 강화하세요.");
                break;
            case TutorialStep.Synthesize:
                ShowTutorialMessage("동일한 아이템 5개를 합성하여 더 높은 등급의 아이템으로 업그레이드하세요.");
                break;
            case TutorialStep.Rebirth:
                ShowTutorialMessage("레벨 50에 도달하면 환생할 수 있습니다. 환생 시 데이터가 초기화되지만 강력한 보너스를 얻습니다.");
                break;
            case TutorialStep.Completed:
                ShowTutorialMessage("튜토리얼 완료! 이제 자유롭게 게임을 즐기세요. 보상으로 골드 1000과 보석 10을 받았습니다.");
                break;
        }
    }

    /// <summary>
    /// 튜토리얼 메시지 표시
    /// </summary>
    private void ShowTutorialMessage(string message)
    {
        _logger.Info($"[튜토리얼] {message}");
        // UI 표시는 UI 시스템에서 처리
    }

    // ========== 이벤트 핸들러 ==========
    
    private void OnLevelUp()
    {
        if (CurrentStep == TutorialStep.FirstCombat)
        {
            if (_gameState.Player.level >= 2)
            {
                AdvanceTutorial();
            }
        }
    }

    private void OnEquipItem()
    {
        if (CurrentStep == TutorialStep.EquipItem)
        {
            AdvanceTutorial();
        }
    }

    private void OnSynthesize()
    {
        if (CurrentStep == TutorialStep.Synthesize)
        {
            AdvanceTutorial();
        }
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 튜토리얼 완료 여부
    /// </summary>
    /// <returns>완료되었으면 true</returns>
    public bool IsTutorialCompleted()
    {
        return CurrentStep == TutorialStep.Completed;
    }

    /// <summary>
    /// 튜토리얼 표시 중인지 확인
    /// </summary>
    /// <returns>표시 중이면 true</returns>
    public bool IsTutorialActive()
    {
        return CurrentStep != TutorialStep.Completed && CurrentStep != TutorialStep.None;
    }
}
