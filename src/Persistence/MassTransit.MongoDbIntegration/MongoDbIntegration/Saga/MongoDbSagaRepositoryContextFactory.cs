namespace MassTransit.MongoDbIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MassTransit.Saga;
    using MongoDB.Driver;


    public class MongoDbSagaRepositoryContextFactory<TSaga> :
        ISagaRepositoryContextFactory<TSaga>
        where TSaga : class, ISagaVersion
    {
        readonly MongoDbCollectionContext<TSaga> _dbContext;
        readonly ISagaConsumeContextFactory<MongoDbCollectionContext<TSaga>, TSaga> _factory;

        public MongoDbSagaRepositoryContextFactory(MongoDbCollectionContext<TSaga> dbContext,
            ISagaConsumeContextFactory<MongoDbCollectionContext<TSaga>, TSaga> factory)
        {
            _dbContext = dbContext;
            _factory = factory;
        }

        public void Probe(ProbeContext context)
        {
            context.Add("persistence", "mongodb");
        }

        public async Task Send<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
            where T : class
        {
            var repositoryContext = new MongoDbSagaRepositoryContext<TSaga, T>(_dbContext, context, _factory);

            await next.Send(repositoryContext).ConfigureAwait(false);
        }

        public async Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
            where T : class
        {
            var repositoryContext = new MongoDbSagaRepositoryContext<TSaga, T>(_dbContext, context, _factory);

            IList<TSaga> instances = await _dbContext.Find(query.FilterExpression)
                .ToListAsync(repositoryContext.CancellationToken)
                .ConfigureAwait(false);

            var queryContext = new LoadedSagaRepositoryQueryContext<TSaga, T>(repositoryContext, instances);

            await next.Send(queryContext).ConfigureAwait(false);
        }

        public async Task<T> Execute<T>(Func<SagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
            where T : class
        {
            var repositoryContext = new MongoDbSagaRepositoryContext<TSaga>(_dbContext, cancellationToken);

            return await asyncMethod(repositoryContext).ConfigureAwait(false);
        }
    }
}
