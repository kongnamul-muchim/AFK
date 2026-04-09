using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;

/// <summary>
/// EventBus 클래스에 대한 단위 테스트
/// </summary>
public class EventBusTests
{
    private EventBus _eventBus;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("EventBus_Test");
        _eventBus = go.AddComponent<EventBus>();
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.DestroyImmediate(_eventBus.gameObject);
    }

    /// <summary>
    /// EventBus 싱글톤 인스턴스가 정상적으로 생성되는지 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_Singleton_CreatesInstance()
    {
        var eventBus = EventBus.Instance;
        Assert.IsNotNull(eventBus);
        yield return null;
    }

    /// <summary>
    /// 이벤트 등록 및 발생 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_Event_EmitsCorrectly()
    {
        bool eventFired = false;
        
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { eventFired = true; });
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        
        Assert.IsTrue(eventFired);
        yield return null;
    }

    /// <summary>
    /// 이벤트 해제 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_Event_OffRemovesListener()
    {
        int fireCount = 0;
        System.Action callback = () => { fireCount++; };
        
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, callback);
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        Assert.AreEqual(1, fireCount);
        
        _eventBus.Off(GameEvents.PLAYER_LEVEL_UP, callback);
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        Assert.AreEqual(1, fireCount); // 여전히 1이어야 함
        
        yield return null;
    }

    /// <summary>
    /// 1회용 이벤트 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_Once_OnlyFiresOnce()
    {
        int fireCount = 0;
        
        _eventBus.Once(GameEvents.PLAYER_LEVEL_UP, () => { fireCount++; });
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        
        Assert.AreEqual(1, fireCount);
        yield return null;
    }

    /// <summary>
    /// 여러 리스너 등록 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_MultipleListeners_AllFire()
    {
        int fireCount = 0;
        
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { fireCount++; });
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { fireCount++; });
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { fireCount++; });
        
        _eventBus.Emit(GameEvents.PLAYER_LEVEL_UP);
        
        Assert.AreEqual(3, fireCount);
        yield return null;
    }

    /// <summary>
    /// 리스너 존재 여부 확인 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_HasListeners_ReturnsCorrectly()
    {
        Assert.IsFalse(_eventBus.HasListeners(GameEvents.PLAYER_LEVEL_UP));
        
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { });
        Assert.IsTrue(_eventBus.HasListeners(GameEvents.PLAYER_LEVEL_UP));
        
        yield return null;
    }

    /// <summary>
    /// 리스너 수 확인 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_GetListenerCount_ReturnsCorrectly()
    {
        Assert.AreEqual(0, _eventBus.GetListenerCount(GameEvents.PLAYER_LEVEL_UP));
        
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { });
        _eventBus.On(GameEvents.PLAYER_LEVEL_UP, () => { });
        
        Assert.AreEqual(2, _eventBus.GetListenerCount(GameEvents.PLAYER_LEVEL_UP));
        
        yield return null;
    }

    /// <summary>
    /// 빈 이벤트명으로 등록 시도 시 에러 발생 테스트
    /// </summary>
    [UnityTest]
    public IEnumerator EventBus_EmptyEventName_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => {
            _eventBus.On("", () => { });
        });
        
        yield return null;
    }
}
