﻿namespace MassTransit
{
    using System;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Middleware;
    using Middleware.Outbox;


    public static class InMemoryOutboxConfigurationExtensions
    {
        /// <summary>
        /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
        /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
        /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
        /// </summary>
        /// <param name="configurator">The pipe configurator</param>
        /// <param name="configure">Configure the outbox</param>
        public static void UseInMemoryOutbox<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<IOutboxConfigurator> configure = default)
            where T : class
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var specification = new InMemoryOutboxSpecification<T>();

            configure?.Invoke(specification);

            configurator.AddPipeSpecification(specification);
        }

        /// <summary>
        /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
        /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
        /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
        /// </summary>
        /// <param name="configurator">The pipe configurator</param>
        /// <param name="configure">Configure the outbox</param>
        public static void UseInMemoryOutbox(this IConsumePipeConfigurator configurator, Action<IOutboxConfigurator> configure = default)
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var observer = new InMemoryOutboxConfigurationObserver(configurator, configure);
        }

        /// <summary>
        /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
        /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
        /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="configure">Configure the outbox</param>
        public static void UseInMemoryOutbox<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<IOutboxConfigurator> configure = default)
            where TConsumer : class
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var observer = new InMemoryOutboxConsumerConfigurationObserver<TConsumer>(configurator, configure);
            configurator.ConnectConsumerConfigurationObserver(observer);
        }

        /// <summary>
        /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
        /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
        /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="configure">Configure the outbox</param>
        public static void UseInMemoryOutbox<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IOutboxConfigurator> configure = default)
            where TSaga : class, ISaga
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var observer = new InMemoryOutboxSagaConfigurationObserver<TSaga>(configurator, configure);
            configurator.ConnectSagaConfigurationObserver(observer);
        }

        /// <summary>
        /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
        /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
        /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="configure">Configure the outbox</param>
        public static void UseInMemoryOutbox<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<IOutboxConfigurator> configure = default)
            where TMessage : class
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var observer = new InMemoryOutboxHandlerConfigurationObserver(configure);
            configurator.ConnectHandlerConfigurationObserver(observer);
        }

        /// <summary>
        /// Adds the required components to support the in-memory version of the InboxOutbox, which is intended for
        /// testing purposes only.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IServiceCollection AddInMemoryInboxOutbox(this IServiceCollection collection)
        {
            collection.TryAddSingleton<InMemoryOutboxMessageRepository>();
            collection.TryAddScoped<IOutboxContextFactory<InMemoryOutboxMessageRepository>, InMemoryOutboxContextFactory>();

            return collection;
        }

        /// <summary>
        /// Includes a combination inbox/outbox in the consume pipeline, which stores outgoing messages in memory until
        /// the message consumer completes.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="provider">Configuration service provider</param>
        public static void UseInMemoryInboxOutbox(this IReceiveEndpointConfigurator configurator, IServiceProvider provider)
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var observer = new OutboxConsumePipeSpecificationObserver<InMemoryOutboxMessageRepository>(configurator, provider);

            configurator.ConnectConsumerConfigurationObserver(observer);
            configurator.ConnectSagaConfigurationObserver(observer);
        }
    }
}
