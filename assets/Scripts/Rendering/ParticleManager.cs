using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 파티클 이펙트 관리 클래스
/// Object Pooling을 사용하여 성능을 최적화합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class ParticleManager : MonoBehaviour
{
    private static ParticleManager _instance;
    
    /// <summary>
    /// ParticleManager의 싱글톤 인스턴스
    /// </summary>
    public static ParticleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ParticleManager");
                _instance = go.AddComponent<ParticleManager>();
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
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
    }

    // ========== 파티클 풀 ==========
    
    [System.Serializable]
    public class ParticlePool
    {
        public ParticleSystem prefab;
        public int poolSize = 10;
        [HideInInspector]
        public Queue<ParticleSystem> available = new Queue<ParticleSystem>();
        [HideInInspector]
        public List<ParticleSystem> active = new List<ParticleSystem>();
    }
    
    [Header("Particle Pools")]
    [SerializeField] private ParticlePool[] _particlePools;
    
    [Header("Default Settings")]
    [SerializeField] private Transform _particleParent;
    [SerializeField] private int defaultPoolSize = 20;
    
    private Dictionary<string, ParticlePool> _pools = new Dictionary<string, ParticlePool>();
    
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
        
        // 풀 초기화
        InitializePools();
    }
    
    private void InitializePools()
    {
        if (_particleParent == null)
        {
            _particleParent = transform;
        }
        
        foreach (var pool in _particlePools)
        {
            if (pool.prefab == null) continue;
            
            string poolId = pool.prefab.name;
            _pools[poolId] = pool;
            
            for (int i = 0; i < pool.poolSize; i++)
            {
                ParticleSystem particle = Instantiate(pool.prefab, _particleParent);
                particle.gameObject.SetActive(false);
                pool.available.Enqueue(particle);
            }
        }
        
        _logger.Debug($"파티클 풀 초기화 완료 ({_pools.Count}개 풀)");
    }
    
    // ========== 파티클 재생 ==========
    
    /// <summary>
    /// 파티클 재생 (위치 지정)
    /// </summary>
    /// <param name="particleId">파티클 프리팹 이름</param>
    /// <param name="position">위치</param>
    public void PlayParticle(string particleId, Vector3 position)
    {
        if (!_pools.ContainsKey(particleId))
        {
            _logger.Warn($"파티클 풀을 찾을 수 없음: {particleId}");
            return;
        }
        
        ParticlePool pool = _pools[particleId];
        
        if (pool.available.Count == 0)
        {
            // 풀이 부족하면 확장
            _logger.Debug($"파티클 풀 확장: {particleId}");
            ExpandPool(pool);
        }
        
        ParticleSystem particle = pool.available.Dequeue();
        particle.transform.position = position;
        particle.gameObject.SetActive(true);
        particle.Play();
        
        pool.active.Add(particle);
        
        // 자동 정리 예약
        StartCoroutine(StopAfterDuration(particle, pool));
    }
    
    /// <summary>
    /// 파티클 재생 (회전 포함)
    /// </summary>
    /// <param name="particleId">파티클 프리팹 이름</param>
    /// <param name="position">위치</param>
    /// <param name="rotation">회전</param>
    public void PlayParticle(string particleId, Vector3 position, Quaternion rotation)
    {
        if (!_pools.ContainsKey(particleId))
        {
            _logger.Warn($"파티클 풀을 찾을 수 없음: {particleId}");
            return;
        }
        
        ParticlePool pool = _pools[particleId];
        
        if (pool.available.Count == 0)
        {
            ExpandPool(pool);
        }
        
        ParticleSystem particle = pool.available.Dequeue();
        particle.transform.position = position;
        particle.transform.rotation = rotation;
        particle.gameObject.SetActive(true);
        particle.Play();
        
        pool.active.Add(particle);
        
        StartCoroutine(StopAfterDuration(particle, pool));
    }
    
    /// <summary>
    /// 파티클 즉시 정지 및 반환
    /// </summary>
    /// <param name="particleId">파티클 ID</param>
    public void StopParticle(string particleId)
    {
        if (!_pools.ContainsKey(particleId)) return;
        
        ParticlePool pool = _pools[particleId];
        
        if (pool.active.Count > 0)
        {
            ParticleSystem particle = pool.active[pool.active.Count - 1];
            particle.Stop();
            particle.gameObject.SetActive(false);
            
            pool.active.RemoveAt(pool.active.Count - 1);
            pool.available.Enqueue(particle);
        }
    }
    
    /// <summary>
    /// 모든 파티클 정지
    /// </summary>
    public void StopAllParticles()
    {
        foreach (var kvp in _pools)
        {
            ParticlePool pool = kvp.Value;
            
            // 활성 파티클 복사본을 만들어 순회
            var activeCopy = new List<ParticleSystem>(pool.active);
            foreach (var particle in activeCopy)
            {
                particle.Stop();
                particle.gameObject.SetActive(false);
                pool.available.Enqueue(particle);
            }
            
            pool.active.Clear();
        }
    }
    
    // ========== 풀 관리 ==========
    
    private void ExpandPool(ParticlePool pool)
    {
        int expandCount = pool.poolSize / 2; // 50% 확장
        
        for (int i = 0; i < expandCount; i++)
        {
            ParticleSystem particle = Instantiate(pool.prefab, _particleParent);
            particle.gameObject.SetActive(false);
            pool.available.Enqueue(particle);
        }
        
        _logger.Debug($"파티클 풀 확장 완료: +{expandCount}개");
    }
    
    private System.Collections.IEnumerator StopAfterDuration(ParticleSystem particle, ParticlePool pool)
    {
        // 파티클의 main.duration + startLifetime 대기
        float duration = particle.main.duration + particle.main.startLifetime.constant;
        
        yield return new WaitForSeconds(duration);
        
        if (particle != null)
        {
            particle.Stop();
            particle.gameObject.SetActive(false);
            
            pool.active.Remove(particle);
            pool.available.Enqueue(particle);
        }
    }
    
    // ========== 편의 메서드 ==========
    
    /// <summary>
    /// 공격 이펙트 재생
    /// </summary>
    public void PlayAttackEffect(Vector3 position)
    {
        PlayParticle("ParticleAttackHit", position);
    }
    
    /// <summary>
    /// 피격 이펙트 재생
    /// </summary>
    public void PlayDamageEffect(Vector3 position)
    {
        PlayParticle("ParticleDamageFlash", position);
    }
    
    /// <summary>
    /// 처치 이펙트 재생
    /// </summary>
    public void PlayDeathEffect(Vector3 position)
    {
        PlayParticle("ParticleMonsterDeath", position);
    }
    
    /// <summary>
    /// 레벨업 이펙트 재생
    /// </summary>
    public void PlayLevelUpEffect(Vector3 position)
    {
        PlayParticle("ParticleLevelUp", position);
    }
    
    /// <summary>
    /// 합성 이펙트 재생
    /// </summary>
    public void PlaySynthesisEffect(Vector3 position)
    {
        PlayParticle("ParticleSynthesis", position);
    }
    
    /// <summary>
    /// 환생 이펙트 재생
    /// </summary>
    public void PlayRebirthEffect(Vector3 position)
    {
        PlayParticle("ParticleRebirth", position);
    }
    
    // ========== 디버그 ==========
    
    /// <summary>
    /// 파티클 풀 상태 출력
    /// </summary>
    public void PrintPoolStatus()
    {
        _logger.Info("=== 파티클 풀 상태 ===");
        foreach (var kvp in _pools)
        {
            _logger.Info($"{kvp.Key}: 활성={kvp.Value.active.Count}, 대기={kvp.Value.available.Count}");
        }
    }
    
    /// <summary>
    /// 현재 활성 파티클 수
    /// </summary>
    public int GetActiveParticleCount()
    {
        int count = 0;
        foreach (var kvp in _pools)
        {
            count += kvp.Value.active.Count;
        }
        return count;
    }
}
