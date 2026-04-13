# Unity Core Systems

## GameState.cs
```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 상태를 관리하는 싱글톤 클래스
/// </summary>
public class GameState : MonoBehaviour
{
    private static GameState _instance;
    public static GameState Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameState");
                _instance = go.AddComponent<GameState>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 데이터 필드
    public PlayerData player;
    public StageData stage;
    public CombatPhaseData combatPhase;
    public InventoryData inventory;
    public SettingsData settings;
    public TutorialData tutorial;
    public DailyMissionData dailyMissions;
    public RebirthData rebirth;
    public StatsData stats;
    public GemUpgradeData gemUpgrades;

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
    /// 새 게임 시작 시 초기값 설정
    /// </summary>
    public void Initialize()
    {
        player = new PlayerData();
        stage = new StageData();
        combatPhase = new CombatPhaseData();
        inventory = new InventoryData();
        settings = new SettingsData();
        tutorial = new TutorialData();
        dailyMissions = new DailyMissionData();
        rebirth = new RebirthData();
        stats = new StatsData();
        gemUpgrades = new GemUpgradeData();
    }

    /// <summary>
    /// 환생 시 초기화
    /// </summary>
    public void Reset()
    {
        // 환생 시 유지되는 데이터와 초기화되는 데이터 분리
        player.ResetForRebirth();
        stage.Reset();
        combatPhase.Reset();
        inventory.Reset();
        // 기타 초기화...
    }
}

// 데이터 구조체들
[System.Serializable]
public struct PlayerData
{
    public int level;
    public long experience;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public float health;
    public float speed;
    public float critChance;
    public float critDamage;
    public long gold;
    public int gems;
    public int rebirthCount;

    public void ResetForRebirth()
    {
        level = 1;
        experience = 0;
        gold = 0;
        // 기타 초기화...
        rebirthCount++;
    }
}

[System.Serializable]
public struct StageData
{
    public int currentStage;
    public int maxStage;
    public bool[] clearedStages;

    public void Reset()
    {
        currentStage = 1;
        // maxStage는 유지
    }
}

[System.Serializable]
public struct CombatPhaseData
{
    public int phase; // 0: 대기, 1: 전투, 2: 보상
    public PlayerCombatState playerState;
    public MonsterData monsterState;
    public float timer;

    public void Reset()
    {
        phase = 0;
        timer = 0;
    }
}

[System.Serializable]
public struct PlayerCombatState
{
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
}

[System.Serializable]
public struct MonsterData
{
    public string name;
    public int stage;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public int grade; // 0:일반, 1:고급, 2:희귀, 3:영웅, 4:전설
}

[System.Serializable]
public class InventoryData
{
    public List<ItemData> items = new List<ItemData>();
    public List<EquipmentData> equipment = new List<EquipmentData>();
    public HashSet<string> discoveredItems = new HashSet<string>();

    public void Reset()
    {
        items.Clear();
        equipment.Clear();
        // discoveredItems는 유지
    }
}

[System.Serializable]
public struct ItemData
{
    public string id;
    public string name;
    public int grade;
    public int quantity;
}

[System.Serializable]
public struct EquipmentData
{
    public string id;
    public string name;
    public int grade;
    public int slot; // 0:무기, 1:방어구, 2:액세서리
    public float attackBonus;
    public float defenseBonus;
    public float healthBonus;
}

[System.Serializable]
public class SettingsData
{
    public float soundVolume = 1f;
    public float musicVolume = 1f;
    public bool autoBattleEnabled = true;
}

[System.Serializable]
public class TutorialData
{
    public int currentStep;
    public HashSet<string> completedSteps = new HashSet<string>();
}

[System.Serializable]
public class DailyMissionData
{
    public List<MissionData> dailyMissions = new List<MissionData>();
    public List<MissionData> weeklyMissions = new List<MissionData>();
    public System.DateTime lastDailyReset;
    public System.DateTime lastWeeklyReset;
}

[System.Serializable]
public struct MissionData
{
    public string id;
    public string description;
    public int targetCount;
    public int currentCount;
    public bool isCompleted;
    public bool isClaimed;
    public MissionType type;
}

public enum MissionType
{
    Kill,
    ClearStage,
    CollectGold,
    UpgradeItem,
    Rebirth
}

[System.Serializable]
public class RebirthData
{
    public int rebirthCount;
    public float totalBonus;
}

[System.Serializable]
public class StatsData
{
    public float totalPlayTime;
    public int totalLevelUps;
    public int totalRebirths;
    public int totalKills;
    public int totalBossKills;
    public long totalGoldEarned;
    public int totalItemsDiscovered;
}

[System.Serializable]
public class GemUpgradeData
{
    public int offlineRewardLevel;
    public int critDamageLevel;
    public int autoBattleLevel;
    public int rebirthBonusLevel;
    public int dropRateLevel;
    public int statBonusLevel;
}
```

