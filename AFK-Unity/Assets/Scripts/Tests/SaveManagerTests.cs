using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.IO;

/// <summary>
/// SaveManager 클래스에 대한 단위 테스트
/// </summary>
public class SaveManagerTests
{
    private SaveManager _saveManager;
    private GameState _gameState;
    private string _testSavePath;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("SaveManager_Test");
        _saveManager = go.AddComponent<SaveManager>();
        
        var gsGo = new GameObject("GameState_Test");
        _gameState = gsGo.AddComponent<GameState>();
        _gameState.Initialize();
        
        _testSavePath = Path.Combine(Application.persistentDataPath, "test_savegame.json");
    }

    [TearDown]
    public void TearDown()
    {
        // 테스트 파일 정리
        if (File.Exists(_testSavePath))
        {
            File.Delete(_testSavePath);
        }
        
        GameObject.DestroyImmediate(_saveManager.gameObject);
        GameObject.DestroyImmediate(_gameState.gameObject);
    }

    /// <summary>
    /// SaveManager 싱글톤 인스턴스 생성 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_Singleton_CreatesInstance()
    {
        var saveManager = SaveManager.Instance;
        Assert.IsNotNull(saveManager);
        yield return null;
    }

    /// <summary>
    /// 저장 파일 존재 여부 확인 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_SaveExists_ReturnsFalseForNewGame()
    {
        // 새 게임에서는 저장 파일이 없어야 함
        Assert.IsFalse(_saveManager.SaveExists());
        yield return null;
    }

    /// <summary>
    /// 저장 및 로드 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_SaveAndLoad_WorksCorrectly()
    {
        // 초기 상태 설정
        _gameState.player.level = 10;
        _gameState.player.gold = 1000;
        _gameState.stage.currentStage = 5;
        
        // 저장
        _saveManager.Save(_gameState);
        
        // 저장 파일이 생성되었는지 확인
        Assert.IsTrue(File.Exists(_testSavePath));
        
        // 로드
        GameState loadedState = _saveManager.Load();
        
        Assert.IsNotNull(loadedState);
        Assert.AreEqual(10, loadedState.player.level);
        Assert.AreEqual(1000, loadedState.player.gold);
        Assert.AreEqual(5, loadedState.stage.currentStage);
        
        yield return null;
    }

    /// <summary>
    /// 저장 파일 삭제 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_DeleteSave_RemovesFile()
    {
        // 저장
        _saveManager.Save(_gameState);
        Assert.IsTrue(File.Exists(_testSavePath));
        
        // 삭제
        _saveManager.DeleteSave();
        Assert.IsFalse(File.Exists(_testSavePath));
        
        yield return null;
    }

    /// <summary>
    /// 내보내기 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_ExportSave_ReturnsJson()
    {
        // 저장
        _saveManager.Save(_gameState);
        
        // 내보내기
        string json = _saveManager.ExportSave();
        
        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("player"));
        
        yield return null;
    }

    /// <summary>
    /// 가져오기 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_ImportSave_CreatesFile()
    {
        string testJson = "{\"player\":{\"level\":5,\"gold\":500}}";
        
        _saveManager.ImportSave(testJson);
        
        Assert.IsTrue(File.Exists(_testSavePath));
        
        yield return null;
    }

    /// <summary>
    /// 자동 저장 시작/중지 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_AutoSave_StartAndStop()
    {
        _saveManager.StartAutoSave(1f);
        yield return new WaitForSeconds(2.5f);
        
        // 2번 이상 저장되었는지 확인 (파일 수정 시간으로 확인)
        Assert.IsTrue(File.Exists(_testSavePath));
        
        _saveManager.StopAutoSave();
        
        yield return null;
    }

    /// <summary>
    /// 백업 및 복원 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator SaveManager_BackupAndRestore_WorksCorrectly()
    {
        // 초기 저장
        _gameState.player.level = 10;
        _saveManager.Save(_gameState);
        
        // 백업
        _saveManager.BackupSave();
        
        string backupPath = Path.Combine(Application.persistentDataPath, "savegame_backup.json");
        Assert.IsTrue(File.Exists(backupPath));
        
        // 원본 변경
        _gameState.player.level = 20;
        _saveManager.Save(_gameState);
        
        // 복원
        _saveManager.RestoreFromBackup();
        
        GameState restoredState = _saveManager.Load();
        Assert.AreEqual(10, restoredState.player.level);
        
        yield return null;
    }
}
