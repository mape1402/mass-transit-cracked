namespace MassTransit.Batching
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Util;


    public class BatchCollector<TMessage> :
        IBatchCollector<TMessage>
        where TMessage : class
    {
        readonly ChannelExecutor _collector;
        readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
        readonly ChannelExecutor _dispatcher;
        readonly BatchOptions _options;
        BatchConsumer<TMessage> _currentConsumer;

        public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe)
        {
            _options = options;
            _consumerPipe = consumerPipe;

            _collector = new ChannelExecutor(1);
            _dispatcher = new ChannelExecutor(options.ConcurrencyLimit);
        }

        public ValueTask DisposeAsync()
        {
            return _collector.DisposeAsync();
        }

        public Task<BatchConsumer<TMessage>> Collect(ConsumeContext<TMessage> context)
        {
            var currentActivity = Activity.Current;

            return _collector.Run(() => Add(context, currentActivity), context.CancellationToken);
        }

        public Task Complete(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer)
        {
            return _collector.Run(() => Remove(consumer));
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("batchCollector");

            _consumerPipe.Probe(scope);
        }

        Task Remove(BatchConsumer<TMessage> consumer)
        {
            if (_currentConsumer == consumer)
                _currentConsumer = null;

            return Task.CompletedTask;
        }

        async Task<BatchConsumer<TMessage>> Add(ConsumeContext<TMessage> context, Activity currentActivity)
        {
            if (_currentConsumer != null)
            {
                if (context.GetRetryAttempt() > 0)
                    await _currentConsumer.ForceComplete().ConfigureAwait(false);
            }

            if (_currentConsumer == null || _currentConsumer.IsCompleted)
                _currentConsumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe);

            await _currentConsumer.Add(context, currentActivity).ConfigureAwait(false);

            return _currentConsumer;
        }
    }


    public class BatchCollector<TMessage, TKey> :
        IBatchCollector<TMessage>
        where TMessage : class
    {
        readonly ChannelExecutor _collector;
        readonly IDictionary<TKey, BatchConsumer<TMessage>> _collectors;
        readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
        readonly ChannelExecutor _dispatcher;
        readonly IGroupKeyProvider<TMessage, TKey> _keyProvider;
        readonly BatchOptions _options;
        BatchConsumer<TMessage> _currentConsumer;

        public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe, IGroupKeyProvider<TMessage, TKey> keyProvider)
        {
            _options = options;
            _consumerPipe = consumerPipe;
            _keyProvider = keyProvider;

            _collector = new ChannelExecutor(1);
            _dispatcher = new ChannelExecutor(options.ConcurrencyLimit);
            _collectors = new Dictionary<TKey, BatchConsumer<TMessage>>();
        }

        public ValueTask DisposeAsync()
        {
            return _collector.DisposeAsync();
        }

        public Task<BatchConsumer<TMessage>> Collect(ConsumeContext<TMessage> context)
        {
            var currentActivity = Activity.Current;

            return _collector.Run(() => Add(context, currentActivity), context.CancellationToken);
        }

        public Task Complete(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer)
        {
            return _collector.Run(() => Remove(context, consumer));
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("batchCollector");

            _consumerPipe.Probe(scope);
        }

        Task Remove(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer)
        {
            if (_currentConsumer == consumer)
                _currentConsumer = null;
            else if (_keyProvider.TryGetKey(context, out var key) && _collectors.TryGetValue(key, out BatchConsumer<TMessage> existingConsumer))
            {
                if (existingConsumer == consumer)
                    _collectors.Remove(key);
            }

            return Task.CompletedTask;
        }

        async Task<BatchConsumer<TMessage>> Add(ConsumeContext<TMessage> context, Activity currentActivity)
        {
            if (_keyProvider.TryGetKey(context, out var key))
            {
                if (_collectors.TryGetValue(key, out BatchConsumer<TMessage> consumer))
                {
                    if (context.GetRetryAttempt() > 0)
                        await consumer.ForceComplete().ConfigureAwait(false);
                }

                if (consumer == null || consumer.IsCompleted)
                {
                    consumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe);
                    _collectors[key] = consumer;
                }

                await consumer.Add(context, currentActivity).ConfigureAwait(false);

                return consumer;
            }

            if (_currentConsumer != null)
            {
                if (context.GetRetryAttempt() > 0)
                    await _currentConsumer.ForceComplete().ConfigureAwait(false);
            }

            if (_currentConsumer == null || _currentConsumer.IsCompleted)
                _currentConsumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe);

            await _currentConsumer.Add(context, currentActivity).ConfigureAwait(false);

            return _currentConsumer;
        }
    }
}
