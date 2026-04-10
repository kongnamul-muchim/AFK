using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 의존성 주입을 위한 Service Locator
/// DIP 준수: 구체적 구현 대신 인터페이스를 통해 서비스 접근
/// </summary>
public class ServiceLocator : MonoBehaviour
{
    private static ServiceLocator _instance;
    
    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ServiceLocator");
                _instance = go.AddComponent<ServiceLocator>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 싱글톤 인스턴스를 서비스로 등록
    /// </summary>
    public void RegisterSingleton<TInterface, TService>(TService instance) 
        where TService : class, TInterface
    {
        _services[typeof(TInterface)] = instance;
        GameLogger.DebugLog($"Service registered: {typeof(TInterface).Name}");
    }

    /// <summary>
    /// 팩토리 기반 서비스 등록 (필요시 생성)
    /// </summary>
    public void RegisterFactory<TInterface>(Func<TInterface> factory)
    {
        _factories[typeof(TInterface)] = () => factory();
    }

    /// <summary>
    /// 서비스 조회
    /// </summary>
    public TInterface Get<TInterface>()
    {
        if (_services.TryGetValue(typeof(TInterface), out var service))
        {
            return (TInterface)service;
        }

        if (_factories.TryGetValue(typeof(TInterface), out var factory))
        {
            var instance = (TInterface)factory();
            _services[typeof(TInterface)] = instance;
            return instance;
        }

        throw new KeyNotFoundException($"Service not found: {typeof(TInterface).Name}");
    }

    /// <summary>
    /// 서비스 등록 여부 확인
    /// </summary>
    public bool IsRegistered<TInterface>()
    {
        return _services.ContainsKey(typeof(TInterface)) || _factories.ContainsKey(typeof(TInterface));
    }

    /// <summary>
    /// 모든 서비스 초기화
    /// </summary>
    public void Clear()
    {
        _services.Clear();
        _factories.Clear();
    }
}

/// <summary>
/// ServiceLocator 확장 메서드 (편의성 제공)
/// </summary>
public static class ServiceLocatorExtensions
{
    /// <summary>
    /// GameState를 간편하게 가져오기
    /// </summary>
    public static IGameState GetGameState(this ServiceLocator locator)
    {
        return locator.Get<IGameState>();
    }

    /// <summary>
    /// EventBus를 간편하게 가져오기
    /// </summary>
    public static IEventBus GetEventBus(this ServiceLocator locator)
    {
        return locator.Get<IEventBus>();
    }

    /// <summary>
    /// Logger를 간편하게 가져오기
    /// </summary>
    public static ILogger GetLogger(this ServiceLocator locator)
    {
        return locator.Get<ILogger>();
    }

    /// <summary>
    /// SaveManager를 간편하게 가져오기
    /// </summary>
    public static ISaveManager GetSaveManager(this ServiceLocator locator)
    {
        return locator.Get<ISaveManager>();
    }
}
