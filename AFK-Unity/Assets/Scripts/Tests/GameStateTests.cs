using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;

/// <summary>
/// GameState 클래스에 대한 단위 테스트
/// </summary>
public class GameStateTests
{
    private GameState _gameState;

    [SetUp]
    public void Setup()
    {
        // GameState 인스턴스 생성
        var go = new GameObject("GameState_Test");
        _gameState = go.AddComponent<GameState>();
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.DestroyImmediate(_gameState.gameObject);
    }

    /// <summary>
    /// GameState 싱글톤 인스턴스가 정상적으로 생성되는지 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_Singleton_CreatesInstance()
    {
        var gameState = GameState.Instance;
        Assert.IsNotNull(gameState);
        yield return null;
    }

    /// <summary>
    /// GameState 초기화 시 기본값이 설정되는지 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_Initialize_SetsDefaultValues()
    {
        _gameState.Initialize();
        
        Assert.AreEqual(1, _gameState.player.level);
        Assert.AreEqual(0, _gameState.player.gold);
        Assert.AreEqual(0, _gameState.player.gems);
        Assert.AreEqual(1, _gameState.stage.currentStage);
        Assert.IsNotNull(_gameState.inventory);
        Assert.IsNotNull(_gameState.inventory.items);
        Assert.IsNotNull(_gameState.inventory.equipment);
        
        yield return null;
    }

    /// <summary>
    /// GameState JSON 직렬화/역직렬화 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_Serialization_RoundTrip()
    {
        _gameState.Initialize();
        _gameState.player.level = 10;
        _gameState.player.gold = 1000;
        _gameState.stage.currentStage = 5;
        
        // 직렬화
        string json = JsonUtility.ToJson(_gameState);
        Assert.IsFalse(string.IsNullOrEmpty(json));
        
        // 역직렬화
        GameState loadedState = JsonUtility.FromJson<GameState>(json);
        Assert.IsNotNull(loadedState);
        Assert.AreEqual(10, loadedState.player.level);
        Assert.AreEqual(1000, loadedState.player.gold);
        Assert.AreEqual(5, loadedState.stage.currentStage);
        
        yield return null;
    }

    /// <summary>
    /// 인벤토리 아이템 추가/삭제 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_Inventory_AddRemoveItems()
    {
        _gameState.Initialize();
        
        ItemData item = new ItemData { id = "test_item", name = "테스트 아이템", grade = 0, quantity = 1 };
        _gameState.inventory.items.Add(item);
        
        Assert.AreEqual(1, _gameState.inventory.items.Count);
        Assert.AreEqual("test_item", _gameState.inventory.items[0].id);
        
        _gameState.inventory.items.RemoveAt(0);
        Assert.AreEqual(0, _gameState.inventory.items.Count);
        
        yield return null;
    }

    /// <summary>
    /// 환생 초기화 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_Rebirth_ResetsCorrectly()
    {
        _gameState.Initialize();
        _gameState.player.level = 50;
        _gameState.player.gold = 10000;
        _gameState.rebirth.rebirthCount = 0;
        
        _gameState.ResetForRebirth();
        
        Assert.AreEqual(1, _gameState.player.level);
        Assert.AreEqual(0, _gameState.player.gold);
        Assert.AreEqual(1, _gameState.rebirth.rebirthCount);
        
        yield return null;
    }

    /// <summary>
    /// 총 공격력 계산 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_CalculateTotalAttack_WithEquipment()
    {
        _gameState.Initialize();
        _gameState.player.attack = 100;
        
        // 장비 추가
        EquipmentData weapon = new EquipmentData 
        { 
            id = "sword", 
            name = "검", 
            slot = 0, 
            attackBonus = 50 
        };
        _gameState.inventory.equipment.Add(weapon);
        
        float totalAttack = _gameState.GetTotalAttack();
        Assert.AreEqual(150, totalAttack);
        
        yield return null;
    }

    /// <summary>
    /// 레벨업 필요 경험치 계산 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator GameState_GetExpToNextLevel_CalculatesCorrectly()
    {
        _gameState.Initialize();
        _gameState.player.level = 1;
        
        long exp1 = _gameState.GetExpToNextLevel();
        Assert.AreEqual(GameConfig.ExpToLevelUp, exp1);
        
        _gameState.player.level = 2;
        long exp2 = _gameState.GetExpToNextLevel();
        Assert.AreEqual((long)(GameConfig.ExpToLevelUp * GameConfig.ExpMultiplier), exp2);
        
        yield return null;
    }
}
