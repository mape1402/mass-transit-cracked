namespace MassTransit.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;


    public class CachedSendEndpoint<TKey> :
        ITransportSendEndpoint,
        INotifyValueUsed,
        IAsyncDisposable
    {
        readonly ITransportSendEndpoint _endpoint;

        public CachedSendEndpoint(TKey key, ISendEndpoint endpoint)
        {
            Key = key;
            _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
        }

        public TKey Key { get; }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return _endpoint switch
            {
                IAsyncDisposable disposable => disposable.DisposeAsync(),
                _ => default
            };
        }

        public event Action Used;

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            Used?.Invoke();
            return _endpoint.ConnectSendObserver(observer);
        }

        public Task<SendContext<T>> CreateSendContext<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            return _endpoint.CreateSendContext(message, pipe, cancellationToken);
        }

        public Task Send<T>(T message, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send(message, cancellationToken);
        }

        public Task Send<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send(message, pipe, cancellationToken);
        }

        public Task Send<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send(message, pipe, cancellationToken);
        }

        public Task Send(object message, CancellationToken cancellationToken = new CancellationToken())
        {
            Used?.Invoke();
            return _endpoint.Send(message, cancellationToken);
        }

        public Task Send(object message, Type messageType, CancellationToken cancellationToken = new CancellationToken())
        {
            Used?.Invoke();
            return _endpoint.Send(message, messageType, cancellationToken);
        }

        public Task Send(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        {
            Used?.Invoke();
            return _endpoint.Send(message, pipe, cancellationToken);
        }

        public Task Send(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        {
            Used?.Invoke();
            return _endpoint.Send(message, messageType, pipe, cancellationToken);
        }

        public Task Send<T>(object values, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send<T>(values, cancellationToken);
        }

        public Task Send<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send(values, pipe, cancellationToken);
        }

        public Task Send<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
            where T : class
        {
            Used?.Invoke();
            return _endpoint.Send<T>(values, pipe, cancellationToken);
        }
    }
}
