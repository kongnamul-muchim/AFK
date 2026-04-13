/// <summary>
/// 저장 관리자 인터페이스
/// </summary>
public interface ISaveManager
{
    void Save(GameState state);
    GameState Load();
    bool SaveExists();
    void DeleteSave();
    void CreateBackup();
    void RestoreFromBackup();
    void StartAutoSave(float interval = 5f);
    void StopAutoSave();
}
