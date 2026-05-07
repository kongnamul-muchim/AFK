using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 튜토리얼 시스템 - CSV tutorial.csv 기반 가이드 및 조건 감지
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    private static TutorialSystem _instance;

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

    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;

    private List<Dictionary<string, object>> _tutorialData;
    private bool _isInitialized = false;

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

    /// <summary>
    /// 튜토리얼 시스템 초기화
    /// </summary>
    public void Initialize(IGameState gameState, IEventBus eventBus, IGameLogger logger)
    {
        _gameState = gameState;
        _eventBus = eventBus;
        _logger = logger;

        // 튜토리얼 데이터 로드
        _tutorialData = DataLoader.Load("tutorial");
        if (_tutorialData == null || _tutorialData.Count == 0)
        {
            // 튜토리얼 데이터 없음 → 완료 처리
            MarkTutorialComplete();
            return;
        }

        // 이미 완료된 튜토리얼
        if (IsTutorialComplete())
        {
            _logger.Debug("튜토리얼 이미 완료됨");
            return;
        }

        SetupEventListeners();
        _isInitialized = true;

        // 시작 단계가 0이면 1단계로 진행
        if (_gameState.Tutorial.currentStep == 0)
        {
            AdvanceToStep(1);
        }

        _logger.Debug("TutorialSystem 초기화 완료");
    }

    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        if (_eventBus == null) return;

        // 몬스터 처치
        _eventBus.On(GameEvents.MONSTER_KILL, () => CheckCondition("kill_count"));

        // 레벨업
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => CheckCondition("level"));

        // 합성
        _eventBus.On(GameEvents.ITEM_SYNTHESIZED, () => CheckCondition("synthesize"));

        // 보스 처치 (스테이지가 10의 배수에서 승리)
        _eventBus.On(GameEvents.STAGE_CLEAR, () =>
        {
            if (_gameState.Stage.currentStage % 10 == 0)
                CheckCondition("boss_defeat");
        });
    }

    /// <summary>
    /// 조건 타입에 따른 진행 확인
    /// </summary>
    private void CheckCondition(string conditionType)
    {
        if (!_isInitialized || IsTutorialComplete()) return;

        var currentTutorial = GetCurrentTutorialStep();
        if (currentTutorial == null) return;

        string condType = currentTutorial["condition_type"]?.ToString();
        if (condType != conditionType) return;

        // "none" 조건은 즉시 통과
        if (condType == "none")
        {
            AdvanceToStep(_gameState.Tutorial.currentStep + 1);
            return;
        }

        // 조건 값 확인
        if (!int.TryParse(currentTutorial["condition_value"]?.ToString(), out int conditionValue))
            return;

        int currentValue = GetCurrentConditionValue(conditionType);
        if (currentValue >= conditionValue)
        {
            AdvanceToStep(_gameState.Tutorial.currentStep + 1);
        }
    }

    /// <summary>
    /// 현재 조건 값 조회
    /// </summary>
    private int GetCurrentConditionValue(string conditionType)
    {
        switch (conditionType)
        {
            case "kill_count":
                return _gameState.Stats.totalKills;
            case "level":
                return _gameState.Player.level;
            case "boss_defeat":
                return 1; // 보스 처치 시 1
            default:
                return 0;
        }
    }

    /// <summary>
    /// 다음 단계로 진행
    /// </summary>
    private void AdvanceToStep(int step)
    {
        var tutorial = FindTutorialStep(step);

        if (tutorial == null)
        {
            // 마지막 단계 → 튜토리얼 완료
            CompleteTutorial();
            return;
        }

        // 현재 단계 업데이트
        var tutorialData = _gameState.Tutorial;
        tutorialData.currentStep = step;
        _gameState.Tutorial = tutorialData;

        // 보상 지급
        string rewardStr = tutorial["reward"]?.ToString();
        if (!string.IsNullOrEmpty(rewardStr))
        {
            GiveReward(rewardStr);
        }

        // UI 업데이트
        string guideMessage = tutorial["guide_message"]?.ToString() ?? "";
        UpdateTutorialUI(step, guideMessage, rewardStr);

        _logger.Info($"튜토리얼 단계 {step}: {guideMessage}");
    }

    /// <summary>
    /// 보상 지급
    /// </summary>
    private void GiveReward(string rewardStr)
    {
        if (string.IsNullOrEmpty(rewardStr)) return;

        // "gold:50" 형식
        var parts = rewardStr.Split(':');
        if (parts.Length != 2) return;

        string type = parts[0];
        if (!int.TryParse(parts[1], out int amount)) return;

        switch (type)
        {
            case "gold":
                _gameState.Player.gold += amount;
                _eventBus?.Emit(GameEvents.GOLD_CHANGED);
                break;
            case "exp":
                _gameState.Player.AddExperience(amount);
                break;
            case "sp":
                _gameState.Player.statPoints += amount;
                _eventBus?.Emit(GameEvents.PLAYER_STAT_CHANGED);
                break;
            case "item":
                // 아이템 지급 (추후 구현)
                _logger.Debug($"튜토리얼 아이템 지급: {parts[1]}");
                break;
        }

        _logger.Debug($"튜토리얼 보상: {type} {amount}");
    }

    /// <summary>
    /// 튜토리얼 UI 업데이트
    /// </summary>
    private void UpdateTutorialUI(int step, string message, string reward)
    {
        // 메시지 표시
        if (UIManager.Instance != null)
        {
            string displayText = message;
            if (!string.IsNullOrEmpty(reward))
            {
                displayText += $"\n\n보상: {reward}";
            }
            UIManager.Instance.UpdateTutorialMessage(displayText);
            UIManager.Instance.ShowTutorial();
        }

        // 이벤트 발생
        _eventBus?.Emit(GameEvents.TUTORIAL_STEP_COMPLETED);
    }

    /// <summary>
    /// 튜토리얼 완료 처리
    /// </summary>
    private void CompleteTutorial()
    {
        MarkTutorialComplete();

        // UI 숨김
        UIManager.Instance?.HideTutorial();
        CombatLogManager.Instance?.AddLog("튜토리얼이 완료되었습니다!");

        _logger.Info("튜토리얼 완료");
    }

    /// <summary>
    /// 튜토리얼 완료 표시
    /// </summary>
    private void MarkTutorialComplete()
    {
        var tutorialData = _gameState.Tutorial;
        tutorialData.currentStep = 99;
        _gameState.Tutorial = tutorialData;
        _isInitialized = false;
    }

    /// <summary>
    /// 튜토리얼 완료 여부
    /// </summary>
    public bool IsTutorialComplete()
    {
        return _gameState != null && _gameState.Tutorial.currentStep >= 99;
    }

    /// <summary>
    /// 현재 단계의 튜토리얼 데이터 조회
    /// </summary>
    private Dictionary<string, object> GetCurrentTutorialStep()
    {
        return FindTutorialStep(_gameState.Tutorial.currentStep);
    }

    /// <summary>
    /// 특정 단계의 튜토리얼 데이터 찾기
    /// </summary>
    private Dictionary<string, object> FindTutorialStep(int step)
    {
        if (_tutorialData == null) return null;

        return _tutorialData.FirstOrDefault(row =>
        {
            if (row.TryGetValue("step", out var stepObj) && stepObj != null)
            {
                return int.TryParse(stepObj.ToString(), out int s) && s == step;
            }
            return false;
        });
    }

    /// <summary>
    /// 튜토리얼 스킵
    /// </summary>
    public void SkipTutorial()
    {
        CompleteTutorial();
        _logger.Info("튜토리얼 스킵됨");
    }
}
