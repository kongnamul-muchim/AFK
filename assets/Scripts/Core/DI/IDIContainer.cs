using System;

namespace AFK.Core.DI
{
    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton
    }

    public interface IDIContainer : IDisposable
    {
        void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TImplementation : class, TInterface;

        void Register<TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImplementation : class;

        void RegisterInstance<TInterface>(TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class;

        T Resolve<T>() where T : class;

        IDIContainer CreateScope();

        bool IsRegistered<T>() where T : class;
    }
}
