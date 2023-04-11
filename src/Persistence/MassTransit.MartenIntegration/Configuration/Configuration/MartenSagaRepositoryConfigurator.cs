namespace MassTransit.Configuration
{
    using System;
    using System.Collections.Generic;
    using Marten;
    using Marten.Schema.Identity;
    using MartenIntegration.Saga;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Npgsql;
    using Saga;


    public class MartenSagaRepositoryConfigurator<TSaga> :
        IMartenSagaRepositoryConfigurator<TSaga>,
        ISpecification
        where TSaga : class, ISaga
    {
        readonly Action<MartenRegistry.DocumentMappingExpression<TSaga>> _configureSchema;
        Action<StoreOptions> _configureMarten;

        public MartenSagaRepositoryConfigurator(Action<MartenRegistry.DocumentMappingExpression<TSaga>> configureSchema = null)
        {
            _configureSchema = configureSchema;
        }

        public void Connection(string connectionString, Action<StoreOptions> configure = null)
        {
            void ConfigureOptions(StoreOptions options)
            {
                options.Connection(connectionString);

                configure?.Invoke(options);
            }

            _configureMarten = ConfigureOptions;
        }

        public void Connection(Func<NpgsqlConnection> connectionFactory, Action<StoreOptions> configure = null)
        {
            void ConfigureOptions(StoreOptions options)
            {
                options.Connection(connectionFactory);

                configure?.Invoke(options);
            }

            _configureMarten = ConfigureOptions;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        public void Register<T>(ISagaRepositoryRegistrationConfigurator<T> configurator)
            where T : class, ISaga
        {
            if (_configureMarten != null)
            {
                configurator.AddMarten(options =>
                {
                    _configureMarten(options);
                });
            }

            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureMarten, MartenSagaRepositoryStoreOptionsConfigurator>(Factory));
            configurator.RegisterSagaRepository<T, IDocumentSession, SagaConsumeContextFactory<IDocumentSession, T>, MartenSagaRepositoryContextFactory<T>>();
        }

        MartenSagaRepositoryStoreOptionsConfigurator Factory(IServiceProvider provider)
        {
            return new MartenSagaRepositoryStoreOptionsConfigurator(_configureSchema);
        }


        class MartenSagaRepositoryStoreOptionsConfigurator :
            IConfigureMarten
        {
            readonly Action<MartenRegistry.DocumentMappingExpression<TSaga>> _configure;

            public MartenSagaRepositoryStoreOptionsConfigurator(Action<MartenRegistry.DocumentMappingExpression<TSaga>> configure)
            {
                _configure = configure;
            }

            public void Configure(IServiceProvider services, StoreOptions options)
            {
                MartenRegistry.DocumentMappingExpression<TSaga> mappingExpression =
                    options.Schema.For<TSaga>().Identity(x => x.CorrelationId).IdStrategy(new NoOpIdGeneration());

                _configure?.Invoke(mappingExpression);
            }
        }
    }
}
