using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 범용 GameObject 풀 (재사용 가능한 오브젝트 관리)
/// Phase 6: 성능 최적화용
/// </summary>
public class GameObjectPool
{
    private readonly GameObject _prefab;
    private readonly Queue<GameObject> _available = new Queue<GameObject>();
    private readonly List<GameObject> _active = new List<GameObject>();
    private readonly Transform _parent;
    private readonly int _maxSize;

    /// <summary>현재 사용 가능한 오브젝트 수</summary>
    public int AvailableCount => _available.Count;
    /// <summary>현재 활성화된 오브젝트 수</summary>
    public int ActiveCount => _active.Count;

    public GameObjectPool(GameObject prefab, int preloadCount, Transform parent = null, int maxSize = 100)
    {
        _prefab = prefab;
        _parent = parent;
        _maxSize = maxSize;

        // 미리 오브젝트 생성
        for (int i = 0; i < preloadCount; i++)
        {
            var obj = CreateNew();
            obj.SetActive(false);
            _available.Enqueue(obj);
        }
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    public GameObject Get()
    {
        GameObject obj;
        if (_available.Count > 0)
        {
            obj = _available.Dequeue();
        }
        else if (_active.Count + _available.Count < _maxSize)
        {
            obj = CreateNew();
        }
        else
        {
            // 최대 크기 도달: 가장 오래된 활성 오브젝트 재사용
            obj = _active[0];
            _active.RemoveAt(0);
        }

        obj.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀로 반환
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        _active.Remove(obj);
        
        if (!_available.Contains(obj))
        {
            _available.Enqueue(obj);
        }
    }

    /// <summary>
    /// 모든 활성 오브젝트를 풀로 반환
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var obj in _active)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                _available.Enqueue(obj);
            }
        }
        _active.Clear();
    }

    /// <summary>
    /// 풀 정리
    /// </summary>
    public void Clear()
    {
        foreach (var obj in _available)
        {
            if (obj != null)
                UnityEngine.Object.Destroy(obj);
        }
        foreach (var obj in _active)
        {
            if (obj != null)
                UnityEngine.Object.Destroy(obj);
        }
        _available.Clear();
        _active.Clear();
    }

    private GameObject CreateNew()
    {
        var obj = Object.Instantiate(_prefab, _parent);
        obj.name = $"{_prefab.name}_Pooled";
        return obj;
    }
}

/// <summary>
/// MonoBehaviour용 범용 오브젝트 풀 컴포넌트 (씬에 배치 가능)
/// </summary>
public class ComponentPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Queue<T> _available = new Queue<T>();
    private readonly List<T> _active = new List<T>();
    private readonly Transform _parent;
    private readonly int _maxSize;

    public int AvailableCount => _available.Count;
    public int ActiveCount => _active.Count;

    public ComponentPool(T prefab, int preloadCount, Transform parent = null, int maxSize = 100)
    {
        _prefab = prefab;
        _parent = parent;
        _maxSize = maxSize;

        for (int i = 0; i < preloadCount; i++)
        {
            var obj = CreateNew();
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj;
        if (_available.Count > 0)
        {
            obj = _available.Dequeue();
        }
        else if (_active.Count + _available.Count < _maxSize)
        {
            obj = CreateNew();
        }
        else
        {
            obj = _active[0];
            _active.RemoveAt(0);
        }

        obj.gameObject.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(false);
        _active.Remove(obj);
        if (!_available.Contains(obj))
        {
            _available.Enqueue(obj);
        }
    }

    public void ReleaseAll()
    {
        foreach (var obj in _active)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                _available.Enqueue(obj);
            }
        }
        _active.Clear();
    }

    public void Clear()
    {
        foreach (var obj in _available)
        {
            if (obj != null) Object.Destroy(obj.gameObject);
        }
        foreach (var obj in _active)
        {
            if (obj != null) Object.Destroy(obj.gameObject);
        }
        _available.Clear();
        _active.Clear();
    }

    private T CreateNew()
    {
        var obj = Object.Instantiate(_prefab, _parent);
        obj.name = $"{_prefab.name}_Pooled";
        return obj;
    }
}
