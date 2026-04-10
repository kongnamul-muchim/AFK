/// <summary>
/// GameLogger를 ILogger 인터페이스로 적응시키는 어댑터
/// </summary>
public class GameLoggerAdapter : ILogger
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
