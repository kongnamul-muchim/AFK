using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 게임 전반의 이벤트를 관리하는 싱글톤 클래스
/// C# delegates와 events를 사용하여 느슨한 결합의 이벤트 시스템 구현
/// DIP 준수: IEventBus 인터페이스 구현
/// </summary>
public class EventBus : MonoBehaviour, IEventBus
{
    private static EventBus _instance;
    
    /// <summary>
    /// EventBus의 싱글톤 인스턴스
    /// </summary>
    public static EventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EventBus");
                _instance = go.AddComponent<EventBus>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// 이벤트명과 콜백 리스트를 저장하는 딕셔너리
    /// </summary>
    private Dictionary<string, List<Action>> _eventListeners = new Dictionary<string, List<Action>>();

    // ========== MonoBehaviour 라이프사이클 ==========

    private void Awake()
    {
        // 싱글톤 인스턴스 관리
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지: 객체 파괴 시 모든 리스너 제거
        _eventListeners.Clear();
    }

    // ========== 이벤트 관리 메서드 ==========

    /// <summary>
    /// 이벤트 등록
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    /// <param name="callback">실행할 콜백</param>
    public void On(string eventName, Action callback)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            GameLogger.Error("빈 이벤트명으로 등록 시도");
            return;
        }

        if (!_eventListeners.ContainsKey(eventName))
        {
            _eventListeners[eventName] = new List<Action>();
        }
        
        _eventListeners[eventName].Add(callback);
        GameLogger.DebugLog($"이벤트 등록: {eventName} (리스너 수: {_eventListeners[eventName].Count})");
    }

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    /// <param name="callback">해제할 콜백</param>
    public void Off(string eventName, Action callback)
    {
        if (_eventListeners.ContainsKey(eventName))
        {
            bool removed = _eventListeners[eventName].Remove(callback);
            if (removed)
            {
                GameLogger.DebugLog($"이벤트 해제: {eventName}");
            }
        }
    }

    /// <summary>
    /// 이벤트 발생
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    public void Emit(string eventName)
    {
        if (_eventListeners.ContainsKey(eventName))
        {
            // 복사본을 만들어 순회 (이벤트 처리 중 등록/해제 방지)
            var listeners = new List<Action>(_eventListeners[eventName]);
            
            foreach (var listener in listeners)
            {
                try
                {
                    listener?.Invoke();
                }
                catch (Exception e)
                {
                    GameLogger.Error($"이벤트 발생 중 오류 ({eventName}): {e.Message}");
                }
            }
            
            GameLogger.DebugLog($"이벤트 발생: {eventName} (리스너 수: {listeners.Count})");
        }
    }

    /// <summary>
    /// 1회용 이벤트 등록
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    /// <param name="callback">실행할 콜백</param>
    public void Once(string eventName, Action callback)
    {
        Action wrapper = null;
        wrapper = () =>
        {
            try
            {
                callback?.Invoke();
            }
            finally
            {
                Off(eventName, wrapper);
            }
        };
        On(eventName, wrapper);
    }

    /// <summary>
    /// 특정 이벤트의 리스너 존재 여부 확인
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    /// <returns>리스너가 있으면 true</returns>
    public bool HasListeners(string eventName)
    {
        return _eventListeners.ContainsKey(eventName) && _eventListeners[eventName].Count > 0;
    }

    /// <summary>
    /// 특정 이벤트의 리스너 수 반환
    /// </summary>
    /// <param name="eventName">이벤트명</param>
    /// <returns>리스너 수</returns>
    public int GetListenerCount(string eventName)
    {
        return _eventListeners.ContainsKey(eventName) ? _eventListeners[eventName].Count : 0;
    }

    /// <summary>
    /// 모든 이벤트 초기화 (테스트용)
    /// </summary>
    public void ClearAll()
    {
        _eventListeners.Clear();
    }

    // ========== IEventBus 인터페이스 구현 ==========
    
    void IEventBus.Clear()
    {
        _eventListeners.Clear();
    }
}

/// <summary>
/// 게임 이벤트 상수 클래스
/// 모든 이벤트명은 여기서 정의하여 일관성 유지
/// </summary>
public static class GameEvents
{
    // ========== 플레이어 관련 이벤트 ==========
    
    /// <summary>플레이어가 레벨업했을 때</summary>
    public const string PLAYER_LEVEL_UP = "PLAYER_LEVEL_UP";
    
    /// <summary>플레이어 스탯이 변경되었을 때</summary>
    public const string PLAYER_STAT_CHANGED = "PLAYER_STAT_CHANGED";
    
    /// <summary>플레이어가 사망했을 때</summary>
    public const string PLAYER_DEATH = "PLAYER_DEATH";
    
    /// <summary>플레이어가 부활했을 때</summary>
    public const string PLAYER_REVIVE = "PLAYER_REVIVE";

    // ========== 스테이지 관련 이벤트 ==========
    
    /// <summary>스테이지를 클리어했을 때</summary>
    public const string STAGE_CLEAR = "STAGE_CLEAR";
    
    /// <summary>스테이지에 진입했을 때</summary>
    public const string STAGE_ENTERED = "STAGE_ENTERED";
    
    /// <summary>최대 스테이지 기록을 갱신했을 때</summary>
    public const string STAGE_RECORD_UPDATED = "STAGE_RECORD_UPDATED";

