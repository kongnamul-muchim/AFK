using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 게임 초기화를 담당하는 클래스
/// 게임 시작 시 모든 싱글톤 인스턴스를 초기화하고 저장된 게임을 로드하거나 새 게임을 시작합니다.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    /// <summary>
    /// 부트스트랩이 이미 실행되었는지 확인하는 플래그
    /// </summary>
    private static bool _isInitialized = false;

    private void Awake()
    {
        // 중복 초기화 방지
        if (_isInitialized)
        {
            Destroy(gameObject);
            return;
        }
        _isInitialized = true;

        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    /// <summary>
    /// 게임 초기화 수행
    /// </summary>
    private void InitializeGame()
    {
        GameLogger.Info("게임 부트스트랩 시작...");

        // 0. ServiceLocator 초기화 및 서비스 등록 (DIP 준수)
        var serviceLocator = ServiceLocator.Instance;
        
        // GameState를 IGameState로 등록
        var gameState = GameState.Instance;
        serviceLocator.RegisterSingleton<IGameState, GameState>(gameState);
        
        // EventBus를 IEventBus로 등록
        var eventBus = EventBus.Instance;
        serviceLocator.RegisterSingleton<IEventBus, EventBus>(eventBus);
        
        // SaveManager를 ISaveManager로 등록
        var saveManager = SaveManager.Instance;
        serviceLocator.RegisterSingleton<ISaveManager, SaveManager>(saveManager);
        
        // Logger 등록
        serviceLocator.RegisterSingleton<IGameLogger, GameLoggerAdapter>(new GameLoggerAdapter());
        
        GameLogger.DebugLog("ServiceLocator에 서비스 등록 완료");

        // 1. 싱글톤 인스턴스들 초기화 순서 보장
        GameLogger.DebugLog("싱글톤 인스턴스 초기화 완료");

        // 2. 저장된 게임 로드 또는 새 게임 시작
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
            }
        }
        else
        {
            // 저장 파일 없음 - 새 게임 시작
            gameState.Initialize();
            GiveStarterItems(gameState);
            GameLogger.Info("새 게임 시작");
        }

        // 3. 자동 저장 시작
        saveManager.StartAutoSave(5f);

        // 4. 초기 이벤트 발생
        eventBus.Emit(GameEvents.GAME_LOADED);

        // 5. 첫 스테이지 진입 (전투 시작)
        StageSystem.Instance.EnterStage(1);

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

            // stats JSON 파싱 (간단히)
            if (row.TryGetValue("stats", out var statsObj) && statsObj != null)
            {
                var statsStr = statsObj.ToString();
                if (statsStr.Contains("attackBonus"))
                {
                    var start = statsStr.IndexOf(":") + 1;
                    var end = statsStr.IndexOf("}", start);
                    if (int.TryParse(statsStr.Substring(start, end - start).Trim(), out var atk))
                        item.attackBonus = atk;
                }
                if (statsStr.Contains("defenseBonus"))
                {
                    var start = statsStr.IndexOf(":") + 1;
                    var end = statsStr.IndexOf("}", start);
                    if (int.TryParse(statsStr.Substring(start, end - start).Trim(), out var def))
                        item.defenseBonus = def;
                }
                if (statsStr.Contains("healthBonus"))
                {
                    var start = statsStr.IndexOf(":") + 1;
                    var end = statsStr.IndexOf("}", start);
                    if (int.TryParse(statsStr.Substring(start, end - start).Trim(), out var hp))
                        item.healthBonus = hp;
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
