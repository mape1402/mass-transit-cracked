namespace MassTransit.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;


    public class DependencyInjectionContainerRegistrar :
        IContainerRegistrar
    {
        protected readonly IServiceCollection Collection;

        public DependencyInjectionContainerRegistrar(IServiceCollection collection)
        {
            Collection = collection;
        }

        public void RegisterRequestClient<T>(RequestTimeout timeout)
            where T : class
        {
            Collection.TryAddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(timeout));
        }

        public void RegisterRequestClient<T>(Uri destinationAddress, RequestTimeout timeout)
            where T : class
        {
            Collection.TryAddScoped(provider => GetScopedBusContext(provider).CreateRequestClient<T>(destinationAddress, timeout));
        }

        public void RegisterScopedClientFactory()
        {
            Collection.TryAddScoped(provider => GetScopedBusContext(provider));
        }

        public T GetOrAdd<T>(Type type, Func<Type, T> missingRegistrationFactory = default)
            where T : class, IRegistration
        {
            if (TryGetValue<T>(type, out var value))
                return value;

            Func<Type, T> factory = missingRegistrationFactory ?? throw new ArgumentNullException(nameof(missingRegistrationFactory));

            value = factory(type);

            AddRegistration(value);

            return value;
        }

        public virtual IEnumerable<T> GetRegistrations<T>()
            where T : class, IRegistration
        {
            return Collection.Where(x => x.ServiceType == typeof(T)).Select(x => x.ImplementationInstance).Cast<T>();
        }

        public bool TryGetValue<T>(IServiceProvider provider, Type type, out T value)
            where T : class, IRegistration
        {
            value = GetRegistrations<T>(provider).FirstOrDefault(x => x.Type == type);

            return value != null;
        }

        public virtual IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration
        {
            return provider.GetService<IEnumerable<T>>() ?? Array.Empty<T>();
        }

        bool TryGetValue<T>(Type type, out T value)
            where T : class, IRegistration
        {
            value = GetRegistrations<T>().FirstOrDefault(x => x.Type == type);

            return value != null;
        }

        protected virtual void AddRegistration<T>(T value)
            where T : class, IRegistration
        {
            Collection.Add(ServiceDescriptor.Singleton(value));
        }

        protected virtual IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
        {
            return provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.ClientFactory;
        }
    }


    public class DependencyInjectionContainerRegistrar<TBus> :
        DependencyInjectionContainerRegistrar
        where TBus : class, IBus
    {
        public DependencyInjectionContainerRegistrar(IServiceCollection collection)
            : base(collection)
        {
        }

        public override IEnumerable<T> GetRegistrations<T>()
        {
            return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, T>))
                .Select(x => x.ImplementationInstance).Cast<Bind<TBus, T>>()
                .Select(x => x.Value);
        }

        public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        {
            return provider.GetService<IEnumerable<Bind<TBus, T>>>().Select(x => x.Value) ?? Array.Empty<T>();
        }

        protected override void AddRegistration<T>(T value)
        {
            Collection.Add(ServiceDescriptor.Singleton(Bind<TBus>.Create(value)));
        }

        protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
        {
            return provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.ClientFactory;
        }
    }
}
