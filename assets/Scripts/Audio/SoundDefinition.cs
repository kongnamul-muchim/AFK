using UnityEngine;

/// <summary>
/// 사운드 정의 (CSV audio_definitions.csv 기반)
/// </summary>
[System.Serializable]
public class SoundDefinition
{
    public string soundId;
    public string fileName;
    public string type;      // "sfx" or "bgm"
    public float defaultVolume = 0.8f;
    
    /// <summary>실제 로드된 AudioClip (Resources에서 로드)</summary>
    [System.NonSerialized] public AudioClip clip;
    [System.NonSerialized] public bool loaded;
}
