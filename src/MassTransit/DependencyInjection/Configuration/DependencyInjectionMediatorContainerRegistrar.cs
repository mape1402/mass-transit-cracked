namespace MassTransit.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Clients;
    using DependencyInjection;
    using Mediator;
    using Microsoft.Extensions.DependencyInjection;


    public class DependencyInjectionMediatorContainerRegistrar :
        DependencyInjectionContainerRegistrar
    {
        public DependencyInjectionMediatorContainerRegistrar(IServiceCollection collection)
            : base(collection)
        {
        }

        public override IEnumerable<T> GetRegistrations<T>()
        {
            return Collection.Where(x => x.ServiceType == typeof(Bind<IMediator, T>))
                .Select(x => x.ImplementationInstance).Cast<Bind<IMediator, T>>()
                .Select(x => x.Value);
        }

        public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
        {
            return provider.GetService<IEnumerable<Bind<IMediator, T>>>().Select(x => x.Value) ?? Array.Empty<T>();
        }

        protected override void AddRegistration<T>(T value)
        {
            Collection.Add(ServiceDescriptor.Singleton(Bind<IMediator>.Create(value)));
        }

        protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
        {
            var clientFactory = provider.GetRequiredService<IScopedMediator>();
            var consumeContext = provider.GetRequiredService<ScopedConsumeContextProvider>().GetContext();

            return consumeContext != null
                ? new ScopedClientFactory(clientFactory, consumeContext)
                : new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(clientFactory, provider)), null);
        }
    }
}