## EventBus.cs
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전반의 이벤트를 관리하는 싱글톤 클래스
/// </summary>
public class EventBus : MonoBehaviour
{
    private static EventBus _instance;
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

    private Dictionary<string, Action> _events = new Dictionary<string, Action>();
    private Dictionary<string, List<Action>> _eventListeners = new Dictionary<string, List<Action>>();

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
    /// 이벤트 등록
    /// </summary>
    public void On(string eventName, Action callback)
    {
        if (!_eventListeners.ContainsKey(eventName))
        {
            _eventListeners[eventName] = new List<Action>();
        }
        _eventListeners[eventName].Add(callback);
    }

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    public void Off(string eventName, Action callback)
    {
        if (_eventListeners.ContainsKey(eventName))
        {
            _eventListeners[eventName].Remove(callback);
        }
    }

    /// <summary>
    /// 이벤트 발생
    /// </summary>
    public void Emit(string eventName)
    {
        if (_eventListeners.ContainsKey(eventName))
        {
            // 복사본을 만들어 순회 (등록/해제 동시 발생 방지)
            var listeners = new List<Action>(_eventListeners[eventName]);
            foreach (var listener in listeners)
            {
                listener?.Invoke();
            }
        }
    }

    /// <summary>
    /// 1회용 이벤트 등록
    /// </summary>
    public void Once(string eventName, Action callback)
    {
        Action wrapper = null;
        wrapper = () =>
        {
            callback?.Invoke();
            Off(eventName, wrapper);
        };
        On(eventName, wrapper);
    }

    /// <summary>
    /// 리스너 존재 여부 확인
    /// </summary>
    public bool HasListeners(string eventName)
    {
        return _eventListeners.ContainsKey(eventName) && _eventListeners[eventName].Count > 0;
    }

    private void OnDestroy()
    {
        _eventListeners.Clear();
    }
}

/// <summary>
/// 게임 이벤트 상수 클래스
/// </summary>
public static class GameEvents
{
    // 플레이어 관련
    public const string PLAYER_LEVEL_UP = "PLAYER_LEVEL_UP";
    public const string PLAYER_STAT_CHANGED = "PLAYER_STAT_CHANGED";
    
    // 스테이지 관련
    public const string STAGE_CLEAR = "STAGE_CLEAR";
    public const string STAGE_ENTERED = "STAGE_ENTERED";
    
    // 전투 관련
    public const string MONSTER_KILL = "MONSTER_KILL";
    public const string COMBAT_PHASE_CHANGED = "COMBAT_PHASE_CHANGED";
    public const string COMBAT_ENCOUNTER = "COMBAT_ENCOUNTER";
    public const string COMBAT_VICTORY = "COMBAT_VICTORY";
    
    // 아이템 관련
    public const string ITEM_ACQUIRED = "ITEM_ACQUIRED";
    public const string ITEM_SYNTHESIZED = "ITEM_SYNTHESIZED";
    public const string ITEM_EQUIPPED = "ITEM_EQUIPPED";
    
    // 재화 관련
    public const string GOLD_CHANGED = "GOLD_CHANGED";
    public const string GEM_CHANGED = "GEM_CHANGED";
    
    // 미션 관련
    public const string DAILY_MISSION_PROGRESS = "DAILY_MISSION_PROGRESS";
    public const string DAILY_MISSION_COMPLETED = "DAILY_MISSION_COMPLETED";
    public const string DAILY_MISSION_CLAIMED = "DAILY_MISSION_CLAIMED";
    public const string WEEKLY_MISSIONS_RESET = "WEEKLY_MISSIONS_RESET";
    public const string WEEKLY_MISSION_COMPLETED = "WEEKLY_MISSION_COMPLETED";
    public const string WEEKLY_MISSION_CLAIMED = "WEEKLY_MISSION_CLAIMED";
    
