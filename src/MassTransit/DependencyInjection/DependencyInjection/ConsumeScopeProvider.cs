namespace MassTransit.DependencyInjection
{
    using System;
    using System.Threading.Tasks;
    using Context;
    using Microsoft.Extensions.DependencyInjection;


    public class ConsumeScopeProvider :
        BaseConsumeScopeProvider,
        IConsumeScopeProvider
    {
        public ConsumeScopeProvider(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public void Probe(ProbeContext context)
        {
            context.Add("provider", "dependencyInjection");
        }

        public ValueTask<IConsumeScopeContext> GetScope(ConsumeContext context)
        {
            return GetScopeContext(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
        }

        public ValueTask<IConsumeScopeContext<T>> GetScope<T>(ConsumeContext<T> context)
            where T : class
        {
            return GetScopeContext(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
        }

        public ValueTask<IConsumerConsumeScopeContext<TConsumer, T>> GetScope<TConsumer, T>(ConsumeContext<T> context)
            where TConsumer : class
            where T : class
        {
            return GetScopeContext(context, ExistingScopeContextFactory<TConsumer, T>, CreatedScopeContextFactory<TConsumer, T>, PipeContextFactory);
        }

        static ConsumeContext PipeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IServiceProvider serviceProvider)
        {
            return new ConsumeContextScope(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
        }

        static IConsumeScopeContext ExistingScopeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IDisposable disposable)
        {
            return new ExistingConsumeScopeContext(consumeContext, disposable);
        }

        static IConsumeScopeContext CreatedScopeContextFactory(ConsumeContext consumeContext, IServiceScope serviceScope, IDisposable disposable)
        {
            return new CreatedConsumeScopeContext(serviceScope, consumeContext, disposable);
        }

        static ConsumeContext<T> PipeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IServiceProvider serviceProvider)
            where T : class
        {
            return new ConsumeContextScope<T>(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
        }

        static IConsumeScopeContext<T> ExistingScopeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IDisposable disposable)
            where T : class
        {
            return new ExistingConsumeScopeContext<T>(consumeContext, serviceScope, disposable);
        }

        static IConsumeScopeContext<T> CreatedScopeContextFactory<T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope, IDisposable disposable)
            where T : class
        {
            return new CreatedConsumeScopeContext<T>(serviceScope, consumeContext, disposable);
        }

        static IConsumerConsumeScopeContext<TConsumer, T> ExistingScopeContextFactory<TConsumer, T>(ConsumeContext<T> consumeContext,
            IServiceScope serviceScope, IDisposable disposable)
            where T : class
            where TConsumer : class
        {
            var consumer = serviceScope.ServiceProvider.GetService<TConsumer>();
            if (consumer == null)
                throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

            var consumerContext = new ConsumerConsumeContextScope<TConsumer, T>(consumeContext, consumer);

            return new ExistingConsumerConsumeScopeContext<TConsumer, T>(consumerContext, disposable);
        }

        static IConsumerConsumeScopeContext<TConsumer, T> CreatedScopeContextFactory<TConsumer, T>(ConsumeContext<T> consumeContext, IServiceScope serviceScope,
            IDisposable disposable)
            where T : class
            where TConsumer : class
        {
            var consumer = serviceScope.ServiceProvider.GetService<TConsumer>();
            if (consumer == null)
                throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

            var consumerContext = new ConsumerConsumeContextScope<TConsumer, T>(consumeContext, consumer);

            return new CreatedConsumerConsumeScopeContext<TConsumer, T>(serviceScope, consumerContext, disposable);
        }
    }
}
