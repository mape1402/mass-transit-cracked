namespace MassTransit.KafkaIntegration.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using MassTransit.Middleware;
    using Transports;


    public class KafkaConsumerFilter<TKey, TValue> :
        IFilter<ConsumerContext>
        where TValue : class
    {
        readonly KafkaReceiveEndpointContext<TKey, TValue> _context;

        public KafkaConsumerFilter(KafkaReceiveEndpointContext<TKey, TValue> context)
        {
            _context = context;
        }

        public async Task Send(ConsumerContext context, IPipe<ConsumerContext> next)
        {
            var receiveSettings = _context.GetPayload<ReceiveSettings>();
            var consumers = new IKafkaMessageConsumer<TKey, TValue>[receiveSettings.ConcurrentConsumerLimit];
            for (var i = 0; i < consumers.Length; i++)
                consumers[i] = new KafkaMessageConsumer<TKey, TValue>(receiveSettings, _context, context);

            var supervisor = CreateConsumerSupervisor(consumers);

            await _context.TransportObservers.NotifyReady(_context.InputAddress).ConfigureAwait(false);

            try
            {
                await supervisor.Completed.ConfigureAwait(false);
            }
            finally
            {
                DeliveryMetrics metrics = new CombinedDeliveryMetrics(consumers);

                await _context.TransportObservers.NotifyCompleted(_context.InputAddress, metrics).ConfigureAwait(false);

                _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.ConcurrentDeliveryCount);
            }

            await next.Send(context).ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
        }

        Supervisor CreateConsumerSupervisor(IKafkaMessageConsumer<TKey, TValue>[] actualConsumers)
        {
            var supervisor = new ConsumerSupervisor(actualConsumers);

            _context.AddConsumeAgent(supervisor);

            supervisor.SetReady();

            return supervisor;
        }


        class ConsumerSupervisor :
            Supervisor
        {
            public ConsumerSupervisor(IKafkaMessageConsumer<TKey, TValue>[] consumers)
            {
                foreach (IKafkaMessageConsumer<TKey, TValue> consumer in consumers)
                {
                    consumer.Completed.ContinueWith(async _ =>
                    {
                        try
                        {
                            if (!IsStopping)
                                await this.Stop("Consumer stopped, stopping supervisor").ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            LogContext.Warning?.Log(exception, "Stop Faulted");
                        }
                    }, TaskContinuationOptions.RunContinuationsAsynchronously);

                    Add(consumer);
                }
            }
        }


        class CombinedDeliveryMetrics :
            DeliveryMetrics
        {
            public CombinedDeliveryMetrics(IKafkaMessageConsumer<TKey, TValue>[] receivers)
            {
                DeliveryCount = receivers.Sum(x => x.DeliveryCount);
                ConcurrentDeliveryCount = receivers.Sum(x => x.ConcurrentDeliveryCount);
            }

            public long DeliveryCount { get; }
            public int ConcurrentDeliveryCount { get; }
        }
    }
}