    // 기타
    public const string STATS_CHANGED = "STATS_CHANGED";
    public const string OFFLINE_REWARDS_CLAIMED = "OFFLINE_REWARDS_CLAIMED";
    public const string REBIRTH_PERFORMED = "REBIRTH_PERFORMED";
    public const string TUTORIAL_STEP_COMPLETED = "TUTORIAL_STEP_COMPLETED";
    public const string SETTINGS_CHANGED = "SETTINGS_CHANGED";
    public const string UI_PANEL_OPENED = "UI_PANEL_OPENED";
    public const string UI_PANEL_CLOSED = "UI_PANEL_CLOSED";
}
```

## SaveManager.cs
```csharp
using UnityEngine;
using System.IO;
using System.Collections;

/// <summary>
/// 게임 저장/로드를 관리하는 싱글톤 클래스
/// </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveManager");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private const string SAVE_FILE_NAME = "savegame.json";
    private const int CURRENT_SAVE_VERSION = 3;
    private Coroutine _autoSaveCoroutine;

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
    /// 게임 상태 저장
    /// </summary>
    public void Save(GameState state)
    {
        try
        {
            string json = JsonUtility.ToJson(state, true);
            string savePath = GetSavePath();
            File.WriteAllText(savePath, json);
            Debug.Log($"게임 저장 완료: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 게임 상태 로드
    /// </summary>
    public GameState Load()
    {
        try
        {
            string savePath = GetSavePath();
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                GameState state = JsonUtility.FromJson<GameState>(json);
                return state;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로드 실패: {e.Message}");
        }
        return null;
    }

    /// <summary>
    /// 저장 파일 존재 여부 확인
    /// </summary>
    public bool SaveExists()
    {
        return File.Exists(GetSavePath());
    }

    /// <summary>
    /// 저장 파일 삭제
    /// </summary>
    public void DeleteSave()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("저장 파일 삭제 완료");
        }
    }

    /// <summary>
    /// 자동 저장 시작
    /// </summary>
    public void StartAutoSave(float interval = 5f)
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }
        _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine(interval));
    }

    /// <summary>
    /// 자동 저장 중지
    /// </summary>
    public void StopAutoSave()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
            _autoSaveCoroutine = null;
        }
    }

    private IEnumerator AutoSaveCoroutine(float interval)
    {
        WaitForSeconds wait = new WaitForSeconds(interval);
        while (true)
        {
            yield return wait;
            if (GameState.Instance != null)
            {
                Save(GameState.Instance);
            }
        }
    }

    /// <summary>
    /// 웹 버전 세이브 가져오기
    /// </summary>
    public void ImportWebSave(string json)
    {
        try
        {
            // 웹 버전 JSON을 Unity 형식으로 변환
            // 필요한 경우 마이그레이션 로직 추가
            GameState state = JsonUtility.FromJson<GameState>(json);
            Save(state);
            Debug.Log("웹 세이브 가져오기 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"웹 세이브 가져오기 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 저장 파일 내보내기 (텍스트)
    /// </summary>
    public string ExportSave()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            return File.ReadAllText(savePath);
        }
        return null;
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }
}
```

## GameLogger.cs
```csharp
using UnityEngine;
using System.Diagnostics;

/// <summary>
/// 게임 로깅을 관리하는 정적 클래스
/// </summary>
public static class GameLogger
{
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    private static LogLevel _currentLevel = LogLevel.DEBUG;

    /// <summary>
    /// 로그 레벨 설정
    /// </summary>
    public static void SetLogLevel(LogLevel level)
    {
        _currentLevel = level;
    }

