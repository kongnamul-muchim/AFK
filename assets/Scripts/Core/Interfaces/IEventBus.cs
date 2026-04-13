using System;

/// <summary>
/// 이벤트 버스 인터페이스
/// 문자열 기반 이벤트 시스템을 위한 인터페이스
/// </summary>
public interface IEventBus
{
    void On(string eventName, Action callback);
    void Off(string eventName, Action callback);
    void Emit(string eventName);
    void Clear();
    void Once(string eventName, Action callback);
}
