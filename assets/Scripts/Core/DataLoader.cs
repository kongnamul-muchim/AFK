using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSV 데이터 로더
/// Resources 폴더의 CSV 파일을 로드하고 캐싱
/// </summary>
public static class DataLoader
{
    private static Dictionary<string, List<Dictionary<string, object>>> _cache = 
        new Dictionary<string, List<Dictionary<string, object>>>();

    /// <summary>
    /// CSV 데이터 로드
    /// </summary>
    public static List<Dictionary<string, object>> Load(string csvName)
    {
        if (_cache.TryGetValue(csvName, out var cached))
            return cached;

        var textAsset = Resources.Load<TextAsset>($"data/{csvName}");
        if (textAsset == null)
        {
            Debug.LogError($"CSV file not found: data/{csvName}.csv");
            return new List<Dictionary<string, object>>();
        }

        var data = CSVParser.Parse(textAsset.text);
        _cache[csvName] = data;
        
        Debug.Log($"Loaded {csvName}.csv: {data.Count} rows");
        return data;
    }

    /// <summary>
    /// 캐시 초기화
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
