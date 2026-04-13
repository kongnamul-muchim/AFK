/// <summary>
/// GameLogger를 IGameLogger 인터페이스로 적응시키는 어댑터
/// UnityEngine.ILogger와 충돌 방지를 위해 IGameLogger 사용
/// </summary>
public class GameLoggerAdapter : IGameLogger
{
    public void Info(string message)
    {
        GameLogger.Info(message);
    }

    public void Warn(string message)
    {
        GameLogger.Warn(message);
    }

    public void Error(string message)
    {
        GameLogger.Error(message);
    }

    public void Debug(string message)
    {
        GameLogger.DebugLog(message);
    }
}
