using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 전투 로그 매니저 (SRP 준수)
/// 전투 중 발생하는 이벤트를 로그로 표시합니다.
/// </summary>
public class CombatLogManager : MonoBehaviour
{
    private static CombatLogManager _instance;
    
    public static CombatLogManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CombatLogManager");
                _instance = go.AddComponent<CombatLogManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    private ScrollView _logScrollView;
    private IEventBus _eventBus;
    private const int MAX_LOG_ENTRIES = 50;
    private Queue<string> _logHistory = new Queue<string>();
    
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
    /// 전투 로그 초기화 (Web 버전의 gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, ...) 연동)
    /// </summary>
    public void Initialize(VisualElement root)
    {
        // CombatLog VisualElement 찾기
        var combatLogContainer = root.Q<VisualElement>("CombatLog");
        if (combatLogContainer == null)
        {
            Debug.LogWarning("[CombatLogManager] CombatLog VisualElement를 찾을 수 없습니다.");
            return;
        }
        
        // ScrollView가 자식에 있는지 확인
        _logScrollView = combatLogContainer.Q<ScrollView>();
        if (_logScrollView == null)
        {
            // 없으면 CombatLog을 ScrollView로 변경
            Debug.Log("[CombatLogManager] CombatLog을 ScrollView로 변경합니다.");
            combatLogContainer.Clear();
            _logScrollView = new ScrollView();
            _logScrollView.name = "CombatLogScrollView";
            _logScrollView.style.flexGrow = 1;
            combatLogContainer.Add(_logScrollView);
        }
        
        // 이벤트 버스 연결 (이벤트 구독)
        _eventBus = Bootstrap.Container?.Resolve<IEventBus>() ?? EventBus.Instance;
        if (_eventBus != null)
        {
            _eventBus.On(GameEvents.GOLD_CHANGED, OnGoldChanged);
            _eventBus.On(GameEvents.ITEM_ACQUIRED, OnItemAcquired);
            _eventBus.On(GameEvents.ITEM_DISCOVERED, OnItemDiscovered);
            _eventBus.On(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
            _eventBus.On(GameEvents.GEM_CHANGED, OnGemChanged);
        }
    }
    
    private void OnGoldChanged()
    {
        if (Bootstrap.Container == null) return;
        var state = Bootstrap.Container.Resolve<IGameState>();
        AddLog($"골드: {state.Player.gold}", LogType.Gold);
    }
    
    private void OnItemAcquired()
    {
        AddLog("아이템을 획득했습니다!", LogType.Item);
    }
    
    private void OnItemDiscovered()
    {
        AddLog("새로운 아이템을 발견했습니다!", LogType.Item);
    }
    
    private void OnPlayerLevelUp()
    {
        if (Bootstrap.Container == null) return;
        var state = Bootstrap.Container.Resolve<IGameState>();
        AddLog($"레벨업! Lv.{state.Player.level}", LogType.Info);
    }
    
    private void OnGemChanged()
    {
        if (Bootstrap.Container == null) return;
        var state = Bootstrap.Container.Resolve<IGameState>();
        AddLog($"보석: {state.Player.gems}", LogType.Item);
    }
    
    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Off(GameEvents.GOLD_CHANGED, OnGoldChanged);
            _eventBus.Off(GameEvents.ITEM_ACQUIRED, OnItemAcquired);
            _eventBus.Off(GameEvents.ITEM_DISCOVERED, OnItemDiscovered);
            _eventBus.Off(GameEvents.PLAYER_LEVEL_UP, OnPlayerLevelUp);
            _eventBus.Off(GameEvents.GEM_CHANGED, OnGemChanged);
        }
    }
    
    /// <summary>
    /// 로그 추가
    /// </summary>
    public void AddLog(string message, LogType type = LogType.Info)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        string formattedMessage = FormatLogMessage(message, type);
        
        _logHistory.Enqueue(formattedMessage);
        
        // 최대 로그 수 제한
        while (_logHistory.Count > MAX_LOG_ENTRIES)
        {
            _logHistory.Dequeue();
        }
        
        // UI 업데이트
        UpdateLogDisplay();
    }
    
    private string FormatLogMessage(string message, LogType type)
    {
        string prefix = type switch
        {
            LogType.Damage => "💥",
            LogType.Heal => "💚",
            LogType.Gold => "💰",
            LogType.Exp => "✨",
            LogType.Item => "🎁",
            LogType.Boss => "👹",
            _ => "📝"
        };
        
        return $"{prefix} {message}";
    }
    
    private void UpdateLogDisplay()
    {
        if (_logScrollView == null) return;
        
        _logScrollView.Clear();
        
        foreach (var log in _logHistory)
        {
            var label = new Label(log);
            label.style.fontSize = 28;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            _logScrollView.Add(label);
        }
        
        // 자동으로 스크롤을 맨 아래로
        _logScrollView.verticalScroller.value = _logScrollView.verticalScroller.highValue;
    }
    
    /// <summary>
    /// 로그 지우기
    /// </summary>
    public void Clear()
    {
        _logHistory.Clear();
        UpdateLogDisplay();
    }
    
    public enum LogType
    {
        Info,
        Damage,
        Heal,
        Gold,
        Exp,
        Item,
        Boss
    }
}
