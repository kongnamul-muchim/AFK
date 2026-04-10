using UnityEngine;

namespace AFK.Core.Systems
{
    /// <summary>
    /// 시스템 클래스의 기반이 되는 추상 클래스
    /// 공통적인 DI 패턴을 제공하여 코드 중복을 방지 (OCP, DRY)
    /// </summary>
    public abstract class BaseSystem : MonoBehaviour
    {
        // ========== 공통 의존성 ==========
        
        /// <summary>게임 상태 인터페이스</summary>
        protected IGameState _gameState;
        
        /// <summary>이벤트 버스 인터페이스</summary>
        protected IEventBus _eventBus;
        
        /// <summary>로거 인터페이스</summary>
        protected ILogger _logger;
        
        /// <summary>의존성 주입 완료 여부</summary>
        private bool _dependenciesInjected = false;
        
        /// <summary>
        /// ServiceLocator를 통한 의존성 주입 (지연 초기화 지원)
        /// </summary>
        protected virtual void InjectDependencies()
        {
            if (_dependenciesInjected) return;
            
            var serviceLocator = ServiceLocator.Instance;
            if (serviceLocator == null)
            {
                Debug.LogError($"[{GetType().Name}] ServiceLocator 인스턴스를 찾을 수 없습니다!");
                return;
            }
            
            _gameState = serviceLocator.Get<IGameState>();
            _eventBus = serviceLocator.Get<IEventBus>();
            _logger = serviceLocator.Get<ILogger>();
            
            _dependenciesInjected = true;
        }
        
        /// <summary>
        /// 의존성 확인 - null 체크와 함께 지연 주입 수행
        /// </summary>
        protected void EnsureDependencies()
        {
            if (!_dependenciesInjected)
            {
                InjectDependencies();
            }
            
            if (_gameState == null)
                Debug.LogWarning($"[{GetType().Name}] IGameState가 아직 초기화되지 않았습니다.");
            if (_eventBus == null)
                Debug.LogWarning($"[{GetType().Name}] IEventBus가 아직 초기화되지 않았습니다.");
            if (_logger == null)
                Debug.LogWarning($"[{GetType().Name}] ILogger가 아직 초기화되지 않았습니다.");
        }
        
        /// <summary>
        /// Awake에서 호출되는 기본 의존성 주입
        /// </summary>
        protected virtual void Awake()
        {
            InjectDependencies();
        }
        
        /// <summary>
        /// OnEnable에서 호출되는 의존성 확인 및 이벤트 구독
        /// </summary>
        protected virtual void OnEnable()
        {
            EnsureDependencies();
            SubscribeEvents();
        }
        
        /// <summary>
        /// OnDisable에서 호출되는 이벤트 해제
        /// </summary>
        protected virtual void OnDisable()
        {
            UnsubscribeEvents();
        }
        
        /// <summary>
        /// 이벤트 구독 - 하위 클래스에서 오버라이드
        /// </summary>
        protected virtual void SubscribeEvents() { }
        
        /// <summary>
        /// 이벤트 해제 - 하위 클래스에서 오버라이드
        /// </summary>
        protected virtual void UnsubscribeEvents() { }
    }
}
