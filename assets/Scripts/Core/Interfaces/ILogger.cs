/// <summary>
/// 로깅 시스템 인터페이스
/// DIP 준수: 구체적 구현(GameLogger) 대신 인터페이스에 의존
/// UnityEngine.ILogger와 충돌 방지를 위해 IGameLogger로 명명
/// </summary>
public interface IGameLogger
{
    /// <summary>
    /// 정보 로그
    /// </summary>
    void Info(string message);
    
    /// <summary>
    /// 경고 로그
    /// </summary>
    void Warn(string message);
    
    /// <summary>
    /// 에러 로그
    /// </summary>
    void Error(string message);
    
    /// <summary>
    /// 디버그 로그
    /// </summary>
    void Debug(string message);
}
