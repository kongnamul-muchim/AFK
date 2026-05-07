using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 플레이 세션 로그 관리자
/// 플레이 모드 시작/종료에 연동되어 로그를 파일로 저장
/// </summary>
public static class PlaySessionLogger
{
    /// <summary>
    /// 로그 저장 경로
    /// </summary>
    public static string LogDirectory => Application.persistentDataPath + "/Logs";

    /// <summary>
    /// 현재 로그 파일 경로
    /// </summary>
    public static string CurrentLogFilePath { get; private set; }

    /// <summary>
    /// 로그 저장 중 여부
    /// </summary>
    public static bool IsLogging { get; private set; }

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
    /// 플레이 세션 시작
    /// 로그 파일 초기화 후 저장 시작
    /// </summary>
    public static void StartSession()
    {
        if (IsLogging)
        {
            Debug.LogWarning("[PlaySessionLogger] 세션이 이미 실행 중입니다.");
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

        // 초기 파일 작성
        try
        {
            File.WriteAllText(CurrentLogFilePath, header.ToString());
            IsLogging = true;

            // Application.logMessageReceived에 리스너 등록
            Application.logMessageReceived += OnLogMessageReceived;

            Debug.Log($"[PlaySessionLogger] 로그 저장 시작: {CurrentLogFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlaySessionLogger] 로그 파일 생성 실패: {ex.Message}");
            CurrentLogFilePath = null;
        }
    }

    /// <summary>
    /// 플레이 세션 종료
    /// 로그 저장 중지
    /// </summary>
    public static void StopSession()
    {
        if (!IsLogging)
        {
            Debug.LogWarning("[PlaySessionLogger] 실행 중인 세션이 없습니다.");
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
            Debug.LogError($"[PlaySessionLogger] 종료 마커 작성 실패: {ex.Message}");
        }

        IsLogging = false;
        Debug.Log($"[PlaySessionLogger] 로그 저장 종료: {CurrentLogFilePath}");
    }

    /// <summary>
    /// Unity 로그 메시지 수신
    /// </summary>
    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!IsLogging) return;

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
            Debug.LogError($"[PlaySessionLogger] 버퍼 플러시 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 주기적 버퍼 플러시 (업데이트에서 호출)
    /// </summary>
    public static void Update()
    {
        if (!IsLogging) return;

        if (Time.time - _lastFlushTime >= BUFFER_FLUSH_INTERVAL)
        {
            FlushBuffer();
            _lastFlushTime = Time.time;
        }
    }

    /// <summary>
    /// 수동으로 로그 추가
    /// </summary>
    public static void Log(string message, LogType type = LogType.Log)
    {
        if (!IsLogging) return;
        OnLogMessageReceived(message, string.Empty, type);
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
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.playmodeStateChanged += OnPlayModeChanged;
    }

    /// <summary>
    /// 플레이 모드 변경 이벤트 핸들러
    /// </summary>
    private static void OnPlayModeChanged()
    {
        if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // 플레이 시작
            StartSession();
        }
        else if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // 플레이 종료
            StopSession();
        }
    }
#endif
}
