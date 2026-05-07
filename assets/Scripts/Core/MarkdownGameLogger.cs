using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 마크다운 형식으로 로그를 저장하는 모듈
/// 플레이 시작 시 초기화, 종료 시 .md 파일로 저장
/// Unity Console에는 아무것도 출력하지 않음
/// </summary>
public static class MarkdownGameLogger
{
    /// <summary>
    /// 로그 파일 저장 경로 (프로젝트 내 docs/logs/)
    /// </summary>
    public static string LogDirectory => Path.Combine(Application.dataPath, "..", "docs", "logs");

    /// <summary>
    /// 현재 로그 파일 경로
    /// </summary>
    public static string CurrentLogFilePath { get; private set; }

    /// <summary>
    /// 로그 저장 중 여부
    /// </summary>
    public static bool IsLogging { get; private set; }

    /// <summary>
    /// 로그 버퍼
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
    /// 로그 락 객체
    /// </summary>
    private static readonly object _lockObj = new object();

    /// <summary>
    /// 로그 파일명 (임시)
    /// </summary>
    private const string TEMP_LOG_FILENAME = "play-session-temp.md";

    /// <summary>
    /// 로그 저장 시작 (플레이 시작 시 호출)
    /// </summary>
    public static void StartLogging()
    {
        // 로그 디렉토리 생성
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }

        // 임시 파일 경로
        CurrentLogFilePath = Path.Combine(LogDirectory, TEMP_LOG_FILENAME);

        // 마크다운 헤더 작성
        StringBuilder header = new StringBuilder();
        header.AppendLine("# 🎮 플레이 세션 로그");
        header.AppendLine();
        header.AppendLine("## 세션 정보");
        header.AppendLine();
        header.AppendLine($"| 항목 | 내용 |");
        header.AppendLine($"|------|------|");
        header.AppendLine($"| 시작 시간 | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        header.AppendLine($"| Unity 버전 | {Application.unityVersion} |");
        header.AppendLine($"| 플랫폼 | {Application.platform} |");
        header.AppendLine($"| 플레이어 | {Application.productName} |");
        header.AppendLine();
        header.AppendLine("## 로그 기록");
        header.AppendLine();
        header.AppendLine("```");
        header.AppendLine();

        try
        {
            File.WriteAllText(CurrentLogFilePath, header.ToString());
            IsLogging = true;

            // Application.logMessageReceived에 리스너 등록
            Application.logMessageReceived += OnLogMessageReceived;
        }
        catch (Exception)
        {
            CurrentLogFilePath = null;
        }
    }

    /// <summary>
    /// 로그 저장 종료 (플레이 종료 시 호출)
    /// .md 파일로 최종 저장
    /// </summary>
    public static void StopLogging()
    {
        if (!IsLogging) return;

        // 리스너 해제
        Application.logMessageReceived -= OnLogMessageReceived;

        // 남은 버퍼 플러시
        FlushBuffer();

        // 마크다운 푸터 작성
        string footer = "```\n\n";
        footer += "## 세션 종료\n\n";
        footer += $"- **종료 시간**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        footer += "- **로그 종료**\n";

        try
        {
            File.AppendAllText(CurrentLogFilePath, footer);

            // .md 파일로 복사 (타임스탬프 포함)
            string finalPath = Path.Combine(LogDirectory, $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.md");
            File.Copy(CurrentLogFilePath, finalPath, overwrite: true);
        }
        catch (Exception)
        {
            // 아무것도 안 함
        }

        IsLogging = false;
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

        // 예외/에러의 경우 스택 트레이스 추가
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                logEntry += $"\n  ```\n  {stackTrace}\n  ```";
            }
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
            case LogType.Exception: return "EXCEPTION";
            case LogType.Assert: return "ASSERT";
            default: return "UNKNOWN";
        }
    }

    /// <summary>
    /// 버퍼를 파일에 플러시
    /// </summary>
    private static void FlushBuffer()
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
        catch (Exception)
        {
            // 아무것도 안 함
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
    /// 인벤토리 디버그용 로그
    /// </summary>
    public static void LogInventory(string message)
    {
        if (!IsLogging) return;
        OnLogMessageReceived($"[INVENTORY] {message}", string.Empty, LogType.Log);
    }

    /// <summary>
    /// 인벤토리 에러 로그
    /// </summary>
    public static void LogInventoryError(string message)
    {
        if (!IsLogging) return;
        OnLogMessageReceived($"[INVENTORY ERROR] {message}", string.Empty, LogType.Error);
    }

    /// <summary>
    /// 인벤토리 경고 로그
    /// </summary>
    public static void LogInventoryWarning(string message)
    {
        if (!IsLogging) return;
        OnLogMessageReceived($"[INVENTORY WARN] {message}", string.Empty, LogType.Warning);
    }

    /// <summary>
    /// 현재 로그 파일 내용을 문자열로 반환
    /// </summary>
    public static string GetCurrentLogContent()
    {
        if (string.IsNullOrEmpty(CurrentLogFilePath) || !File.Exists(CurrentLogFilePath))
        {
            return string.Empty;
        }

        try
        {
            FlushBuffer();
            return File.ReadAllText(CurrentLogFilePath);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 저장된 모든 로그 파일 목록 반환
    /// </summary>
    public static string[] GetAllLogFiles()
    {
        if (!Directory.Exists(LogDirectory))
        {
            return new string[0];
        }

        return Directory.GetFiles(LogDirectory, "Log_*.md", SearchOption.TopDirectoryOnly);
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
        // 플레이 시작 (Edit → Play)
        // isPlaying=false, isPlayingOrWillChangePlaymode=true → 플레이 시작 직전
        // isPlaying=true, isPlayingOrWillChangePlaymode=true → 플레이 중
        if (EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            StartLogging();
        }
        // 플레이 종료 (Play → Edit)
        // isPlaying=true, isPlayingOrWillChangePlaymode=false → 플레이 중 → 종료 직전
        // isPlaying=false, isPlayingOrWillChangePlaymode=false → 편집 중
        else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            StopLogging();
        }
    }
#endif
}