    // ========== 전투 관련 이벤트 ==========
    
    /// <summary>몬스터를 처치했을 때</summary>
    public const string MONSTER_KILL = "MONSTER_KILL";
    
    /// <summary>보스를 처치했을 때</summary>
    public const string BOSS_KILL = "BOSS_KILL";
    
    /// <summary>전투 페이즈가 변경되었을 때</summary>
    public const string COMBAT_PHASE_CHANGED = "COMBAT_PHASE_CHANGED";
    
    /// <summary>전투가 시작되었을 때 (몬스터 조우)</summary>
    public const string COMBAT_ENCOUNTER = "COMBAT_ENCOUNTER";
    
    /// <summary>전투에서 승리했을 때</summary>
    public const string COMBAT_VICTORY = "COMBAT_VICTORY";
    
    /// <summary>전투에서 패배했을 때</summary>
    public const string COMBAT_DEFEAT = "COMBAT_DEFEAT";

    // ========== 아이템 관련 이벤트 ==========
    
    /// <summary>아이템을 획득했을 때</summary>
    public const string ITEM_ACQUIRED = "ITEM_ACQUIRED";
    
    /// <summary>아이템을 합성했을 때</summary>
    public const string ITEM_SYNTHESIZED = "ITEM_SYNTHESIZED";
    
    /// <summary>아이템을 장착했을 때</summary>
    public const string ITEM_EQUIPPED = "ITEM_EQUIPPED";
    
    /// <summary>아이템을 해제했을 때</summary>
    public const string ITEM_UNEQUIPPED = "ITEM_UNEQUIPPED";
    
    /// <summary>아이템을 버렸을 때</summary>
    public const string ITEM_DISCARDED = "ITEM_DISCARDED";
    
    /// <summary>새 아이템을 발견했을 때 (도감용)</summary>
    public const string ITEM_DISCOVERED = "ITEM_DISCOVERED";

    // ========== 재화 관련 이벤트 ==========
    
    /// <summary>골드가 변경되었을 때</summary>
    public const string GOLD_CHANGED = "GOLD_CHANGED";
    
    /// <summary>보석이 변경되었을 때</summary>
    public const string GEM_CHANGED = "GEM_CHANGED";

    // ========== 미션 관련 이벤트 ==========
    
    /// <summary>일일 미션이 진행되었을 때</summary>
    public const string DAILY_MISSION_PROGRESS = "DAILY_MISSION_PROGRESS";
    
    /// <summary>일일 미션이 완료되었을 때</summary>
    public const string DAILY_MISSION_COMPLETED = "DAILY_MISSION_COMPLETED";
    
    /// <summary>일일 미션 보상을 수령했을 때</summary>
    public const string DAILY_MISSION_CLAIMED = "DAILY_MISSION_CLAIMED";
    
    /// <summary>주간 미션이 초기화되었을 때</summary>
    public const string WEEKLY_MISSIONS_RESET = "WEEKLY_MISSIONS_RESET";
    
    /// <summary>주간 미션이 완료되었을 때</summary>
    public const string WEEKLY_MISSION_COMPLETED = "WEEKLY_MISSION_COMPLETED";
    
    /// <summary>주간 미션 보상을 수령했을 때</summary>
    public const string WEEKLY_MISSION_CLAIMED = "WEEKLY_MISSION_CLAIMED";

    // ========== 업그레이드 관련 이벤트 ==========
    
    /// <summary>보석 업그레이드를 했을 때</summary>
    public const string GEM_UPGRADED = "GEM_UPGRADED";
    
    /// <summary>스탯을 올렸을 때</summary>
    public const string STAT_POINT_USED = "STAT_POINT_USED";

    // ========== 기타 이벤트 ==========
    
    /// <summary>게임 통계가 변경되었을 때</summary>
    public const string STATS_CHANGED = "STATS_CHANGED";
    
    /// <summary>오프라인 보상을 수령했을 때</summary>
    public const string OFFLINE_REWARDS_CLAIMED = "OFFLINE_REWARDS_CLAIMED";
    
    /// <summary>환생했을 때</summary>
    public const string REBIRTH_PERFORMED = "REBIRTH_PERFORMED";
    
    /// <summary>튜토리얼 스텝을 완료했을 때</summary>
    public const string TUTORIAL_STEP_COMPLETED = "TUTORIAL_STEP_COMPLETED";
    
    /// <summary>게임 설정이 변경되었을 때</summary>
    public const string SETTINGS_CHANGED = "SETTINGS_CHANGED";
    
    /// <summary>UI 패널이 열렸을 때</summary>
    public const string UI_PANEL_OPENED = "UI_PANEL_OPENED";
    
    /// <summary>UI 패널이 닫혔을 때</summary>
    public const string UI_PANEL_CLOSED = "UI_PANEL_CLOSED";
    
    /// <summary>게임이 일시정지되었을 때</summary>
    public const string GAME_PAUSED = "GAME_PAUSED";
    
    /// <summary>게임이 재개되었을 때</summary>
    public const string GAME_RESUMED = "GAME_RESUMED";
    
    /// <summary>게임이 저장되었을 때</summary>
    public const string GAME_SAVED = "GAME_SAVED";
    
    /// <summary>게임이 로드되었을 때</summary>
    public const string GAME_LOADED = "GAME_LOADED";
}
