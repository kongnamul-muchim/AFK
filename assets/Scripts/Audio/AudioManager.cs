using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 오디오 시스템을 관리하는 클래스
/// BGM, SFX, UI 사운드를 Object Pooling으로 효율적으로 관리합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    
    /// <summary>
    /// AudioManager의 싱글톤 인스턴스
    /// </summary>
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IGameLogger _logger;
    
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;
        if (_gameState == null)
            _gameState = Bootstrap.Container.Resolve<IGameState>();
        if (_logger == null)
            _logger = Bootstrap.Container.Resolve<IGameLogger>();
    }

    // ========== 오디오 소스 풀 ==========
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private int _sfxPoolSize = 10;
    [SerializeField] private int _uiSourceCount = 2;
    
    private List<AudioSource> _sfxPool = new List<AudioSource>();
    private List<AudioSource> _uiSources = new List<AudioSource>();
    
    // ========== 볼륨 설정 ==========
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float _uiVolume = 0.6f;
    
    // ========== 페이드 설정 ==========
    
    [Header("Fade Settings")]
    [SerializeField] private float _fadeDuration = 1f;
    
    // ========== 상태 ==========
    
    private bool _isMuted = false;
    private float _targetBgmVolume;
    private float _targetSfxVolume;
    private float _targetUiVolume;
    
    // ========== MonoBehaviour 라이프사이클 ==========
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 의존성 주입
        InjectDependencies();
        
        // 초기화
        InitializeAudio();
    }
    
    private void Update()
    {
        UpdateVolume();
    }
    
    // ========== 초기화 ==========
    
    private void InitializeAudio()
    {
        // BGM 소스 설정
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = _bgmVolume;
        }
        
        // SFX 풀 생성
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = _sfxVolume;
            _sfxPool.Add(source);
        }
        
        // UI 소스 생성
        for (int i = 0; i < _uiSourceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = _uiVolume;
            _uiSources.Add(source);
        }
        
        // 저장된 볼륨 설정 로드
        LoadVolumeSettings();
        
        _targetBgmVolume = _bgmVolume;
        _targetSfxVolume = _sfxVolume;
        _targetUiVolume = _uiVolume;
        
        _logger.Debug("오디오 매니저 초기화 완료");
    }
    
    // ========== BGM ==========
    
    /// <summary>
    /// BGM 재생 (페이드 인)
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource == null)
        {
            _logger.Warn("BGM 클립이 없음");
            return;
        }
        
        _bgmSource.clip = clip;
        _bgmSource.Play();
        
        // 페이드 인
        StartCoroutine(FadeVolume(_bgmSource, 0f, _targetBgmVolume, _fadeDuration));
        
        _logger.Debug($"BGM 재생: {clip.name}");
    }
    
    /// <summary>
    /// BGM 정지 (페이드 아웃)
    /// </summary>
    public void StopBGM()
    {
        if (_bgmSource == null || !_bgmSource.isPlaying) return;
        
        StartCoroutine(FadeVolume(_bgmSource, _bgmSource.volume, 0f, _fadeDuration));
        
        _logger.Debug("BGM 정지");
    }
    
    /// <summary>
    /// BGM 볼륨 설정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _targetBgmVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }
    
    // ========== SFX ==========
    
    /// <summary>
    /// 효과음 재생
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    /// <param name="position">3D 위치 (null이면 2D)</param>
    public void PlaySFX(AudioClip clip, Vector3? position = null)
    {
        if (clip == null)
        {
            _logger.Warn("SFX 클립이 없음");
            return;
        }
        
        AudioSource source = GetAvailableSFXSource();
        if (source == null)
        {
            _logger.Warn("사용 가능한 SFX 소스 없음");
            return;
        }
        
        if (position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f; // 3D 사운드
        }
        else
        {
            source.spatialBlend = 0f; // 2D 사운드
        }
        
        source.clip = clip;
        source.Play();
        
        // 자동 정리
        StartCoroutine(ReleaseSFXSource(source, clip.length));
    }
    
    /// <summary>
    /// 효과음 볼륨 설정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _targetSfxVolume = Mathf.Clamp01(volume);
        foreach (var source in _sfxPool)
        {
            source.volume = _targetSfxVolume;
        }
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 모든 효과음 정지
    /// </summary>
    public void StopAllSFX()
    {
        foreach (var source in _sfxPool)
        {
            source.Stop();
        }
    }
    
    // ========== UI 사운드 ==========
    
    /// <summary>
    /// UI 사운드 재생
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null)
        {
            _logger.Warn("UI 사운드 클립이 없음");
            return;
        }
        
        AudioSource source = GetAvailableUISource();
        if (source == null) return;
        
        source.clip = clip;
        source.Play();
        
        StartCoroutine(ReleaseUISource(source, clip.length));
    }
    
    /// <summary>
    /// UI 사운드 볼륨 설정
    /// </summary>
    public void SetUIVolume(float volume)
    {
        _targetUiVolume = Mathf.Clamp01(volume);
        foreach (var source in _uiSources)
        {
            source.volume = _targetUiVolume;
        }
        SaveVolumeSettings();
    }
    
    // ========== 뮤트 ==========
    
    /// <summary>
    /// 전체 음량 음소거
    /// </summary>
    /// <param name="muted">음소거 여부</param>
    public void SetMute(bool muted)
    {
        _isMuted = muted;
        AudioListener.volume = muted ? 0f : 1f;
        
        _logger.Info($"음소거: {muted}");
    }
    
    /// <summary>
    /// 음소거 상태 토글
    /// </summary>
    public void ToggleMute()
    {
        SetMute(!_isMuted);
    }
    
    // ========== 모바일 진동 ==========
    
    /// <summary>
    /// 진동 (모바일)
    /// </summary>
    /// <param name="duration">지속 시간 (ms)</param>
    public void Vibrate(int duration = 50)
    {
        // 모바일 진동 (설정 확인)
        Handheld.Vibrate();
    }
    
    /// <summary>
    /// 진동 패턴
    /// </summary>
    /// <param name="pattern">진동/일시정지 패턴 (ms)</param>
    public void VibratePattern(int[] pattern)
    {
        foreach (var duration in pattern)
        {
            Handheld.Vibrate();
        }
    }
    
    // ========== 유틸리티 ==========
    
    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in _sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return null;
    }
    
    private AudioSource GetAvailableUISource()
    {
        foreach (var source in _uiSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return null;
    }
    
    private System.Collections.IEnumerator ReleaseSFXSource(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        source.clip = null;
    }
    
    private System.Collections.IEnumerator ReleaseUISource(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        source.clip = null;
    }
    
    private void UpdateVolume()
    {
        // 볼륨 점진적 변경
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.volume = Mathf.Lerp(_bgmSource.volume, _targetBgmVolume, Time.deltaTime * 5f);
        }
    }
    
    private System.Collections.IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        
        source.volume = to;
        
        if (to == 0f)
        {
            source.Stop();
        }
    }
    
    // ========== 저장/복원 ==========
    
    private void SaveVolumeSettings()
    {
        if (_gameState == null) return;
        
        var settings = _gameState.Settings;
        settings.soundVolume = _targetSfxVolume;  // soundVolume을 SFX로 사용
        settings.musicVolume = _targetBgmVolume;  // musicVolume을 BGM으로 사용
        _gameState.Settings = settings;
    }
    
    private void LoadVolumeSettings()
    {
        if (_gameState == null) return;
        
        var settings = _gameState.Settings;
        _targetBgmVolume = settings.musicVolume > 0 ? settings.musicVolume : _bgmVolume;
        _targetSfxVolume = settings.soundVolume > 0 ? settings.soundVolume : _sfxVolume;
        _targetUiVolume = _uiVolume; // UI 볼륨은 별도 저장 필드 없음
        
        SetBGMVolume(_targetBgmVolume);
        SetSFXVolume(_targetSfxVolume);
    }
    
    // ========== 편의 메서드 ==========
    
    /// <summary>
    /// 공격 사운드 재생
    /// </summary>
    public void PlayAttackSound()
    {
        // 실제로는 AudioDefinitionSO에서 클립을 가져와야 함
        _logger.Debug("공격 사운드");
    }
    
    /// <summary>
    /// 피격 사운드 재생
    /// </summary>
    public void PlayHitSound()
    {
        _logger.Debug("피격 사운드");
    }
    
    /// <summary>
    /// 승리 사운드 재생
    /// </summary>
    public void PlayVictorySound()
    {
        _logger.Debug("승리 사운드");
    }
    
    /// <summary>
    /// 버튼 클릭 사운드
    /// </summary>
    public void PlayButtonClick()
    {
        _logger.Debug("버튼 클릭 사운드");
    }
}
