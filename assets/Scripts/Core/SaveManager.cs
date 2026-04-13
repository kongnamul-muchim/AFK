using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// 게임 저장/로드를 관리하는 싱글톤 클래스
/// JSON 직렬화를 사용하여 GameState를 파일에 저장하고 로드합니다.
/// DIP 준수: ISaveManager 인터페이스 구현
/// </summary>
public class SaveManager : MonoBehaviour, ISaveManager
{
    private static SaveManager _instance;
    
    /// <summary>
    /// SaveManager의 싱글톤 인스턴스
    /// </summary>
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

    /// <summary>저장 파일명</summary>
    private const string SAVE_FILE_NAME = "savegame.json";
    
    /// <summary>현재 세이브 버전 (마이그레이션용)</summary>
    private const int CURRENT_SAVE_VERSION = 3;
    
    /// <summary>자동 저장 코루틴</summary>
    private Coroutine _autoSaveCoroutine;
    
    /// <summary>자동 저장 간격 (초)</summary>
    private float _autoSaveInterval = 5f;

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
        // 자동 저장 중지
        StopAutoSave();
    }

    // ========== 저장/로드 메서드 ==========

    /// <summary>
    /// 게임 상태를 JSON 파일로 저장 (동기)
    /// </summary>
    /// <param name="state">저장할 GameState</param>
    public void Save(GameState state)
    {
        if (state == null)
        {
            GameLogger.Error("저장할 GameState가 null입니다.");
            return;
        }

        try
        {
            // SaveData로 변환하여 직렬화
            SaveData saveData = SaveData.CreateFromGameState(state);
            string json = JsonUtility.ToJson(saveData, true);
            string savePath = GetSavePath();
            
            // 디렉토리 확인
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(savePath, json);
            GameLogger.Info($"게임 저장 완료: {savePath}");
            
            // 저장 이벤트 발생
            EventBus.Instance.Emit(GameEvents.GAME_SAVED);
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"저장 실패: {e.Message}");
            GameLogger.Exception(e);
        }
    }

    /// <summary>
    /// 게임 상태를 JSON 파일로 저장 (비동기)
    /// </summary>
    /// <param name="state">저장할 GameState</param>
    public async Task SaveAsync(GameState state)
    {
        if (state == null)
        {
            GameLogger.Error("저장할 GameState가 null입니다.");
            return;
        }

        try
        {
            // SaveData로 변환하여 직렬화
            SaveData saveData = SaveData.CreateFromGameState(state);
            string json = JsonUtility.ToJson(saveData, true);
            string savePath = GetSavePath();
            
            // 디렉토리 확인
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(savePath, json);
            GameLogger.Info($"게임 저장 완료 (비동기): {savePath}");
            
            // 저장 이벤트 발생
            EventBus.Instance.Emit(GameEvents.GAME_SAVED);
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"저장 실패: {e.Message}");
            GameLogger.Exception(e);
        }
    }

    /// <summary>
    /// JSON 파일에서 GameState 로드 (동기)
    /// </summary>
    /// <returns>로드된 GameState, 없으면 null</returns>
    public GameState Load()
    {
        try
        {
            string savePath = GetSavePath();
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    GameLogger.Warn("저장 파일이 비어있습니다.");
                    return null;
                }
                
                // SaveData로 역직렬화 후 GameState에 적용
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null)
                {
                    GameLogger.Warn("저장 파일이 잘못되었습니다.");
                    return null;
                }
                
                // 기존 GameState 인스턴스에 데이터 적용
                var state = GameState.Instance;
                saveData.ApplyToGameState(state);
                
                GameLogger.Info("게임 로드 완료");
                
                // 로드 이벤트 발생
                EventBus.Instance.Emit(GameEvents.GAME_LOADED);
                
                return state;
            }
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"로드 실패: {e.Message}");
            GameLogger.Exception(e);
        }
        
        return null;
    }

    /// <summary>
    /// JSON 파일에서 GameState 로드 (비동기)
    /// </summary>
    /// <returns>로드된 GameState, 없으면 null</returns>
    public async Task<GameState> LoadAsync()
    {
        try
        {
            string savePath = GetSavePath();
            if (File.Exists(savePath))
            {
                string json = await File.ReadAllTextAsync(savePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    GameLogger.Warn("저장 파일이 비어있습니다.");
                    return null;
                }
                
                // SaveData로 역직렬화 후 GameState에 적용
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null)
                {
                    GameLogger.Warn("저장 파일이 잘못되었습니다.");
                    return null;
                }
                
                // 기존 GameState 인스턴스에 데이터 적용
                var state = GameState.Instance;
                saveData.ApplyToGameState(state);
                
                GameLogger.Info("게임 로드 완료 (비동기)");
                
                // 로드 이벤트 발생
                EventBus.Instance.Emit(GameEvents.GAME_LOADED);
                
                return state;
            }
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"로드 실패: {e.Message}");
            GameLogger.Exception(e);
        }
        
        return null;
    }

    /// <summary>
    /// 저장 파일 존재 여부 확인
    /// </summary>
    /// <returns>저장 파일이 있으면 true</returns>
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
            GameLogger.Info("저장 파일 삭제 완료");
        }
    }

    // ========== 자동 저장 ==========

    /// <summary>
    /// 자동 저장 시작
    /// </summary>
    /// <param name="interval">저장 간격 (초, 기본 5초)</param>
    public void StartAutoSave(float interval = 5f)
    {
        _autoSaveInterval = interval;
        
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }
        
        _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
        GameLogger.Info($"자동 저장 시작 (간격: {interval}초)");
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
            GameLogger.Info("자동 저장 중지");
        }
    }

    private IEnumerator AutoSaveCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_autoSaveInterval);
        
        while (true)
        {
            yield return wait;
            
            if (GameState.Instance != null)
            {
                Save(GameState.Instance);
            }
        }
    }

    // ========== 내보내기/가져오기 ==========

    /// <summary>
    /// 저장 파일을 텍스트로 내보내기 (웹 세이브 이관용)
    /// </summary>
    /// <returns>JSON 문자열</returns>
    public string ExportSave()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            return File.ReadAllText(savePath);
        }
        return null;
    }

    /// <summary>
    /// 텍스트에서 저장 파일로 가져오기 (웹 세이브 이관용)
    /// </summary>
    /// <param name="json">JSON 문자열</param>
    public void ImportSave(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            GameLogger.Error("가져올 데이터가 없습니다.");
            return;
        }

        try
        {
            string savePath = GetSavePath();
            File.WriteAllText(savePath, json);
            GameLogger.Info("세이브 가져오기 완료");
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"세이브 가져오기 실패: {e.Message}");
            GameLogger.Exception(e);
        }
    }

    /// <summary>
    /// 웹 버전 세이브 가져오기 (마이그레이션 포함)
    /// </summary>
    /// <param name="json">웹 버전 JSON 문자열</param>
    public void ImportWebSave(string json)
    {
        try
        {
            // 웹 버전 JSON을 Unity 형식으로 변환
            // 필요한 경우 마이그레이션 로직 추가
            GameState state = JsonUtility.FromJson<GameState>(json);
            
            if (state != null)
            {
                Save(state);
                GameLogger.Info("웹 세이브 가져오기 완료");
            }
            else
            {
                GameLogger.Error("웹 세이브 파싱 실패");
            }
        }
        catch (System.Exception e)
        {
            GameLogger.Error($"웹 세이브 가져오기 실패: {e.Message}");
            GameLogger.Exception(e);
        }
    }

    // ========== 유틸리티 메서드 ==========

    /// <summary>
    /// 저장 경로 가져오기
    /// </summary>
    /// <returns>저장 파일의 전체 경로</returns>
    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    /// <summary>
    /// 저장 파일 크기 가져오기 (바이트)
    /// </summary>
    /// <returns>파일 크기</returns>
    public long GetSaveFileSize()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            FileInfo info = new FileInfo(savePath);
            return info.Length;
        }
        return 0;
    }

    /// <summary>
    /// 백업 저장 (이전 세이브 보관)
    /// </summary>
    public void BackupSave()
    {
        string savePath = GetSavePath();
        string backupPath = Path.Combine(Application.persistentDataPath, "savegame_backup.json");
        
        if (File.Exists(savePath))
        {
            File.Copy(savePath, backupPath, true);
            GameLogger.Info("백업 저장 완료");
        }
    }

    /// <summary>
    /// 백업에서 복원
    /// </summary>
    public void RestoreFromBackup()
    {
        string backupPath = Path.Combine(Application.persistentDataPath, "savegame_backup.json");
        string savePath = GetSavePath();
        
        if (File.Exists(backupPath))
        {
            File.Copy(backupPath, savePath, true);
            GameLogger.Info("백업에서 복원 완료");
        }
    }

    // ========== ISaveManager 인터페이스 구현 ==========
    
    void ISaveManager.CreateBackup()
    {
        BackupSave();
    }
}
