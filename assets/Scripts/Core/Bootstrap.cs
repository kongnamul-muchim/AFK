using UnityEngine;
using System;
using System.Collections.Generic;
using AFK.Core.DI;

/// <summary>
/// 게임 초기화를 담당하는 클래스
/// 게임 시작 시 DI 컨테이너를 초기화하고 서비스를 등록합니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class Bootstrap : MonoBehaviour
{
    public static IDIContainer Container { get; private set; }

    private static bool _isInitialized = false;

    private void Awake()
    {
        if (_isInitialized)
        {
            Destroy(gameObject);
            return;
        }
        _isInitialized = true;

        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        GameLogger.Info("게임 부트스트랩 시작...");

        Container = new DIContainer();

        var gameState = GameState.Instance;
        Container.RegisterInstance<IGameState>(gameState, ServiceLifetime.Singleton);

        var eventBus = EventBus.Instance;
        Container.RegisterInstance<IEventBus>(eventBus, ServiceLifetime.Singleton);

        var saveManager = SaveManager.Instance;
        Container.RegisterInstance<ISaveManager>(saveManager, ServiceLifetime.Singleton);

        Container.RegisterInstance<IGameLogger>(new GameLoggerAdapter(), ServiceLifetime.Singleton);

        GameLogger.DebugLog("DIContainer에 서비스 등록 완료");

        // 1.5 오디오 데이터베이스 초기화 (CSV 기반)
        AudioDatabase.Initialize();

        // 오디오 이벤트 구독
        var audioManager = AudioManager.Instance;
        audioManager.SubscribeToGameEvents(eventBus);

        // 2. 저장된 게임 로드 또는 새 게임 시작
        bool isNewGame = false;
        if (saveManager.SaveExists())
        {
            GameState loadedState = saveManager.Load();
            
            if (loadedState != null)
            {
                // 로드된 상태로 GameState 업데이트
                UpdateGameStateFromLoaded(loadedState);
                GameLogger.Info("저장된 게임 로드 완료");
            }
            else
            {
                // 로드 실패 시 새 게임 시작
                gameState.Initialize();
                GiveStarterItems(gameState);
                GameLogger.Warn("게임 로드 실패, 새 게임 시작");
                isNewGame = true;
            }
        }
        else
        {
            // 저장 파일 없음 - 새 게임 시작
            gameState.Initialize();
            GiveStarterItems(gameState);
            GameLogger.Info("새 게임 시작");
            isNewGame = true;
        }

        // 3. 자동 저장 시작
        saveManager.StartAutoSave(5f);

        // 3.5 튜토리얼 시스템 초기화
        TutorialSystem.Instance.Initialize(gameState, eventBus, Container.Resolve<IGameLogger>());

        // 4. 일일/주간 미션 생성 (저장 데이터가 없으면 신규 생성)
        if (GameState.Instance.dailyMissions.missions.Count == 0)
            DailyMissionSystem.Instance.GenerateDailyMissions();
        if (GameState.Instance.dailyMissions.weeklyMissions.Count == 0)
            DailyMissionSystem.Instance.GenerateWeeklyMissions();

        // 5. 초기 이벤트 발생
        eventBus.Emit(GameEvents.GAME_LOADED);

        // 6. 스테이지 진입 (새 게임이면 1, 불러오기면 저장된 스테이지)
        if (isNewGame)
        {
            StageSystem.Instance.EnterStage(1);
        }
        else
        {
            StageSystem.Instance.EnterStage(GameState.Instance.stage.currentStage);
        }

        GameLogger.Info("게임 부트스트랩 완료");
    }

    /// <summary>
    /// 로드된 GameState로 현재 GameState 업데이트
    /// </summary>
    /// <param name="loadedState">로드된 GameState</param>
    private void UpdateGameStateFromLoaded(GameState loadedState)
    {
        GameState currentState = GameState.Instance;
        
        // 각 필드 복사
        currentState.player = loadedState.player;
        currentState.stage = loadedState.stage;
        currentState.combatPhase = loadedState.combatPhase;
        currentState.inventory = loadedState.inventory;
        currentState.settings = loadedState.settings;
        currentState.tutorial = loadedState.tutorial;
        currentState.dailyMissions = loadedState.dailyMissions;
        currentState.rebirth = loadedState.rebirth;
        currentState.stats = loadedState.stats;
        currentState.gemUpgrades = loadedState.gemUpgrades;
    }

    /// <summary>
    /// 게임 재시작 (환생 또는 리셋 시)
    /// </summary>
    public void RestartGame()
    {
        GameLogger.Info("게임 재시작...");
        
        SaveManager.Instance.StopAutoSave();
        
        // GameState 초기화
        GameState.Instance.Initialize();
        
        // 저장 파일 삭제
        SaveManager.Instance.DeleteSave();
        
        // 다시 초기화
        InitializeGame();
    }

    /// <summary>
    /// 게임 종료 시 정리
    /// </summary>
    private void OnApplicationQuit()
    {
        GameLogger.Info("게임 종료 - 저장 중...");
        
        // 최종 저장
        if (GameState.Instance != null)
        {
            SaveManager.Instance.Save(GameState.Instance);
        }
        
        SaveManager.Instance.StopAutoSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            GameLogger.Info("게임 일시정지 - 저장 중...");
            SaveManager.Instance.Save(GameState.Instance);
            EventBus.Instance.Emit(GameEvents.GAME_PAUSED);
        }
        else
        {
            GameLogger.Info("게임 재개");
            EventBus.Instance.Emit(GameEvents.GAME_RESUMED);
        }
    }

    /// <summary>
    /// 새 게임 시작 시 초기 아이템 지급 (모든 아이템 슬롯 잠금 해제)
    /// </summary>
    private void GiveStarterItems(GameState state)
    {
        // CSV 데이터에서 모든 아이템을 읽어와서 count=0 으로 추가
        var itemsData = DataLoader.Load("items");
        if (itemsData.Count == 0)
        {
            GameLogger.Warn("items.csv 데이터를 불러올 수 없습니다. 기본 아이템만 추가합니다.");
            // 기본 아이템만 추가
            state.inventory.items.Add(new ItemData { id = "sword_iron_001", name = "Rusty Sword", grade = 0, count = 1, rarity = 0, type = "weapon", attackBonus = 2, defenseBonus = 0, healthBonus = 0 });
            state.inventory.items.Add(new ItemData { id = "armor_leather_001", name = "Leather Armor", grade = 0, count = 1, rarity = 0, type = "armor", attackBonus = 0, defenseBonus = 2, healthBonus = 0 });
            return;
        }

        foreach (var row in itemsData)
        {
            var item = new ItemData
            {
                id = row["id"].ToString(),
                name = row["name"].ToString(),
                grade = Convert.ToInt32(row["grade"]),
                count = 0,  // 잠금 상태
                rarity = GetRarityValue(row["rarity"].ToString()),
                type = row["type"].ToString(),
                attackBonus = 0,
                defenseBonus = 0,
                healthBonus = 0
            };

            // stats JSON 파싱
            if (row.TryGetValue("stats", out var statsObj) && statsObj != null)
            {
                var parsed = JsonUtility.FromJson<ItemStatsJson>(statsObj.ToString());
                if (parsed != null)
                {
                    item.attackBonus = parsed.attackBonus;
                    item.defenseBonus = parsed.defenseBonus;
                    item.healthBonus = parsed.hpBonus;
                }
            }

            state.inventory.items.Add(item);
        }

        GameLogger.Info($"모든 아이템 슬롯 추가 완료: {itemsData.Count}개");
    }

    /// <summary>
    /// 희귀도 문자열을 정수값으로 변환
    /// </summary>
    private int GetRarityValue(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return 0;
            case "rare": return 1;
            case "epic": return 2;
            case "legendary": return 3;
            case "mythic": return 4;
            default: return 0;
        }
    }
}
