using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AFK.Core.DI
{
    internal sealed class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public object Instance { get; set; }
    }

    public sealed class DIContainer : IDIContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        private readonly DIContainer _parentContainer;
        private readonly bool _isScope;
        private bool _disposed;

        public DIContainer()
        {
            _isScope = false;
            _parentContainer = null;
        }

        private DIContainer(DIContainer parent)
        {
            _isScope = true;
            _parentContainer = parent;
        }

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);

            ValidateRegistration(interfaceType, implementationType);

            var descriptor = new ServiceDescriptor
            {
                ServiceType = interfaceType,
                ImplementationType = implementationType,
                Lifetime = lifetime
            };

            _services[interfaceType] = descriptor;
        }

        public void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImplementation : class
        {
            var implementationType = typeof(TImplementation);

            var descriptor = new ServiceDescriptor
            {
                ServiceType = implementationType,
                ImplementationType = implementationType,
                Lifetime = lifetime
            };

            _services[implementationType] = descriptor;
        }

        public void RegisterInstance<TInterface>(TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "Instance cannot be null.");
            }

            var interfaceType = typeof(TInterface);

            var descriptor = new ServiceDescriptor
            {
                ServiceType = interfaceType,
                ImplementationType = interfaceType,
                Lifetime = lifetime,
                Instance = instance
            };

            _services[interfaceType] = descriptor;

            if (lifetime == ServiceLifetime.Singleton)
            {
                _singletons[interfaceType] = instance;
            }
            else if (lifetime == ServiceLifetime.Scoped)
            {
                _scopedInstances[interfaceType] = instance;
            }
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public IDIContainer CreateScope()
        {
            var scopeContainer = new DIContainer(parent: this);

            foreach (var kvp in _services)
            {
                scopeContainer._services[kvp.Key] = kvp.Value;
            }

            return scopeContainer;
        }

        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        private object Resolve(Type serviceType)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DIContainer), "Container has been disposed.");
            }

            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                if (_parentContainer != null)
                {
                    return _parentContainer.Resolve(serviceType);
                }

                throw new InvalidOperationException(
                    $"Service '{serviceType.Name}' is not registered. " +
                    $"Call Register<TInterface, TImplementation>() first.");
            }

            return CreateInstance(descriptor);
        }

        private object CreateInstance(ServiceDescriptor descriptor)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton && descriptor.Instance != null)
            {
                return descriptor.Instance;
            }

            if (descriptor.Lifetime == ServiceLifetime.Singleton && _singletons.TryGetValue(descriptor.ServiceType, out var existingSingleton))
            {
                return existingSingleton;
            }

            if (descriptor.Lifetime == ServiceLifetime.Scoped && _scopedInstances.TryGetValue(descriptor.ServiceType, out var existingScoped))
            {
                return existingScoped;
            }

            var constructor = GetInjectableConstructor(descriptor.ImplementationType);
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"'{descriptor.ImplementationType.Name}' has no injectable constructor. " +
                    $"Define a single public constructor or use [Inject] attribute.");
            }

            var parameters = constructor.GetParameters();
            var parameterInstances = new List<object>();

            foreach (var parameter in parameters)
            {
                var parameterInstance = Resolve(parameter.ParameterType);
                parameterInstances.Add(parameterInstance);
            }

            var instance = constructor.Invoke(parameterInstances.ToArray());

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _singletons[descriptor.ServiceType] = instance;
            }

            if (descriptor.Lifetime == ServiceLifetime.Scoped)
            {
                _scopedInstances[descriptor.ServiceType] = instance;
            }

            return instance;
        }

        private ConstructorInfo GetInjectableConstructor(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();

            if (constructors.Length == 0) return null;
            if (constructors.Length == 1) return constructors[0];

            foreach (var constructor in constructors)
            {
                var attributes = constructor.GetCustomAttributes(typeof(InjectAttribute), true);
                if (attributes.Length > 0) return constructor;
            }

            return constructors.OrderByDescending(c => c.GetParameters().Length).First();
        }

        private void ValidateRegistration(Type interfaceType, Type implementationType)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            if (!interfaceType.IsAssignableFrom(implementationType))
            {
                throw new InvalidOperationException(
                    $"'{implementationType.Name}' does not implement '{interfaceType.Name}'.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var scoped in _scopedInstances.Values)
            {
                if (scoped is IDisposable disposable) disposable.Dispose();
            }
            _scopedInstances.Clear();

            foreach (var singleton in _singletons.Values)
            {
                if (singleton is IDisposable disposable) disposable.Dispose();
            }

            _singletons.Clear();
            _services.Clear();
        }
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
