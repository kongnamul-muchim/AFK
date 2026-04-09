using UnityEngine;
using System.Diagnostics;

/// <summary>
/// 게임 로깅을 관리하는 정적 클래스
/// 디버그 빌드에서만 동작하는 로그와 릴리스 빌드에서도 동작하는 로그를 분리
/// </summary>
public static class GameLogger
{
    /// <summary>
    /// 로그 레벨 열거형
    /// </summary>
    public enum LogLevel
    {
        /// <summary>모든 로그 출력 (개발용)</summary>
        DEBUG,
        
        /// <summary>일반 정보 로그</summary>
        INFO,
        
        /// <summary>경고 로그</summary>
        WARN,
        
        /// <summary>에러 로그</summary>
        ERROR,
        
        /// <summary>로그 없음</summary>
        NONE
    }

    private static LogLevel _currentLevel = LogLevel.INFO;

    /// <summary>
    /// 현재 로그 레벨 설정
    /// </summary>
    /// <param name="level">설정할 로그 레벨</param>
    public static void SetLogLevel(LogLevel level)
    {
        _currentLevel = level;
        UnityEngine.Debug.Log($"로그 레벨 변경: {level}");
    }

    /// <summary>
    /// 현재 로그 레벨 가져오기
    /// </summary>
    public static LogLevel GetLogLevel()
    {
        return _currentLevel;
    }

    /// <summary>
    /// 일반 로그 (DEBUG 빌드에서만 동작)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        if (_currentLevel <= LogLevel.INFO)
        {
            UnityEngine.Debug.Log($"[GAME] {message}");
        }
    }

    /// <summary>
    /// 정보 로그 (항상 동작)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    public static void Info(string message)
    {
        if (_currentLevel <= LogLevel.INFO)
        {
            UnityEngine.Debug.Log($"[INFO] {message}");
        }
    }

    /// <summary>
    /// 경고 로그 (항상 동작)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    public static void Warn(string message)
    {
        if (_currentLevel <= LogLevel.WARN)
        {
            UnityEngine.Debug.LogWarning($"[WARN] {message}");
        }
    }

    /// <summary>
    /// 에러 로그 (항상 동작)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    public static void Error(string message)
    {
        if (_currentLevel <= LogLevel.ERROR)
        {
            UnityEngine.Debug.LogError($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// 디버그 로그 (DEBUG 빌드에서만 동작)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    [Conditional("DEBUG")]
    public static void DebugLog(string message)
    {
        if (_currentLevel <= LogLevel.DEBUG)
        {
            UnityEngine.Debug.Log($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// 예외 로그
    /// </summary>
    /// <param name="exception">로그할 예외</param>
    public static void Exception(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    /// <summary>
    /// 조건부 로그 (DEBUG 빌드에서만 동작)
    /// </summary>
    /// <param name="condition">로그를 출력할 조건</param>
    /// <param name="message">로그 메시지</param>
    [Conditional("DEBUG")]
    public static void LogIf(bool condition, string message)
    {
        if (condition && _currentLevel <= LogLevel.INFO)
        {
            UnityEngine.Debug.Log($"[GAME] {message}");
        }
    }
}
