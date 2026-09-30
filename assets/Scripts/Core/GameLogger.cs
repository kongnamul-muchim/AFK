using UnityEngine;
using System.Diagnostics;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 게임 로깅을 관리하는 정적 클래스
/// 디버그 빌드에서만 동작하는 로그와 릴리스 빌드에서도 동작하는 로그를 분리
/// 파일 저장 기능 포함 (플레이 모드 연동)
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

    // ===== 파일 저장 관련 필드 =====

    /// <summary>
    /// 로그 저장 경로
    /// </summary>
    public static string LogDirectory => Application.persistentDataPath + "/Logs";

    /// <summary>
    /// 현재 로그 파일 경로
    /// </summary>
    public static string CurrentLogFilePath { get; private set; }

    /// <summary>
    /// 파일 저장 중 여부
    /// </summary>
    public static bool IsFileLogging { get; private set; }

    /// <summary>
    /// 로그 버퍼 (파일 쓰기 최적화)
    /// </summary>
    private static List<string> _logBuffer = new List<string>();

    /// <summary>
    /// 버퍼 플러시 간격 (초)
    /// </summary>
    private static readonly float BUFFER_FLUSH_INTERVAL = 1.0f;

    /// <summary>
    /// 마지막 버퍼 플러시 시간
    /// </summary>
    private static float _lastFlushTime = 0f;

    /// <summary>
    /// 로그 락 객체 (스레드 세이프)
    /// </summary>
    private static readonly object _lockObj = new object();

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

    // ===== 파일 저장 메서드 =====

    /// <summary>
    /// 파일 저장을 시작합니다. (플레이 모드 시작 시 호출)
    /// </summary>
    public static void StartFileLogging()
    {
        if (IsFileLogging)
        {
            UnityEngine.Debug.LogWarning("[GameLogger] 파일 저장이 이미 실행 중입니다.");
            return;
        }

        // 로그 디렉토리 생성
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }

        // 파일명: Log_yyyyMMdd_HHmmss.txt
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        CurrentLogFilePath = Path.Combine(LogDirectory, $"Log_{timestamp}.txt");

        // 파일 헤더 작성
        StringBuilder header = new StringBuilder();
        header.AppendLine("========================================");
        header.AppendLine($"플레이 세션 로그");
        header.AppendLine($"시작 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        header.AppendLine($"Unity 버전: {Application.unityVersion}");
        header.AppendLine($"플랫폼: {Application.platform}");
        header.AppendLine("========================================");
        header.AppendLine();

        try
        {
            File.WriteAllText(CurrentLogFilePath, header.ToString());
            IsFileLogging = true;

            // Application.logMessageReceived에 리스너 등록
            Application.logMessageReceived += OnLogMessageReceived;

            UnityEngine.Debug.Log($"[GameLogger] 로그 파일 저장 시작: {CurrentLogFilePath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameLogger] 로그 파일 생성 실패: {ex.Message}");
            CurrentLogFilePath = null;
        }
    }

    /// <summary>
    /// 파일 저장을 중지합니다. (플레이 모드 종료 시 호출)
    /// </summary>
    public static void StopFileLogging()
    {
        if (!IsFileLogging)
        {
            UnityEngine.Debug.LogWarning("[GameLogger] 실행 중인 파일 저장이 없습니다.");
            return;
        }

        // 리스너 해제
        Application.logMessageReceived -= OnLogMessageReceived;

        // 남은 버퍼 플러시
        FlushBuffer();

        // 파일 끝에 종료 마커 작성
        try
        {
            string footer = $"\n========================================\n" +
                           $"세션 종료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                           $"========================================\n";

            File.AppendAllText(CurrentLogFilePath, footer);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameLogger] 종료 마커 작성 실패: {ex.Message}");
        }

        IsFileLogging = false;
        UnityEngine.Debug.Log($"[GameLogger] 로그 파일 저장 종료: {CurrentLogFilePath}");
    }

    /// <summary>
    /// Unity 로그 메시지 수신 (Application.logMessageReceived 콜백)
    /// </summary>
    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!IsFileLogging) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logLevel = GetLogLevelString(type);
        string logEntry = $"[{timestamp}] [{logLevel}] {condition}";

        // Error/Warning의 경우 스택 트레이스 추가
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            logEntry += "\n" + stackTrace;
        }

        lock (_lockObj)
        {
            _logBuffer.Add(logEntry);
        }
    }

    /// <summary>
    /// 로그 레벨 문자열 변환
    /// </summary>
    private static string GetLogLevelString(LogType type)
    {
        switch (type)
        {
            case LogType.Log: return "INFO";
            case LogType.Warning: return "WARN";
            case LogType.Error: return "ERROR";
            case LogType.Exception: return "EXCEPT";
            case LogType.Assert: return "ASSERT";
            default: return "UNKNOWN";
        }
    }

    /// <summary>
    /// 버퍼를 파일에 플러시
    /// </summary>
    public static void FlushBuffer()
    {
        if (string.IsNullOrEmpty(CurrentLogFilePath)) return;

        List<string> bufferCopy;
        lock (_lockObj)
        {
            if (_logBuffer.Count == 0) return;
            bufferCopy = new List<string>(_logBuffer);
            _logBuffer.Clear();
        }

        try
        {
            File.AppendAllLines(CurrentLogFilePath, bufferCopy);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[GameLogger] 버퍼 플러시 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 주기적 버퍼 플러시 (업데이트에서 호출)
    /// </summary>
    public static void Update()
    {
        if (!IsFileLogging) return;

        if (Time.time - _lastFlushTime >= BUFFER_FLUSH_INTERVAL)
        {
            FlushBuffer();
            _lastFlushTime = Time.time;
        }
    }

    /// <summary>
    /// 세션 폴더의 모든 로그 파일 반환
    /// </summary>
    public static string[] GetAllLogFiles()
    {
        if (!Directory.Exists(LogDirectory))
        {
            return new string[0];
        }

        return Directory.GetFiles(LogDirectory, "Log_*.txt", SearchOption.TopDirectoryOnly);
    }

    /// <summary>
    /// 최신 로그 파일 경로 반환
    /// </summary>
    public static string GetLatestLogFile()
    {
        var files = GetAllLogFiles();
        if (files.Length == 0) return null;

        Array.Sort(files);
        return files[files.Length - 1];
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 플레이 모드 상태 변경 시 자동 호출
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        UnityEditor.EditorApplication.playmodeStateChanged += OnPlayModeChanged;
    }

    /// <summary>
    /// 플레이 모드 변경 이벤트 핸들러
    /// </summary>
    private static void OnPlayModeChanged()
    {
        // isPlaying이 false에서 true로 변경될 때 (플레이 시작)
        if (UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            StartFileLogging();
        }
        // isPlaying이 true에서 false로 변경될 때 (플레이 종료)
        else if (!UnityEditor.EditorApplication.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            StopFileLogging();
        }
    }
#endif

    // ===== 기존 로그 메서드 =====

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
