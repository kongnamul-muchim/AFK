using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// AGENTS.md 파일 분석기
/// 테스트 실행 전 AGENTS.md를 읽고 규칙을 분석
/// </summary>
public static class AgentsMdAnalyzer
{
    /// <summary>
    /// AGENTS.md 파일 경로
    /// </summary>
    public static string AgentsMdPath => Path.Combine(Application.dataPath, "..", "AGENTS.md");

    /// <summary>
    /// 분석 결과
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>전체 콘텐츠</summary>
        public string Content;

        /// <summary>문서화 규칙</summary>
        public List<string> DocumentationRules = new List<string>();

        /// <summary>코딩 원칙 (SOLID 등)</summary>
        public List<string> CodingRules = new List<string>();

        /// <summary>테스트 체크리스트</summary>
        public List<string> TestChecklist = new List<string>();

        /// <summary>Git 커밋 규칙</summary>
        public Dictionary<string, string> GitRules = new Dictionary<string, string>();

        /// <summary>에러 메시지 (분석 실패 시)</summary>
        public string Error;
    }

    /// <summary>
    /// AGENTS.md 파일을 읽고 분석
    /// </summary>
    public static AnalysisResult Analyze()
    {
        var result = new AnalysisResult();

        try
        {
            // AGENTS.md 파일 존재 확인
            string path = AgentsMdPath;
            if (!File.Exists(path))
            {
                result.Error = $"AGENTS.md 파일을 찾을 수 없습니다: {path}";
                Debug.LogError($"[AgentsMdAnalyzer] {result.Error}");
                return result;
            }

            // 파일 읽기
            result.Content = File.ReadAllText(path);
            Debug.Log($"[AgentsMdAnalyzer] AGENTS.md 로드 완료: {path}");

            // 각 섹션 파싱
            ParseDocumentationRules(result);
            ParseCodingRules(result);
            ParseTestChecklist(result);
            ParseGitRules(result);

            Debug.Log($"[AgentsMdAnalyzer] 분석 완료 - 문서화 규칙: {result.DocumentationRules.Count}, " +
                      $"코딩 규칙: {result.CodingRules.Count}, 테스트 체크리스트: {result.TestChecklist.Count}");
        }
        catch (Exception ex)
        {
            result.Error = $"AGENTS.md 분석 중 오류 발생: {ex.Message}";
            Debug.LogError($"[AgentsMdAnalyzer] {result.Error}");
        }

        return result;
    }

    /// <summary>
    /// 문서화 규칙 파싱 (3장)
    /// </summary>
    private static void ParseDocumentationRules(AnalysisResult result)
    {
        // 3장 문서화 규칙 관련 패턴 매칭
        string[] patterns = new[]
        {
            @"### (\d+\.\d+)\s+(.+?)(?=\n###|\n##|\Z)",
            @"## (\d+)\s+(.+?)(?=\n##|\n#|\Z)",
            @"-\s*\*\*(.+?)\*\*",
        };

        // 간단한 키워드 기반 추출
        var keywords = new[] { "문서", "문서화", "planning", "progress", "reports", "template" };
        var lines = result.Content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#")) continue; // 헤더는 건너뜀

            foreach (var keyword in keywords)
            {
                if (trimmed.Contains(keyword) && trimmed.Length > 10 && trimmed.Length < 200)
                {
                    if (!result.DocumentationRules.Contains(trimmed))
                    {
                        result.DocumentationRules.Add(trimmed);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 코딩 원칙 파싱 (5장 SOLID)
    /// </summary>
    private static void ParseCodingRules(AnalysisResult result)
    {
        var keywords = new[] { "SOLID", "단일 책임", "개방-폐쇄", "리스코프", "인터페이스 분리", "의존 역전",
                               "Dependency Injection", "DI", "单一责任", "开闭", "里氏替换" };

        var lines = result.Content.Split('\n');
        bool inCodingSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 5장 코딩 원칙 섹션 진입
            if (trimmed.Contains("5.") && trimmed.Contains("코딩"))
            {
                inCodingSection = true;
                continue;
            }

            // 다른 장으로 이동 시 탈출
            if (inCodingSection && trimmed.StartsWith("##") && !trimmed.Contains("5."))
            {
                inCodingSection = false;
            }

            if (inCodingSection || keywords.Any(k => trimmed.Contains(k)))
            {
                if (trimmed.Length > 10 && trimmed.Length < 300 && !trimmed.StartsWith("#"))
                {
                    if (!result.CodingRules.Contains(trimmed))
                    {
                        result.CodingRules.Add(trimmed);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 테스트 체크리스트 파싱
    /// </summary>
    private static void ParseTestChecklist(AnalysisResult result)
    {
        var lines = result.Content.Split('\n');
        bool inChecklist = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 체크리스트 섹션 진입
            if (trimmed.Contains("체크리스트") || trimmed.Contains("Checklist"))
            {
                inChecklist = true;
                continue;
            }

            // 체크박스 패턴 [ ] 또는 - [ ]
            if (inChecklist)
            {
                if (trimmed.StartsWith("#"))
                {
                    inChecklist = false;
                    continue;
                }

                if ((trimmed.Contains("[ ]") || trimmed.Contains("[x]") || trimmed.Contains("- [ ]")) &&
                    trimmed.Length > 3 && trimmed.Length < 300)
                {
                    if (!result.TestChecklist.Contains(trimmed))
                    {
                        result.TestChecklist.Add(trimmed);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Git 규칙 파싱 (2장)
    /// </summary>
    private static void ParseGitRules(AnalysisResult result)
    {
        var lines = result.Content.Split('\n');
        bool inGitSection = false;
        string currentKey = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 2장 Git 섹션 진입
            if (trimmed.Contains("2.") && (trimmed.Contains("Git") || trimmed.Contains("커밋")))
            {
                inGitSection = true;
                continue;
            }

            if (inGitSection && trimmed.StartsWith("##") && !trimmed.Contains("2."))
            {
                inGitSection = false;
            }

            if (inGitSection && trimmed.Length > 0)
            {
                if (trimmed.Contains(":") && !trimmed.StartsWith("#"))
                {
                    var parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        currentKey = parts[0].Trim();
                        result.GitRules[currentKey] = parts[1].Trim();
                    }
                }
                else if (!string.IsNullOrEmpty(currentKey) && result.GitRules.ContainsKey(currentKey))
                {
                    result.GitRules[currentKey] += " " + trimmed;
                }
            }
        }
    }

    /// <summary>
    /// 분석 결과를 문자열로 반환 (unity-cli exec에서 사용)
    /// </summary>
    public static string GetAnalysisSummary()
    {
        var result = Analyze();

        if (!string.IsNullOrEmpty(result.Error))
        {
            return $"ERROR: {result.Error}";
        }

        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== AGENTS.md 분석 결과 ===");
        summary.AppendLine();
        summary.AppendLine($"문서화 규칙 ({result.DocumentationRules.Count}):");
        foreach (var rule in result.DocumentationRules.Take(5))
        {
            summary.AppendLine($"  - {rule}");
        }
        if (result.DocumentationRules.Count > 5)
        {
            summary.AppendLine($"  ... 외 {result.DocumentationRules.Count - 5}개");
        }

        summary.AppendLine();
        summary.AppendLine($"코딩 원칙 ({result.CodingRules.Count}):");
        foreach (var rule in result.CodingRules.Take(5))
        {
            summary.AppendLine($"  - {rule}");
        }
        if (result.CodingRules.Count > 5)
        {
            summary.AppendLine($"  ... 외 {result.CodingRules.Count - 5}개");
        }

        summary.AppendLine();
        summary.AppendLine($"테스트 체크리스트 ({result.TestChecklist.Count}):");
        foreach (var item in result.TestChecklist.Take(5))
        {
            summary.AppendLine($"  {item}");
        }
        if (result.TestChecklist.Count > 5)
        {
            summary.AppendLine($"  ... 외 {result.TestChecklist.Count - 5}개");
        }

        summary.AppendLine();
        summary.AppendLine($"Git 규칙: {result.GitRules.Count}개 항목");
        foreach (var kvp in result.GitRules.Take(3))
        {
            summary.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        return summary.ToString();
    }
}

#if UNITY_EDITOR
/// <summary>
/// 테스트 실행 전 AGENTS.md 분석을 자동으로 수행하는 에디터 기능
/// </summary>
public static class AgentsMdTestHook
{
    /// <summary>
    /// 테스트 실행 전에 AGENTS.md를 분석하고 결과를 로그에 출력
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void Initialize()
    {
        Debug.Log("[AgentsMdTestHook] AGENTS.md 분석 시작...");
        var summary = AgentsMdAnalyzer.GetAnalysisSummary();
        Debug.Log(summary);
    }
}
#endif
