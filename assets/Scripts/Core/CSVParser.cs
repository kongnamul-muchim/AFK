using System;
using System.Collections.Generic;

/// <summary>
/// CSV 파일 파서
/// 주석 처리, 큰따옴표 이스케이프, 타입 자동 변환 지원
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// CSV 텍스트 파싱
    /// </summary>
    public static List<Dictionary<string, object>> Parse(string text)
    {
        var result = new List<Dictionary<string, object>>();
        if (string.IsNullOrEmpty(text)) return result;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        // 헤더 행 추출 (주석/빈 행 스킵)
        int headerIndex = 0;
        while (headerIndex < lines.Length)
        {
            var line = lines[headerIndex].Trim();
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                break;
            headerIndex++;
        }
        
        if (headerIndex >= lines.Length) return result;
        
        var headers = ParseLine(lines[headerIndex]);
        
        // 데이터 행 파싱
        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
            
            var values = ParseLine(line);
            var row = new Dictionary<string, object>();
            
            for (int j = 0; j < headers.Count && j < values.Count; j++)
            {
                row[headers[j]] = ConvertType(values[j]);
            }
            
            result.Add(row);
        }
        
        return result;
    }

    /// <summary>
    /// CSV 행 파싱 (큰따옴표 처리 포함)
    /// </summary>
    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        
        result.Add(current.Trim());
        return result;
    }

    /// <summary>
    /// 타입 자동 변환
    /// </summary>
    private static object ConvertType(string value)
    {
        if (int.TryParse(value, out var intValue)) return intValue;
        if (float.TryParse(value, out var floatValue)) return floatValue;
        if (bool.TryParse(value, out var boolValue)) return boolValue;
        return value;
    }
}
