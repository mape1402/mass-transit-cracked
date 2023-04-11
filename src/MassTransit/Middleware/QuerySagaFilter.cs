namespace MassTransit.Middleware
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;


    /// <summary>
    /// Creates a filter to send a query to the saga repository using the query factory and saga policy provided.
    /// </summary>
    /// <typeparam name="TSaga">The saga type</typeparam>
    /// <typeparam name="TMessage">The message type</typeparam>
    public class QuerySagaFilter<TSaga, TMessage> :
        IFilter<ConsumeContext<TMessage>>
        where TSaga : class, ISaga
        where TMessage : class
    {
        readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _messagePipe;
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;
        readonly ISagaRepository<TSaga> _sagaRepository;

        public QuerySagaFilter(ISagaRepository<TSaga> sagaRepository, ISagaPolicy<TSaga, TMessage> policy,
            ISagaQueryFactory<TSaga, TMessage> queryFactory, IPipe<SagaConsumeContext<TSaga, TMessage>> messagePipe)
        {
            _sagaRepository = sagaRepository;
            _messagePipe = messagePipe;
            _policy = policy;
            _queryFactory = queryFactory;
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("saga");
            scope.Set(new { Correlation = "Query" });

            _queryFactory.Probe(scope);
            _sagaRepository.Probe(scope);

            _messagePipe.Probe(scope);
        }

        public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                if (_queryFactory.TryCreateQuery(context, out ISagaQuery<TSaga> query))
                {
                    await _sagaRepository.SendQuery(context, query, _policy, _messagePipe).ConfigureAwait(false);

                    await context.NotifyConsumed(timer.Elapsed, TypeCache<TSaga>.ShortName).ConfigureAwait(false);
                }

                await next.Send(context).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception.GetBaseException() is OperationCanceledException && !context.CancellationToken.IsCancellationRequested)
            {
                await context.NotifyFaulted(timer.Elapsed, TypeCache<TSaga>.ShortName, exception).ConfigureAwait(false);

                throw new ConsumerCanceledException($"The operation was canceled by the saga: {TypeCache<TSaga>.ShortName}");
            }
            catch (Exception exception)
            {
                await context.NotifyFaulted(timer.Elapsed, TypeCache<TSaga>.ShortName, exception).ConfigureAwait(false);

                throw;
            }
        }
    }
}
