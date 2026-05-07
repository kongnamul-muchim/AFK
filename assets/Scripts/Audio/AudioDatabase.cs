using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 사운드 데이터베이스 - CSV audio_definitions.csv 기반 사운드 로드 및 캐싱
/// Resources/audio/ 폴더에서 AudioClip을 로드합니다.
/// </summary>
public static class AudioDatabase
{
    private static Dictionary<string, SoundDefinition> _sounds;
    private static bool _initialized = false;

    /// <summary>
    /// 사운드 데이터베이스 초기화 (CSV + Resources에서 로드)
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        _sounds = new Dictionary<string, SoundDefinition>();
        var soundData = DataLoader.Load("audio_definitions");
        
        if (soundData == null || soundData.Count == 0)
        {
            Debug.LogWarning("[AudioDB] audio_definitions.csv를 찾을 수 없습니다.");
            _initialized = true;
            return;
        }
        
        foreach (var row in soundData)
        {
            var def = new SoundDefinition
            {
                soundId = row["sound_id"]?.ToString() ?? "",
                fileName = row["file_name"]?.ToString() ?? "",
                type = row["type"]?.ToString() ?? "sfx",
                defaultVolume = ParseFloat(row["volume_default"], 0.8f)
            };
            
            if (!string.IsNullOrEmpty(def.soundId))
            {
                _sounds[def.soundId] = def;
            }
        }
        
        Debug.Log($"[AudioDB] {_sounds.Count}개 사운드 정의 로드 완료");
        _initialized = true;
    }

    /// <summary>
    /// 사운드 ID로 AudioClip을 Resources에서 로드하여 반환
    /// </summary>
    public static AudioClip GetClip(string soundId)
    {
        if (!_initialized) Initialize();
        
        if (!_sounds.TryGetValue(soundId, out var def))
        {
            Debug.LogWarning($"[AudioDB] 알 수 없는 사운드 ID: {soundId}");
            return null;
        }
        
        if (!def.loaded)
        {
            // Resources/audio/ 에서 클립 로드 (확장자 제외)
            string path = $"audio/{def.fileName}";
            int extIndex = path.LastIndexOf('.');
            if (extIndex > 0) path = path.Substring(0, extIndex);
            
            def.clip = Resources.Load<AudioClip>(path);
            def.loaded = true;
            
            if (def.clip == null)
            {
                Debug.LogWarning($"[AudioDB] 사운드 파일 없음: {def.fileName} (Resources/{path})");
            }
        }
        
        return def.clip;
    }

    /// <summary>
    /// 모든 사운드 프리로드 (선택사항)
    /// </summary>
    public static void PreloadAll()
    {
        foreach (var key in _sounds.Keys)
        {
            GetClip(key);
        }
        Debug.Log($"[AudioDB] 프리로드 완료: {_sounds.Count}개");
    }

    private static float ParseFloat(object value, float defaultValue)
    {
        if (value == null) return defaultValue;
        float result;
        if (float.TryParse(value.ToString(), out result))
            return result;
        return defaultValue;
    }
}