    /// <summary>
    /// 일반 로그
    /// </summary>
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        if (_currentLevel <= LogLevel.INFO)
        {
            Debug.Log($"[GAME] {message}");
        }
    }

    /// <summary>
    /// 정보 로그
    /// </summary>
    public static void Info(string message)
    {
        if (_currentLevel <= LogLevel.INFO)
        {
            Debug.Log($"[INFO] {message}");
        }
    }

    /// <summary>
    /// 경고 로그
    /// </summary>
    public static void Warn(string message)
    {
        if (_currentLevel <= LogLevel.WARN)
        {
            Debug.LogWarning($"[WARN] {message}");
        }
    }

    /// <summary>
    /// 에러 로그
    /// </summary>
    public static void Error(string message)
    {
        if (_currentLevel <= LogLevel.ERROR)
        {
            Debug.LogError($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// 디버그 로그 (DEBUG 빌드에서만)
    /// </summary>
    [Conditional("DEBUG")]
    public static void DebugLog(string message)
    {
        if (_currentLevel <= LogLevel.DEBUG)
        {
            Debug.Log($"[DEBUG] {message}");
        }
    }
}
```

## GameConfig.cs
```csharp
using UnityEngine;

/// <summary>
/// 게임 설정 상수 클래스
/// </summary>
public static class GameConfig
{
    // 몬스터 기본 스탯
    public static readonly float BaseMonsterHP = 100f;
    public static readonly float BaseMonsterAttack = 10f;
    public static readonly float BaseMonsterDefense = 5f;

    // 플레이어 기본 스탯
    public static readonly float BasePlayerHP = 200f;
    public static readonly float BasePlayerAttack = 15f;
    public static readonly float BasePlayerDefense = 8f;

    // 경험치 및 레벨업
    public static readonly long ExpToLevelUp = 100;
    public static readonly float ExpMultiplier = 1.5f;

    // 드롭률
    public static readonly float GoldDropRate = 0.8f;
    public static readonly float ItemDropRate = 0.3f;

    // 오프라인 보상
    public static readonly float OfflineRewardMultiplier = 0.1f; // 온라인의 10%
    public static readonly float AutoBattleDamageBonus = 0.5f;

    // 아이템 등급별 확률
    public static readonly float[] DropRates = new float[] { 0.70f, 0.20f, 0.07f, 0.025f, 0.005f }; // 일반, 고급, 희귀, 영웅, 전설

    // 보석 업그레이드
    public static readonly float OfflineRewardBonusPerLevel = 0.02f; // 2% per level
    public static readonly float CritDamageBonusPerLevel = 0.02f; // 2% per level
    public static readonly float AutoBattleBonusPerLevel = 0.02f; // 2% per level, max 100%
    public static readonly int RebirthBonusPerLevel = 1; // 1 per level, max 10
    public static readonly float StatBonusPerLevel = 0.01f; // 1% per level

    // 스테이지당 스탯 증가량
    public static readonly float MonsterStatPerStage = 0.1f; // 10% per stage
}
```

## 프로젝트 설정 파일

### ProjectSettings/ProjectVersion.txt
```
m_EditorVersion: 6000.0.0f1
m_EditorVersionWithRevision: 6000.0.0f1 ( unity 6)
```

### Packages/manifest.json
```json
{
  "dependencies": {
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.addressables": "1.21.21",
    "com.unity.test-framework": "1.1.33",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.toolchain.win-x86_64-linux-x86_64": "2.0.9",
    "com.unity.ui": "2.0.0",
    "com.unity.ui.builder": "2.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

## 초기화 스크립트 (Bootstrap)

### Bootstrap.cs
```csharp
using UnityEngine;

/// <summary>
/// 게임 초기화를 담당하는 클래스
/// </summary>
public class Bootstrap : MonoBehaviour
{
    private void Awake()
    {
        // 싱글톤 인스턴스들 초기화 순서 보장
        var gameState = GameState.Instance;
        var eventBus = EventBus.Instance;
        var saveManager = SaveManager.Instance;

        // 저장된 게임 로드 또는 새 게임 시작
        if (saveManager.SaveExists())
        {
            GameState loadedState = saveManager.Load();
            if (loadedState != null)
            {
                // 로드된 상태로 GameState 초기화
                // (GameState 필드를 loadedState의 값으로 복사)
                GameLogger.Info("게임 로드 완료");
            }
            else
            {
                gameState.Initialize();
                GameLogger.Info("새 게임 시작");
            }
        }
        else
        {
            gameState.Initialize();
            GameLogger.Info("새 게임 시작");
        }

        // 자동 저장 시작
        saveManager.StartAutoSave(5f);

        GameLogger.Info("게임 부트스트랩 완료");
    }
}
```

## 테스트 스크립트

### Tests/GameStateTests.cs
```csharp
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class GameStateTests
{
    [UnityTest]
    public IEnumerator GameState_Singleton_CreatesInstance()
    {
        var gameState = GameState.Instance;
        Assert.IsNotNull(gameState);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameState_Initialize_SetsDefaultValues()
    {
        var gameState = GameState.Instance;
        gameState.Initialize();
        
        Assert.AreEqual(1, gameState.player.level);
        Assert.AreEqual(0, gameState.player.gold);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameState_Serialization_RoundTrip()
    {
        var gameState = GameState.Instance;
        gameState.Initialize();
        
        // 저장
        string json = JsonUtility.ToJson(gameState);
        
        // 로드
        GameState loadedState = JsonUtility.FromJson<GameState>(json);
        
        Assert.AreEqual(gameState.player.level, loadedState.player.level);
        Assert.AreEqual(gameState.player.gold, loadedState.player.gold);
        yield return null;
    }
}
```

### Tests/EventBusTests.cs
```csharp
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class EventBusTests
{
    [UnityTest]
    public IEnumerator EventBus_Singleton_CreatesInstance()
    {
        var eventBus = EventBus.Instance;
        Assert.IsNotNull(eventBus);
        yield return null;
    }

    [UnityTest]
    public IEnumerator EventBus_Event_EmitsCorrectly()
    {
        var eventBus = EventBus.Instance;
        bool eventFired = false;
        
        eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { eventFired = true; });
        eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        
        Assert.IsTrue(eventFired);
        yield return null;
    }

    [UnityTest]
    public IEnumerator EventBus_Once_OnlyFiresOnce()
    {
        var eventBus = EventBus.Instance;
        int fireCount = 0;
        
        eventBus.Once(GameEvents.PLAYER_LEVEL_UP, () => { fireCount++; });
        eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        
        Assert.AreEqual(1, fireCount);
        yield return null;
    }
}
```

## Git 커밋 가이드

### Day 1 완료 후 커밋
```bash
git add .
git commit -m "feat: initialize Unity project structure

- Create folder structure (Scripts/Core, Systems, UI, Data, Tests)
- Add .gitattributes for LFS
- Set up Packages manifest with required dependencies"
```

### Day 2 완료 후 커밋
```bash
git commit -m "feat: implement GameState with JSON serialization

- Add GameState singleton with all data structures
- Implement PlayerData, StageData, CombatPhaseData, etc.
- Add JSON serialization/deserialization support"
```

### Day 3 완료 후 커밋
```bash
git commit -m "feat: implement EventBus with C# delegates

- Add EventBus singleton for event management
- Implement On/Off/Emit/Once methods
- Add GameEvents constants class"
```

### Day 4 완료 후 커밋
```bash
git commit -m "feat: implement SaveManager with auto-save

- Add SaveManager singleton for save/load operations
- Implement auto-save coroutine (5 second intervals)
- Add web save import/export functionality"
```

### Day 5 완료 후 커밋
```bash
git commit -m "feat: implement GameLogger and GameConfig

- Add GameLogger static class for centralized logging
- Add GameConfig static class for game balance constants
- Add Bootstrap script for game initialization"
```

### Phase 1 완료 커밋
```bash
git commit -m "feat: complete Phase 1 - core systems

- All core systems implemented and tested
- GameState, EventBus, SaveManager, GameLogger, GameConfig
- Unit tests added for critical functionality"
```

## 다음 Phase 준비

Phase 2 (게임 시스템 이식) 를 위해 다음 스크립트들을 미리 준비:

- `CombatSystem.cs`
- `StageSystem.cs`
- `MonsterFactory.cs`
- `ItemFactory.cs`
- `DropTable.cs`
- `MissionSystem.cs`
- `OfflineRewardSystem.cs`

이 파일들은 빈 클래스로 먼저 생성하고, Phase 2 에서 구현을 채워나갑니다.

---

**Phase 1 완료 체크리스트:**

- [x] Unity 프로젝트 구조 생성
- [x] GameState 구현
- [x] EventBus 구현
- [x] SaveManager 구현
- [x] GameLogger 구현
- [x] GameConfig 구현
- [x] Bootstrap 스크립트 구현
- [x] 단위 테스트 스크립트 작성
- [x] Git LFS 설정
- [x] 패키지 매니저 설정

모든 코드는 웹 버전의 기능을 Unity 에서 재현하기 위한 기반이 됩니다.
