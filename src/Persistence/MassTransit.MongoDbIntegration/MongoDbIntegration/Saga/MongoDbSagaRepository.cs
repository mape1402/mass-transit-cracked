﻿#nullable enable
namespace MassTransit.MongoDbIntegration.Saga
{
    using MassTransit.Saga;
    using MongoDB.Driver;


    public static class MongoDbSagaRepository<TSaga>
        where TSaga : class, ISagaVersion
    {
        public static ISagaRepository<TSaga> Create(MongoUrl mongoUrl, string? collectionName = null)
        {
            return Create(mongoUrl.Url, mongoUrl.DatabaseName, collectionName);
        }

        public static ISagaRepository<TSaga> Create(string connectionString, string database, string? collectionName = null)
        {
            var mongoDatabase = new MongoClient(connectionString).GetDatabase(database);

            return Create(mongoDatabase, collectionName);
        }

        public static ISagaRepository<TSaga> Create(IMongoDatabase mongoDatabase, string? collectionName = null)
        {
            return Create(mongoDatabase, new DefaultCollectionNameFormatter(collectionName));
        }

        public static ISagaRepository<TSaga> Create(IMongoDatabase database, ICollectionNameFormatter collectionNameFormatter)
        {
            var mongoDbSagaConsumeContextFactory = new SagaConsumeContextFactory<MongoDbCollectionContext<TSaga>, TSaga>();

            IMongoCollection<TSaga> collection = database.GetCollection<TSaga>(collectionNameFormatter.Saga<TSaga>());

            var mongoDbContext = new NoSessionMongoDbCollectionContext<TSaga>(collection);

            var repositoryContextFactory = new MongoDbSagaRepositoryContextFactory<TSaga>(mongoDbContext, mongoDbSagaConsumeContextFactory);

            return new SagaRepository<TSaga>(repositoryContextFactory);
        }
    }
}
